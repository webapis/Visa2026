using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Data.SqlClient;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves ministry-leg counts for progress synthesis from frozen
/// <see cref="Bo.ApplicationApprovalLegSnapshot"/> rows, falling back to
/// <see cref="Bo.ApprovalLegProfile"/> leg definitions when snapshots are empty.
/// </summary>
internal static class Visa2014ApplicationMinistryLegCountResolver
{
    private const string LegCountSql = """
        SELECT CAST(a.ID AS varchar(36)) AS ApplicationId,
            CASE
                WHEN ISNULL(snap.SnapshotLegCount, 0) > 0 THEN snap.SnapshotLegCount
                ELSE ISNULL(prof.ProfileLegCount, 0)
            END AS LegCount
        FROM dbo.Applications a
        INNER JOIN dbo.ApplicationTypes at ON at.ID = a.ApplicationTypeID
        LEFT JOIN (
            SELECT s.ApplicationId, COUNT(*) AS SnapshotLegCount
            FROM dbo.ApplicationApprovalLegSnapshots s
            WHERE (s.GCRecord IS NULL OR s.GCRecord = 0)
              AND s.MinistryShortName IS NOT NULL
              AND LEN(LTRIM(RTRIM(s.MinistryShortName))) > 0
            GROUP BY s.ApplicationId
        ) snap ON snap.ApplicationId = a.ID
        LEFT JOIN (
            SELECT ml.ApprovalLegProfileID, COUNT(*) AS ProfileLegCount
            FROM dbo.ApprovalLegProfileMinistryLegs ml
            WHERE ml.ApprovingMinistryID IS NOT NULL
            GROUP BY ml.ApprovalLegProfileID
        ) prof ON prof.ApprovalLegProfileID = a.ApprovalLegProfileID
        WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
          AND at.ApplicationProgressRoute = 0
          AND a.ApprovalLegProfileID IS NOT NULL
        """;

    public static IReadOnlyDictionary<Guid, int> LoadFromObjectSpace(INonSecuredObjectSpaceFactory objectSpaceFactory)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.Application));
        var viaMinistryTypeIds = objectSpace.GetObjectsQuery<Bo.ApplicationType>()
            .Where(t => t.ApplicationProgressRoute == Bo.ApplicationProgressRouteKind.ViaMinistries)
            .Select(t => t.ID)
            .ToHashSet();

        var profileLegCounts = objectSpace.GetObjectsQuery<Bo.ApprovalLegProfileMinistryLeg>()
            .Where(l => l.ApprovingMinistry != null && l.ApprovalLegProfile != null)
            .AsEnumerable()
            .GroupBy(l => l.ApprovalLegProfile!.ID)
            .ToDictionary(g => g.Key, g => g.Count());

        var map = new Dictionary<Guid, int>();
        foreach (var application in objectSpace.GetObjectsQuery<Bo.Application>()
                     .Where(a => a.ApplicationType != null && viaMinistryTypeIds.Contains(a.ApplicationType.ID))
                     .Where(a => a.ApprovalLegProfile != null)
                     .AsEnumerable())
        {
            var legCount = ResolveLegCount(application, profileLegCounts);
            if (legCount > 0)
                map[application.ID] = legCount;
        }

        return map;
    }

    public static async Task<IReadOnlyDictionary<Guid, int>> LoadFromSqlAsync(string targetConnectionString)
    {
        var map = new Dictionary<Guid, int>();
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(LegCountSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!Guid.TryParse(reader.GetString(0), out var applicationId))
                continue;

            var legCount = reader.GetInt32(1);
            if (legCount > 0)
                map[applicationId] = legCount;
        }

        return map;
    }

    internal static int ResolveLegCount(
        Bo.Application application,
        IReadOnlyDictionary<Guid, int>? profileLegCountsByProfileId = null)
    {
        var snapshotCount = application.ApprovalLegSnapshots?
            .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;
        if (snapshotCount > 0)
            return snapshotCount;

        if (application.ApprovalLegProfile == null)
            return 0;

        if (profileLegCountsByProfileId != null
            && profileLegCountsByProfileId.TryGetValue(application.ApprovalLegProfile.ID, out var fromMap))
            return fromMap;

        return Bo.ApprovalLegProfileMinistryHelper.GetLegCount(application.ApprovalLegProfile);
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

    /// <summary>
    /// Via-ministry apps with profile legs but no ministry review rows in progress history.
    /// </summary>
    public static HashSet<Guid> ResolveTargetApplicationIdsMissingMinistryProgress(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IReadOnlyDictionary<Guid, int> targetApplicationIdToLegCount)
    {
        if (targetApplicationIdToLegCount.Count == 0)
            return [];

        var candidateIds = targetApplicationIdToLegCount.Keys.ToHashSet();
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProgress));
        var reviewStateCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "1_REVIEW_STARTED", "1_REVIEW_APPROVED", "1_REVIEW_REJECTED",
            "2_REVIEW_STARTED", "2_REVIEW_APPROVED", "2_REVIEW_REJECTED",
            "3_REVIEW_STARTED", "3_REVIEW_APPROVED", "3_REVIEW_REJECTED",
            "4_REVIEW_STARTED", "4_REVIEW_APPROVED", "4_REVIEW_REJECTED",
            "5_REVIEW_STARTED", "5_REVIEW_APPROVED", "5_REVIEW_REJECTED",
        };

        var appsWithReview = objectSpace.GetObjectsQuery<Bo.ApplicationProgress>()
            .Where(p => p.Application != null && candidateIds.Contains(p.Application.ID))
            .Select(p => new { AppId = p.Application!.ID, StateCode = p.State != null ? p.State.Code : null })
            .AsEnumerable()
            .Where(p => !string.IsNullOrWhiteSpace(p.StateCode) && reviewStateCodes.Contains(p.StateCode!))
            .Select(p => p.AppId)
            .ToHashSet();

        return candidateIds
            .Where(id => targetApplicationIdToLegCount[id] > 0 && !appsWithReview.Contains(id))
            .ToHashSet();
    }
}
