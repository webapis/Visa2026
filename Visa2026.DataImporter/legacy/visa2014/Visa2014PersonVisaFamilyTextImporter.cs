using Microsoft.Data.SqlClient;
using Visa2026.Module.Services;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonVisaFamilyTextImportResult
{
    public int IdMapEntries { get; init; }
    public int Processed { get; init; }
    public int Patched { get; init; }
    public int SkippedNotEmployee { get; init; }
    public int SkippedNoText { get; init; }
    public int PatchedSingleNone { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonVisaFamilyTextImporter
{
    private const string ReadLegacySql = """
        SELECT
            CASE WHEN p.IsEmployee = 1 THEN 1 ELSE 0 END AS IsEmployee,
            CAST(ms.Status AS varchar(10)) AS MaritalStatusStatus,
            ms.StatusL
        FROM dbo.Person p
        LEFT JOIN dbo.MaritalStatus ms ON p.MaritalStatus = ms.Oid
        WHERE p.Oid = @oid AND p.GCRecord IS NULL
        """;

    public static async Task<Visa2014PersonVisaFamilyTextImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string idMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        if (!File.Exists(idMapPath))
            throw new FileNotFoundException("Person id-map not found. Run scalar --import-visa2014 first.", idMapPath);

        var idMap = Visa2014IdMapHelper.Load(idMapPath);
        var entries = maxRows is > 0
            ? idMap.Select(kvp => kvp).Take(maxRows.Value).ToList()
            : idMap.Select(kvp => kvp).ToList();

        var errors = new List<string>();
        int patched = 0;
        int failed = 0;
        int skippedNotEmployee = 0;
        int skippedNoText = 0;
        int patchedSingleNone = 0;

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync();

        foreach (var (legacyOid, targetId) in entries)
        {
            LegacyEmployeeStatusL? legacy;
            try
            {
                legacy = await ReadLegacyEmployeeStatusLAsync(connection, legacyOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: SQL read failed — {ex.Message}");
                continue;
            }

            if (legacy == null)
            {
                skippedNotEmployee++;
                continue;
            }

            if (!legacy.IsEmployee)
            {
                skippedNotEmployee++;
                continue;
            }

            var familyText = Visa2014FamilyMembersTextMapper.FromLegacyStatusL(
                legacy.StatusL,
                legacy.MaritalStatusStatus);

            if (familyText == null)
            {
                skippedNoText++;
                continue;
            }

            if (dryRun)
            {
                if (VisaFamilyMemberLinesHelper.IsNoneValue(familyText))
                    patchedSingleNone++;
                Console.WriteLine($"DRY RUN: PATCH Person {targetId} ← legacy {legacyOid}");
                patched++;
                continue;
            }

            try
            {
                await target.UpdateAsync(typeof(Bo.Person), targetId, new Dictionary<string, object?>
                {
                    ["VisaApplicationFamilyMembersText"] = familyText,
                });
                patched++;
                if (VisaFamilyMemberLinesHelper.IsNoneValue(familyText))
                    patchedSingleNone++;
                if (verbose)
                    Console.WriteLine($"  PATCH Person {targetId} ← legacy {legacyOid} (StatusL)");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
            }
        }

        return new Visa2014PersonVisaFamilyTextImportResult
        {
            IdMapEntries = idMap.Count,
            Processed = entries.Count,
            Patched = patched,
            SkippedNotEmployee = skippedNotEmployee,
            SkippedNoText = skippedNoText,
            PatchedSingleNone = patchedSingleNone,
            Failed = failed,
            Errors = errors,
        };
    }

    private sealed record LegacyEmployeeStatusL(bool IsEmployee, string? MaritalStatusStatus, string? StatusL);

    private static async Task<LegacyEmployeeStatusL?> ReadLegacyEmployeeStatusLAsync(
        SqlConnection connection,
        Guid legacyOid)
    {
        await using var command = new SqlCommand(ReadLegacySql, connection);
        command.Parameters.AddWithValue("@oid", legacyOid);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var isEmployee = reader.GetInt32(0) == 1;
        var status = reader.IsDBNull(1) ? null : reader.GetString(1);
        var statusL = reader.IsDBNull(2) ? null : reader.GetString(2);
        return new LegacyEmployeeStatusL(isEmployee, status, statusL);
    }
}
