using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014InvitationItemImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedMissingRequiredIdMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014InvitationItemODataImporter
{
    public static async Task<Visa2014InvitationItemImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string passportIdMapPath,
        string invitationIdMapPath,
        string? invitationItemIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var invitationIdMap = Visa2014IdMapHelper.Load(invitationIdMapPath);

        if (verbose)
        {
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");
            Console.WriteLine($"INF Invitation id-map entries: {invitationIdMap.Count}");
        }

        var batch = Visa2014InvitationItemTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missing = CountMissingRequiredIdMap(batch.ImportRows, personIdMap, passportIdMap, invitationIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {missing} missing required id-map).");
            return new Visa2014InvitationItemImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                SkippedMissingRequiredIdMap = missing,
            };
        }

        var invitationItemIdMap = LoadOptionalIdMap(invitationItemIdMapOutputPath);
        if (verbose && invitationItemIdMap.Count > 0)
            Console.WriteLine($"INF Existing InvitationItem id-map entries: {invitationItemIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedMissingRequired = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (invitationItemIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in InvitationItem id-map");
                continue;
            }

            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    invitationIdMap,
                    out var personId,
                    out var passportId,
                    out var invitationId,
                    out var missingReason))
            {
                skippedMissingRequired++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: {missingReason}");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, personId, passportId, invitationId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.InvitationItem), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                invitationItemIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedMissingRequired} missing id-map...");
                if (verbose)
                    Console.WriteLine($"  SAVE InvitationItem {createdId.Value} <- legacy PersonInInvitation {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();

        string? idMapPath = null;
        if (invitationItemIdMap.Count > 0 && !string.IsNullOrWhiteSpace(invitationItemIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(invitationItemIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = invitationItemIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014InvitationItemImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedMissingRequiredIdMap = skippedMissingRequired,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingRequiredIdMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    invitationIdMap,
                    out _,
                    out _,
                    out _,
                    out _))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveRequiredIds(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> invitationIdMap,
        out Guid personId,
        out Guid passportId,
        out Guid invitationId,
        out string missingReason)
    {
        personId = Guid.Empty;
        passportId = Guid.Empty;
        invitationId = Guid.Empty;
        missingReason = "";

        if (!TryResolveLegacyGuid(row, "Person", out var legacyPersonOid) ||
            !personIdMap.TryGetValue(legacyPersonOid, out personId))
        {
            missingReason = "Person not in id-map";
            return false;
        }

        if (!TryResolveLegacyGuid(row, "Passport", out var legacyPassportOid) ||
            !passportIdMap.TryGetValue(legacyPassportOid, out passportId))
        {
            missingReason = "Passport not in id-map";
            return false;
        }

        if (!TryResolveLegacyGuid(row, "Invitation", out var legacyInvitationOid) ||
            !invitationIdMap.TryGetValue(legacyInvitationOid, out invitationId))
        {
            missingReason = "Invitation not in id-map";
            return false;
        }

        return true;
    }

    private static bool TryResolveLegacyGuid(Dictionary<string, object?> row, string field, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(field) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Guid personId,
        Guid passportId,
        Guid invitationId)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Passport"] = new { ID = passportId },
            ["Invitation"] = new { ID = invitationId },
        };
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    private static Dictionary<string, object?>? BuildPayloadWithoutParents(Dictionary<string, object?> row)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal);
    }
}
