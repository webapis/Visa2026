using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Prevents sync from inserting a second <c>ApplicationItem</c> for the same
/// (Application, Person) when the legacy Oid is missing from the id-map.
/// </summary>
internal sealed class Visa2014ApplicationItemPersonDuplicateGuard
{
    private const string CanonicalPairsSql = """
        SELECT CAST(ApplicationID AS varchar(36)) AS ApplicationId,
               CAST(PersonID AS varchar(36)) AS PersonId,
               CAST(MIN(ID) AS varchar(36)) AS ItemId
        FROM dbo.ApplicationItems
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND ApplicationID IS NOT NULL
          AND PersonID IS NOT NULL
        GROUP BY ApplicationID, PersonID
        """;

    private readonly Dictionary<(Guid ApplicationId, Guid PersonId), Guid> _canonicalByPair = new();

    public int LoadedPairCount => _canonicalByPair.Count;

    public static async Task<Visa2014ApplicationItemPersonDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014ApplicationItemPersonDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CanonicalPairsSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Guid.TryParse(reader.GetString(0), out var applicationId)
                || !Guid.TryParse(reader.GetString(1), out var personId)
                || !Guid.TryParse(reader.GetString(2), out var itemId))
                continue;

            guard.Register(applicationId, personId, itemId);
        }

        if (verbose)
            Console.WriteLine($"INF ApplicationItem person-duplicate guard: {guard.LoadedPairCount} (Application, Person) pair(s)");

        return guard;
    }

    public bool TryGetCanonical(Guid applicationId, Guid personId, out Guid applicationItemId) =>
        _canonicalByPair.TryGetValue((applicationId, personId), out applicationItemId);

    public Guid? TryResolveFromPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (!TryResolveParentIds(payload, out var applicationId, out var personId))
            return null;

        return TryGetCanonical(applicationId, personId, out var itemId) ? itemId : null;
    }

    public void Register(Guid applicationId, Guid personId, Guid applicationItemId)
    {
        var key = (applicationId, personId);
        if (!_canonicalByPair.TryGetValue(key, out var existing) || applicationItemId.CompareTo(existing) < 0)
            _canonicalByPair[key] = applicationItemId;
    }

    internal static bool TryResolveParentIds(
        IReadOnlyDictionary<string, object?> payload,
        out Guid applicationId,
        out Guid personId)
    {
        applicationId = default;
        personId = default;
        return TryGetPayloadFkId(payload, "Application", out applicationId)
               && TryGetPayloadFkId(payload, "Person", out personId);
    }

    private static bool TryGetPayloadFkId(IReadOnlyDictionary<string, object?> payload, string key, out Guid id) =>
        Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, key, out id);
}
