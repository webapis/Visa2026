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
        bool verbose)
    {
        var batch = Visa2014PersonTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
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

        var employeeProjectContractByLegacyOid = batch.ImportRows
            .Where(IsEmployeeRow)
            .Where(r => r["_legacyRowId"] is Guid)
            .ToDictionary(
                r => (Guid)r["_legacyRowId"]!,
                r => r.GetValueOrDefault("ProjectContract") as string);

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
                var payload = BuildPayload(row, resolver, idMap, employeeProjectContractByLegacyOid);
                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Person), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                idMap[legacyOid] = createdId.Value;
                posted++;
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
        IReadOnlyDictionary<Guid, Guid> idMap,
        IReadOnlyDictionary<Guid, string?> employeeProjectContractByLegacyOid)
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

        TrySetLookup(payload, "Gender", resolver.ResolveGender(row.GetValueOrDefault("Gender") as string));
        TrySetLookup(payload, "CountryOfBirth", resolver.ResolveCountry(row.GetValueOrDefault("CountryOfBirth") as string));
        TrySetLookup(payload, "ForeignAddressCountry", resolver.ResolveCountry(row.GetValueOrDefault("ForeignAddressCountry") as string));
        TrySetLookup(payload, "Nationality", resolver.ResolveCountry(row.GetValueOrDefault("Nationality") as string));
        TrySetLookup(payload, "MaritalStatus", resolver.ResolveMaritalStatus(row.GetValueOrDefault("MaritalStatus") as string));
        TrySetLookup(payload, "Relationship", resolver.ResolveRelationship(row.GetValueOrDefault("Relationship") as string));

        var projectContractCode = row.GetValueOrDefault("ProjectContract") as string;
        if (!isEmployee
            && string.IsNullOrWhiteSpace(projectContractCode)
            && row.GetValueOrDefault("SponsoringEmployee") is string sponsorOidText
            && Guid.TryParse(sponsorOidText, out var legacySponsorOid))
        {
            employeeProjectContractByLegacyOid.TryGetValue(legacySponsorOid, out projectContractCode);
        }

        TrySetLookup(payload, "ProjectContract", resolver.ResolveProjectContract(projectContractCode));
        TrySetLookup(payload, "Subcontractor", resolver.ResolveDefaultSubcontractor());

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
}
