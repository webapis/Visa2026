using System.Text.Json;
using Visa2026.DataImporter;
using Visa2026.Module.Services;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonODataImporter
{
    public static async Task<Visa2014PersonImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string? idMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose,
        bool supplementPermitReferencedOnly = false)
    {
        var batch = supplementPermitReferencedOnly
            ? Visa2014PersonTransform.PrepareSupplementPermitReferencedImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose)
            : Visa2014PersonTransform.PrepareImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose);

        if (supplementPermitReferencedOnly && verbose)
            Console.WriteLine("INF Mode: supplement permit-referenced soft-deleted Person rows (import as IsArchived).");

        var existingIdMap = !string.IsNullOrWhiteSpace(idMapOutputPath)
            ? Visa2014IdMapHelper.Load(idMapOutputPath)
            : new Dictionary<Guid, Guid>();
        if (supplementPermitReferencedOnly && verbose && existingIdMap.Count > 0)
            Console.WriteLine($"INF Existing Person id-map entries: {existingIdMap.Count}");

        if (dryRun)
        {
            int alreadyImported = supplementPermitReferencedOnly
                ? CountAlreadyImported(batch.ImportRows, existingIdMap)
                : 0;
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} duplicate suffixed" +
                (supplementPermitReferencedOnly ? $", {alreadyImported} already in id-map" : "") +
                ").");
            return new Visa2014PersonImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedAlreadyImported = alreadyImported,
            };
        }

        var employeeProjectContractByLegacyOid = batch.ImportRows
            .Where(IsEmployeeRow)
            .Where(r => r["_legacyRowId"] is Guid)
            .ToDictionary(
                r => (Guid)r["_legacyRowId"]!,
                r => r.GetValueOrDefault("ProjectContract") as string);

        var employeeSubcontractorByLegacyOid = batch.ImportRows
            .Where(IsEmployeeRow)
            .Where(r => r["_legacyRowId"] is Guid)
            .ToDictionary(
                r => (Guid)r["_legacyRowId"]!,
                r => r.GetValueOrDefault("Subcontractor") as string);

        var idMap = new Dictionary<Guid, Guid>(existingIdMap);
        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;

        var employees = batch.ImportRows.Where(r => IsEmployeeRow(r)).ToList();
        var familyMembers = batch.ImportRows.Where(r => !IsEmployeeRow(r)).ToList();

        foreach (var row in employees.Concat(familyMembers))
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (supplementPermitReferencedOnly && idMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Person id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver, idMap, employeeProjectContractByLegacyOid, employeeSubcontractorByLegacyOid);
                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Person), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                idMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                {
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedAlreadyImported} already imported...");
                    if (supplementPermitReferencedOnly && !string.IsNullOrWhiteSpace(idMapOutputPath))
                        await WriteIdMapAsync(idMapOutputPath, idMap);
                }
                if (verbose)
                    Console.WriteLine($"  SAVE Person {createdId.Value} <- legacy {legacyOid}");
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
        if (idMap.Count > 0 && !string.IsNullOrWhiteSpace(idMapOutputPath))
        {
            idMapPath = Path.GetFullPath(idMapOutputPath);
            await WriteIdMapAsync(idMapPath, idMap);
        }

        return new Visa2014PersonImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountAlreadyImported(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> idMap)
    {
        int count = 0;
        foreach (var row in importRows)
        {
            if (row["_legacyRowId"] is Guid legacyOid && idMap.ContainsKey(legacyOid))
                count++;
        }

        return count;
    }

    private static bool IsEmployeeRow(Dictionary<string, object?> row) =>
        row.TryGetValue("IsEmployee", out var v) && v is bool b && b;

    private static Dictionary<string, object?> BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<Guid, Guid> idMap,
        IReadOnlyDictionary<Guid, string?> employeeProjectContractByLegacyOid,
        IReadOnlyDictionary<Guid, string?> employeeSubcontractorByLegacyOid)
    {
        var isEmployee = IsEmployeeRow(row);
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["FirstName"] = row["FirstName"],
            ["LastName"] = row["LastName"],
            ["DateOfBirth"] = DateTime.SpecifyKind((DateTime)row["DateOfBirth"]!, DateTimeKind.Utc),
            ["PersonRole"] = isEmployee ? "Employee" : "FamilyMember",
            ["PersonalNumber"] = row.GetValueOrDefault("PersonalNumber") ?? "0",
        };

        if (row.GetValueOrDefault("Email") is string email && !string.IsNullOrWhiteSpace(email))
            payload["Email"] = email;

        // MiddleName intentionally not posted: legacy value is work-position text → EmployeePositionHistory.ActualPosition.
        if (row.GetValueOrDefault("BirthPlace") is string bp && !string.IsNullOrWhiteSpace(bp))
            payload["BirthPlace"] = bp;
        if (row.GetValueOrDefault("ForeignAddress") is string fa && !string.IsNullOrWhiteSpace(fa))
            payload["ForeignAddress"] = fa;

        if (row.GetValueOrDefault("IsArchived") is bool archived && archived)
            payload["IsArchived"] = true;

        TrySetLookup(payload, "Gender", resolver.ResolveGender(row.GetValueOrDefault("Gender") as string));
        TrySetLookup(payload, "CountryOfBirth", resolver.ResolveCountry(row.GetValueOrDefault("CountryOfBirth") as string));
        TrySetLookup(payload, "ForeignAddressCountry", resolver.ResolveCountry(row.GetValueOrDefault("ForeignAddressCountry") as string));
        TrySetLookup(payload, "Nationality", resolver.ResolveCountry(row.GetValueOrDefault("Nationality") as string));
        TrySetLookup(payload, "MaritalStatus", resolver.ResolveMaritalStatus(row.GetValueOrDefault("MaritalStatus") as string));
        TrySetLookup(payload, "Relationship", resolver.ResolveRelationship(row.GetValueOrDefault("Relationship") as string));

        var projectContractCode = row.GetValueOrDefault("ProjectContract") as string;
        var subcontractorName = row.GetValueOrDefault("Subcontractor") as string;
        if (!isEmployee
            && row.GetValueOrDefault("SponsoringEmployee") is string sponsorOidText
            && Guid.TryParse(sponsorOidText, out var legacySponsorOid))
        {
            if (string.IsNullOrWhiteSpace(projectContractCode))
                employeeProjectContractByLegacyOid.TryGetValue(legacySponsorOid, out projectContractCode);
            if (string.IsNullOrWhiteSpace(subcontractorName))
                employeeSubcontractorByLegacyOid.TryGetValue(legacySponsorOid, out subcontractorName);
        }

        TrySetLookup(payload, "ProjectContract", resolver.ResolveProjectContract(projectContractCode));
        TrySetLookup(payload, "Subcontractor", resolver.ResolveSubcontractor(subcontractorName));

        if (row.GetValueOrDefault("SponsoringEmployee") is string sponsorText &&
            Guid.TryParse(sponsorText, out var legacySponsorOidForFk) &&
            idMap.TryGetValue(legacySponsorOidForFk, out var sponsorId))
        {
            payload["SponsoringEmployee"] = new { ID = sponsorId };
        }

        if (isEmployee)
        {
            var maritalCode = row.GetValueOrDefault("MaritalStatus") as string;
            if (string.Equals(maritalCode, "Sallah", StringComparison.OrdinalIgnoreCase))
            {
                payload["VisaApplicationFamilyMembersText"] = VisaFamilyMemberLinesHelper.NoneValue;
            }
            else if (row.GetValueOrDefault("VisaApplicationFamilyMembersText") is string familyText
                     && !string.IsNullOrWhiteSpace(familyText)
                     && !VisaFamilyMemberLinesHelper.IsNoneValue(familyText))
            {
                payload["VisaApplicationFamilyMembersText"] = familyText;
            }
        }

        return payload;
    }

    private static void TrySetLookup(Dictionary<string, object?> payload, string property, Guid? id)
    {
        if (id.HasValue)
            payload[property] = new { ID = id.Value };
    }

    private static async Task WriteIdMapAsync(string idMapOutputPath, IReadOnlyDictionary<Guid, Guid> idMap)
    {
        var idMapPath = Path.GetFullPath(idMapOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
        var serializable = idMap.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value.ToString());
        await File.WriteAllTextAsync(
            idMapPath,
            JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static async Task<Visa2014SyncEntityResult> RunSyncAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        Visa2014SyncContext sync,
        int? maxRows,
        bool verbose)
    {
        var batch = Visa2014PersonTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        var employeeProjectContractByLegacyOid = batch.ImportRows
            .Where(IsEmployeeRow)
            .Where(r => r["_legacyRowId"] is Guid)
            .ToDictionary(
                r => (Guid)r["_legacyRowId"]!,
                r => r.GetValueOrDefault("ProjectContract") as string);

        var employeeSubcontractorByLegacyOid = batch.ImportRows
            .Where(IsEmployeeRow)
            .Where(r => r["_legacyRowId"] is Guid)
            .ToDictionary(
                r => (Guid)r["_legacyRowId"]!,
                r => r.GetValueOrDefault("Subcontractor") as string);

        var employees = batch.ImportRows.Where(IsEmployeeRow).ToList();
        var familyMembers = batch.ImportRows.Where(r => !IsEmployeeRow(r)).ToList();
        var orderedRows = employees.Concat(familyMembers).ToList();

        return await Visa2014SyncUpsertHelper.RunAsync(
            target,
            typeof(Visa2026.Module.BusinessObjects.Person),
            "Person",
            orderedRows,
            sync,
            row => BuildPayload(row, resolver, sync.IdMap, employeeProjectContractByLegacyOid, employeeSubcontractorByLegacyOid),
            batch.LegacyRowCount,
            batch.Skipped.Count,
            batch.DedupeMergedCount,
            verbose);
    }
}
