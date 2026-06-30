using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014FamilyMemberProjectContractSyncResult
{
    public int FamilyMembersScanned { get; init; }
    public int Patched { get; init; }
    public int SkippedAlreadySynced { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedNoSponsorMap { get; init; }
    public int SkippedNoSponsorContract { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Backfill: set each family member's <see cref="ProjectContract"/> from their sponsoring employee.
/// Reads FK links from Visa2026 SQL (<c>People</c>); writes via OData only.
/// </summary>
internal static class Visa2014FamilyMemberProjectContractSync
{
    private const string ListRowsNeedingPatchSql = """
        SELECT
            CAST(f.ID AS varchar(36)) AS FamilyId,
            CAST(s.ProjectContractID AS varchar(36)) AS SponsorContractId
        FROM People f
        INNER JOIN People s ON f.SponsoringEmployeeID = s.ID AND s.GCRecord = 0
        WHERE f.GCRecord = 0
          AND f.IsEmployee = 0
          AND f.SponsoringEmployeeID IS NOT NULL
          AND s.ProjectContractID IS NOT NULL
          AND (f.ProjectContractID IS NULL OR f.ProjectContractID <> s.ProjectContractID)
        ORDER BY f.ID
        """;

    public static async Task<Visa2014FamilyMemberProjectContractSyncResult> RunAsync(
        ApiClient api,
        string targetConnectionString,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var rows = await ListRowsNeedingPatchAsync(targetConnectionString, maxRows);

        var errors = new List<string>();
        int patched = 0;
        int failed = 0;

        foreach (var (familyId, sponsorContractId) in rows)
        {
            if (dryRun)
            {
                Console.WriteLine($"DRY RUN: PATCH Person {familyId} ProjectContract ← {sponsorContractId}");
                patched++;
                continue;
            }

            try
            {
                await api.UpdateAsync("Person", familyId, new Dictionary<string, object?>
                {
                    ["ProjectContract"] = new { ID = sponsorContractId },
                });
                patched++;
                if (verbose)
                    Console.WriteLine($"  PATCH Person {familyId} ProjectContract ← {sponsorContractId}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{familyId}: {ex.Message}");
            }
        }

        return new Visa2014FamilyMemberProjectContractSyncResult
        {
            FamilyMembersScanned = rows.Count,
            Patched = patched,
            SkippedAlreadySynced = 0,
            SkippedNoPersonMap = 0,
            SkippedNoSponsorMap = 0,
            SkippedNoSponsorContract = 0,
            Failed = failed,
            Errors = errors,
        };
    }

    private static async Task<List<(Guid FamilyId, Guid SponsorContractId)>> ListRowsNeedingPatchAsync(
        string targetConnectionString,
        int? maxRows)
    {
        var sql = maxRows is > 0
            ? ListRowsNeedingPatchSql.Replace("SELECT", $"SELECT TOP ({maxRows.Value})", StringComparison.Ordinal)
            : ListRowsNeedingPatchSql;

        var rows = new List<(Guid, Guid)>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var familyId))
                continue;
            if (!Guid.TryParse(reader.GetString(1), out var sponsorContractId))
                continue;
            rows.Add((familyId, sponsorContractId));
        }

        return rows;
    }
}
