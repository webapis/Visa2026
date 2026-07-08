using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014WorkPermitItemPersonDuplicateGuard
{
    private const string CanonicalPairsSql = """
        SELECT CAST(WorkPermitID AS varchar(36)) AS WorkPermitId,
               CAST(PersonID AS varchar(36)) AS PersonId,
               CAST(MIN(ID) AS varchar(36)) AS ItemId
        FROM dbo.WorkPermitItems
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND WorkPermitID IS NOT NULL
          AND PersonID IS NOT NULL
        GROUP BY WorkPermitID, PersonID
        """;

    private readonly Dictionary<(Guid WorkPermitId, Guid PersonId), Guid> _canonicalByPair = new();

    public int LoadedPairCount => _canonicalByPair.Count;

    public static async Task<Visa2014WorkPermitItemPersonDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014WorkPermitItemPersonDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CanonicalPairsSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Guid.TryParse(reader.GetString(0), out var workPermitId)
                || !Guid.TryParse(reader.GetString(1), out var personId)
                || !Guid.TryParse(reader.GetString(2), out var itemId))
                continue;

            guard.Register(workPermitId, personId, itemId);
        }

        if (verbose)
            Console.WriteLine($"INF WorkPermitItem duplicate guard: {guard.LoadedPairCount} (WorkPermit, Person) pair(s)");

        return guard;
    }

    public Guid? TryResolveFromPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "WorkPermit", out var workPermitId))
            return null;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return null;

        return _canonicalByPair.TryGetValue((workPermitId, personId), out var itemId) ? itemId : null;
    }

    public void Register(Guid workPermitId, Guid personId, Guid itemId)
    {
        var key = (workPermitId, personId);
        if (!_canonicalByPair.TryGetValue(key, out var existing) || itemId.CompareTo(existing) < 0)
            _canonicalByPair[key] = itemId;
    }

    public void RegisterFromPayload(IReadOnlyDictionary<string, object?> payload, Guid itemId)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "WorkPermit", out var workPermitId))
            return;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return;

        Register(workPermitId, personId, itemId);
    }
}
