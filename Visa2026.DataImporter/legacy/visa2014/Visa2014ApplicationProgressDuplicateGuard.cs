using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Prevents sync from inserting a second <c>ApplicationProgress</c> for the same
/// (Application, ProgressOrder) when the synthetic id-map key is missing.
/// </summary>
internal sealed class Visa2014ApplicationProgressDuplicateGuard
{
    private const string CanonicalPairsSql = """
        SELECT CAST(ApplicationID AS varchar(36)) AS ApplicationId,
               ProgressOrder,
               CAST(MIN(ID) AS varchar(36)) AS ProgressId
        FROM dbo.ApplicationProgresses
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND ApplicationID IS NOT NULL
        GROUP BY ApplicationID, ProgressOrder
        """;

    private readonly Dictionary<(Guid ApplicationId, int Order), Guid> _canonicalByPair = new();

    public int LoadedPairCount => _canonicalByPair.Count;

    public static async Task<Visa2014ApplicationProgressDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014ApplicationProgressDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CanonicalPairsSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Guid.TryParse(reader.GetString(0), out var applicationId))
                continue;

            var order = reader.GetInt32(1);
            if (!Guid.TryParse(reader.GetString(2), out var progressId))
                continue;

            guard.Register(applicationId, order, progressId);
        }

        if (verbose)
            Console.WriteLine($"INF ApplicationProgress duplicate guard: {guard.LoadedPairCount} (Application, Order) pair(s)");

        return guard;
    }

    public Guid? TryResolveFromPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Application", out var applicationId))
            return null;
        if (!TryGetOrder(payload, out var order))
            return null;

        return _canonicalByPair.TryGetValue((applicationId, order), out var progressId) ? progressId : null;
    }

    public void Register(Guid applicationId, int order, Guid progressId)
    {
        var key = (applicationId, order);
        if (!_canonicalByPair.TryGetValue(key, out var existing) || progressId.CompareTo(existing) < 0)
            _canonicalByPair[key] = progressId;
    }

    public void RegisterFromPayload(IReadOnlyDictionary<string, object?> payload, Guid progressId)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Application", out var applicationId))
            return;
        if (!TryGetOrder(payload, out var order))
            return;

        Register(applicationId, order, progressId);
    }

    private static bool TryGetOrder(IReadOnlyDictionary<string, object?> payload, out int order)
    {
        order = 0;
        if (!payload.TryGetValue("Order", out var raw) || raw == null)
            return false;

        try
        {
            order = Convert.ToInt32(raw);
            return order > 0;
        }
        catch
        {
            return false;
        }
    }
}