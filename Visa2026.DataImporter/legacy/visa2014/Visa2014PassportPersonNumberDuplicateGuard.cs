using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PassportPersonNumberDuplicateGuard
{
    private const string CanonicalPairsSql = """
        SELECT CAST(PersonID AS varchar(36)) AS PersonId,
               LTRIM(RTRIM(PassportNumber)) AS PassportNumber,
               CAST(MIN(ID) AS varchar(36)) AS PassportId
        FROM dbo.Passports
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND PersonID IS NOT NULL
          AND NULLIF(LTRIM(RTRIM(PassportNumber)), '') IS NOT NULL
        GROUP BY PersonID, LTRIM(RTRIM(PassportNumber))
        """;

    private readonly Dictionary<(Guid PersonId, string PassportNumber), Guid> _canonicalByPair = new();

    public int LoadedPairCount => _canonicalByPair.Count;

    public static async Task<Visa2014PassportPersonNumberDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014PassportPersonNumberDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CanonicalPairsSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Guid.TryParse(reader.GetString(0), out var personId))
                continue;

            var passportNumber = reader.GetString(1);
            if (!Guid.TryParse(reader.GetString(2), out var passportId))
                continue;

            guard.Register(personId, passportNumber, passportId);
        }

        if (verbose)
            Console.WriteLine($"INF Passport duplicate guard: {guard.LoadedPairCount} (Person, PassportNumber) pair(s)");

        return guard;
    }

    public Guid? TryResolveFromPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return null;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "PassportNumber", out var passportNumber))
            return null;

        return _canonicalByPair.TryGetValue((personId, passportNumber), out var passportId) ? passportId : null;
    }

    public void Register(Guid personId, string passportNumber, Guid passportId)
    {
        var key = (personId, passportNumber.Trim());
        if (!_canonicalByPair.TryGetValue(key, out var existing) || passportId.CompareTo(existing) < 0)
            _canonicalByPair[key] = passportId;
    }

    public void RegisterFromPayload(IReadOnlyDictionary<string, object?> payload, Guid passportId)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "PassportNumber", out var passportNumber))
            return;

        Register(personId, passportNumber, passportId);
    }
}
