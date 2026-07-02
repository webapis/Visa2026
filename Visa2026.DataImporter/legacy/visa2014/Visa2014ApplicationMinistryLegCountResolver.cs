using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves ministry-leg counts from frozen <see cref="Bo.ApplicationApprovalLegSnapshot"/> rows.
/// </summary>
internal static class Visa2014ApplicationMinistryLegCountResolver
{
    private const string SnapshotLegCountSql = """
        SELECT CAST(a.ID AS varchar(36)) AS ApplicationId, COUNT(s.ID) AS LegCount
        FROM dbo.Applications a
        INNER JOIN dbo.ApplicationTypes at ON at.ID = a.ApplicationTypeID
        LEFT JOIN dbo.ApplicationApprovalLegSnapshots s
            ON s.ApplicationId = a.ID AND s.GCRecord = 0
        WHERE a.GCRecord = 0
          AND at.ApplicationProgressRoute = 0
          AND a.ApprovalLegProfileID IS NOT NULL
        GROUP BY a.ID
        """;

    public static IReadOnlyDictionary<Guid, int> LoadFromObjectSpace(INonSecuredObjectSpaceFactory objectSpaceFactory)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        var viaMinistryTypeIds = objectSpace.GetObjectsQuery<Bo.ApplicationType>()
            .Where(t => t.ApplicationProgressRoute == Bo.ApplicationProgressRouteKind.ViaMinistries)
            .Select(t => t.ID)
            .ToHashSet();

        return objectSpace.GetObjectsQuery<Bo.Application>()
            .Where(a => a.ApplicationType != null && viaMinistryTypeIds.Contains(a.ApplicationType.ID))
            .Where(a => a.ApprovalLegProfile != null)
            .Select(a => new
            {
                a.ID,
                LegCount = a.ApprovalLegSnapshots == null
                    ? 0
                    : a.ApprovalLegSnapshots.Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)),
            })
            .AsEnumerable()
            .ToDictionary(x => x.ID, x => x.LegCount);
    }

    public static async Task<IReadOnlyDictionary<Guid, int>> LoadFromSqlAsync(string targetConnectionString)
    {
        var map = new Dictionary<Guid, int>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(SnapshotLegCountSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var applicationId))
                continue;
            map[applicationId] = reader.GetInt32(1);
        }

        return map;
    }

    public static Dictionary<Guid, int> MapLegacyLegCounts(
        IReadOnlyDictionary<Guid, Guid> legacyToTargetApplicationId,
        IReadOnlyDictionary<Guid, int> targetApplicationIdToLegCount)
    {
        var map = new Dictionary<Guid, int>();
        foreach (var (legacyOid, targetId) in legacyToTargetApplicationId)
        {
            if (targetApplicationIdToLegCount.TryGetValue(targetId, out var legCount) && legCount > 0)
                map[legacyOid] = legCount;
        }

        return map;
    }

    public static HashSet<Guid> ResolveTargetApplicationIdsInScope(
        IReadOnlyDictionary<Guid, Guid> legacyToTargetApplicationId,
        IReadOnlyDictionary<Guid, int> targetApplicationIdToLegCount) =>
        legacyToTargetApplicationId
            .Where(kv => targetApplicationIdToLegCount.TryGetValue(kv.Value, out var legs) && legs > 0)
            .Select(kv => kv.Value)
            .ToHashSet();
}
