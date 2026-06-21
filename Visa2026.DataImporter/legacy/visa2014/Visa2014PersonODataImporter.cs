using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonODataImporter
{
    public static async Task<Visa2014PersonImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        string lookupTranslationsPath,
        string? idMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var batch = Visa2014PersonTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationsPath,
            maxRows,
            verbose);

        if (dryRun)
        {
            Console.WriteLine($"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST ({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged).");
            return new Visa2014PersonImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
            };
        }

        var resolver = new Visa2014ODataLookupResolver();
        await resolver.LoadAsync(api);

        var idMap = new Dictionary<Guid, Guid>();
        var errors = new List<string>();
        int posted = 0;
        int failed = 0;

        var employees = batch.ImportRows.Where(r => IsEmployeeRow(r)).ToList();
        var familyMembers = batch.ImportRows.Where(r => !IsEmployeeRow(r)).ToList();

        foreach (var row in employees.Concat(familyMembers))
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            try
            {
                var payload = BuildPayload(row, resolver, idMap);
                var created = await api.CreateAsync<Person>("Person", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: POST returned null");
                    continue;
                }

                idMap[legacyOid] = created.Id;
                posted++;
                if (verbose)
                    Console.WriteLine($"  POST Person {created.Id} ← legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        string? idMapPath = null;
        if (idMap.Count > 0 && !string.IsNullOrWhiteSpace(idMapOutputPath))
        {
            idMapPath = Path.GetFullPath(idMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = idMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014PersonImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static bool IsEmployeeRow(Dictionary<string, object?> row) =>
        row.TryGetValue("IsEmployee", out var v) && v is bool b && b;

    private static Dictionary<string, object?> BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<Guid, Guid> idMap)
    {
        var isEmployee = IsEmployeeRow(row);
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["FirstName"] = row["FirstName"],
            ["LastName"] = row["LastName"],
            ["DateOfBirth"] = DateTime.SpecifyKind((DateTime)row["DateOfBirth"]!, DateTimeKind.Utc),
            ["IsEmployee"] = isEmployee,
            ["PersonRole"] = isEmployee ? 0 : 1,
            ["Email"] = row.GetValueOrDefault("Email") ?? "",
            ["PersonalNumber"] = row.GetValueOrDefault("PersonalNumber") ?? "0",
            ["IsArchived"] = row.GetValueOrDefault("IsArchived") is bool archived && archived,
        };

        if (row.GetValueOrDefault("MiddleName") is string middle && !string.IsNullOrWhiteSpace(middle))
            payload["MiddleName"] = middle;
        if (row.GetValueOrDefault("BirthPlace") is string bp && !string.IsNullOrWhiteSpace(bp))
            payload["BirthPlace"] = bp;
        if (row.GetValueOrDefault("ForeignAddress") is string fa && !string.IsNullOrWhiteSpace(fa))
            payload["ForeignAddress"] = fa;
        if (row.GetValueOrDefault("VisaApplicationFamilyMembersText") is string fam &&
            !string.IsNullOrWhiteSpace(fam))
            payload["VisaApplicationFamilyMembersText"] = fam;

        TrySetLookup(payload, "Gender", resolver.ResolveGender(row.GetValueOrDefault("Gender") as string));
        TrySetLookup(payload, "CountryOfBirth", resolver.ResolveCountry(row.GetValueOrDefault("CountryOfBirth") as string));
        TrySetLookup(payload, "ForeignAddressCountry", resolver.ResolveCountry(row.GetValueOrDefault("ForeignAddressCountry") as string));
        TrySetLookup(payload, "Nationality", resolver.ResolveCountry(row.GetValueOrDefault("Nationality") as string));
        TrySetLookup(payload, "MaritalStatus", resolver.ResolveMaritalStatus(row.GetValueOrDefault("MaritalStatus") as string));
        TrySetLookup(payload, "Relationship", resolver.ResolveRelationship(row.GetValueOrDefault("Relationship") as string));
        TrySetLookup(payload, "ProjectContract", resolver.ResolveProjectContract(row.GetValueOrDefault("ProjectContract") as string));

        if (row.GetValueOrDefault("SponsoringEmployee") is string sponsorText &&
            Guid.TryParse(sponsorText, out var legacySponsorOid) &&
            idMap.TryGetValue(legacySponsorOid, out var sponsorId))
        {
            payload["SponsoringEmployee"] = new { ID = sponsorId };
        }

        return payload;
    }

    private static void TrySetLookup(Dictionary<string, object?> payload, string property, Guid? id)
    {
        if (id.HasValue)
            payload[property] = new { ID = id.Value };
    }
}
