using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014EducationImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014EducationODataImporter
{
    public static async Task<Visa2014EducationImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? educationIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = Visa2014EducationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingPerson = CountMissingPersonMap(batch.ImportRows, personIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missingPerson} missing Person id-map).");
            return new Visa2014EducationImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPersonMap = missingPerson,
            };
        }

        var resolver = new Visa2014ODataLookupResolver();
        await resolver.LoadAsync(api);

        var educationIdMap = LoadOptionalEducationIdMap(educationIdMapOutputPath);
        if (verbose && educationIdMap.Count > 0)
            Console.WriteLine($"INF Existing Education id-map entries: {educationIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (educationIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Education id-map");
                continue;
            }

            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                failed++;
                errors.Add($"{legacyOid}: missing legacy Person Oid on row");
                continue;
            }

            if (!personIdMap.TryGetValue(legacyPersonOid, out var personId))
            {
                skippedNoPerson++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: Person {legacyPersonOid} not in id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver, personId);
                if (payload == null)
                {
                    failed++;
                    var detail = DescribePayloadGap(row, resolver);
                    errors.Add($"{legacyOid}: incomplete OData payload ({detail})");
                    continue;
                }

                var created = await api.CreateAsync<Education>("Education", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: POST returned null");
                    continue;
                }

                educationIdMap[legacyOid] = created.Id;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  POST Education {created.Id} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        string? idMapPath = null;
        if (educationIdMap.Count > 0 && !string.IsNullOrWhiteSpace(educationIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(educationIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = educationIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014EducationImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedNoPersonMap = skippedNoPerson,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingPersonMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> personIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                missing++;
                continue;
            }

            if (!personIdMap.ContainsKey(legacyPersonOid))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveLegacyPersonOid(Dictionary<string, object?> row, out Guid legacyPersonOid)
    {
        legacyPersonOid = Guid.Empty;
        var text = row.GetValueOrDefault("Person") as string
            ?? row.GetValueOrDefault("_legacy_PersonOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyPersonOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid personId)
    {
        var educationLevelId = resolver.ResolveEducationLevel(row.GetValueOrDefault("EducationLevel") as string);
        var institutionId = resolver.ResolveEducationInstitution(row.GetValueOrDefault("EducationInstitution") as string);
        var countryId = resolver.ResolveCountry(row.GetValueOrDefault("EducationCountry") as string);
        var specialtyId = resolver.ResolveSpecialty(row.GetValueOrDefault("Specialty") as string);
        if (!educationLevelId.HasValue || !institutionId.HasValue || !countryId.HasValue || !specialtyId.HasValue)
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["EducationLevel"] = new { ID = educationLevelId.Value },
            ["EducationInstitution"] = new { ID = institutionId.Value },
            ["EducationCountry"] = new { ID = countryId.Value },
            ["Specialty"] = new { ID = specialtyId.Value },
        };

        if (row.GetValueOrDefault("GraduationYear") is string graduationYear && !string.IsNullOrWhiteSpace(graduationYear))
            payload["GraduationYear"] = graduationYear.Trim();

        return payload;
    }

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        if (!resolver.ResolveEducationLevel(row.GetValueOrDefault("EducationLevel") as string).HasValue)
            gaps.Add($"EducationLevel={row.GetValueOrDefault("EducationLevel")}");
        if (!resolver.ResolveEducationInstitution(row.GetValueOrDefault("EducationInstitution") as string).HasValue)
            gaps.Add($"EducationInstitution={row.GetValueOrDefault("EducationInstitution")}");
        if (!resolver.ResolveCountry(row.GetValueOrDefault("EducationCountry") as string).HasValue)
            gaps.Add($"EducationCountry={row.GetValueOrDefault("EducationCountry")}");
        if (!resolver.ResolveSpecialty(row.GetValueOrDefault("Specialty") as string).HasValue)
            gaps.Add($"Specialty={row.GetValueOrDefault("Specialty")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalEducationIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
