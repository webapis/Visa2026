using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Prevents supplement Person inserts when an active row with the same business key already exists
/// (PersonalNumber, or FirstName + LastName + DateOfBirth when PN is sentinel <c>0</c>).
/// </summary>
internal sealed class Visa2014PersonIdentityDuplicateGuard
{
    private const string CanonicalPersonalNumberSql = """
        SELECT LTRIM(RTRIM(PersonalNumber)) AS PersonalNumber,
               CAST(MIN(ID) AS varchar(36)) AS PersonId
        FROM dbo.People
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND NULLIF(LTRIM(RTRIM(PersonalNumber)), '') IS NOT NULL
          AND PersonalNumber <> N'0'
        GROUP BY LTRIM(RTRIM(PersonalNumber))
        """;

    private const string CanonicalIdentitySql = """
        SELECT UPPER(LTRIM(RTRIM(FirstName))) AS FirstName,
               UPPER(LTRIM(RTRIM(LastName))) AS LastName,
               CAST(DateOfBirth AS date) AS DateOfBirth,
               CAST(MIN(ID) AS varchar(36)) AS PersonId
        FROM dbo.People
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND FirstName IS NOT NULL AND LTRIM(RTRIM(FirstName)) <> N''
          AND LastName IS NOT NULL AND LTRIM(RTRIM(LastName)) <> N''
          AND DateOfBirth IS NOT NULL
          AND (PersonalNumber IS NULL OR LTRIM(RTRIM(PersonalNumber)) = N'' OR PersonalNumber = N'0')
        GROUP BY UPPER(LTRIM(RTRIM(FirstName))), UPPER(LTRIM(RTRIM(LastName))), CAST(DateOfBirth AS date)
        """;

    private readonly Dictionary<string, Guid> _canonicalByPersonalNumber = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _canonicalByIdentity = new(StringComparer.Ordinal);

    public int LoadedPersonalNumberCount => _canonicalByPersonalNumber.Count;
    public int LoadedIdentityCount => _canonicalByIdentity.Count;

    public static async Task<Visa2014PersonIdentityDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014PersonIdentityDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(CanonicalPersonalNumberSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var personalNumber = reader.GetString(0);
                if (!Guid.TryParse(reader.GetString(1), out var personId))
                    continue;

                guard.RegisterPersonalNumber(personalNumber, personId);
            }
        }

        await using (var command = new SqlCommand(CanonicalIdentitySql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var firstName = reader.GetString(0);
                var lastName = reader.GetString(1);
                var dateOfBirth = reader.GetDateTime(2);
                if (!Guid.TryParse(reader.GetString(3), out var personId))
                    continue;

                guard.RegisterIdentity(BuildIdentityKey(firstName, lastName, dateOfBirth), personId);
            }
        }

        if (verbose)
        {
            Console.WriteLine(
                $"INF Person identity duplicate guard: {guard.LoadedPersonalNumberCount} PersonalNumber key(s), " +
                $"{guard.LoadedIdentityCount} identity key(s)");
        }

        return guard;
    }

    public Guid? TryResolveFromImportRow(IReadOnlyDictionary<string, object?> row)
    {
        var personalNumber = row.GetValueOrDefault("PersonalNumber") as string;
        var normalized = Visa2014PersonTransform.NormalizePersonalNumber(personalNumber);
        if (!Visa2014PersonTransform.IsSentinelPersonalNumber(normalized))
        {
            return _canonicalByPersonalNumber.TryGetValue(normalized.Trim(), out var personId)
                ? personId
                : null;
        }

        if (row.GetValueOrDefault("FirstName") is not string firstName ||
            row.GetValueOrDefault("LastName") is not string lastName ||
            row.GetValueOrDefault("DateOfBirth") is not DateTime dateOfBirth)
        {
            return null;
        }

        var identityKey = BuildIdentityKey(
            firstName.Trim().ToUpperInvariant(),
            lastName.Trim().ToUpperInvariant(),
            dateOfBirth.Date);
        return _canonicalByIdentity.TryGetValue(identityKey, out var existingId) ? existingId : null;
    }

    public void RegisterFromImportRow(IReadOnlyDictionary<string, object?> row, Guid personId)
    {
        var personalNumber = row.GetValueOrDefault("PersonalNumber") as string;
        var normalized = Visa2014PersonTransform.NormalizePersonalNumber(personalNumber);
        if (!Visa2014PersonTransform.IsSentinelPersonalNumber(normalized))
        {
            RegisterPersonalNumber(normalized.Trim(), personId);
            return;
        }

        if (row.GetValueOrDefault("FirstName") is not string firstName ||
            row.GetValueOrDefault("LastName") is not string lastName ||
            row.GetValueOrDefault("DateOfBirth") is not DateTime dateOfBirth)
        {
            return;
        }

        RegisterIdentity(
            BuildIdentityKey(
                firstName.Trim().ToUpperInvariant(),
                lastName.Trim().ToUpperInvariant(),
                dateOfBirth.Date),
            personId);
    }

    internal static string BuildIdentityKey(string firstNameUpper, string lastNameUpper, DateTime dateOfBirth) =>
        $"{firstNameUpper}|{lastNameUpper}|{dateOfBirth:yyyy-MM-dd}";

    private void RegisterPersonalNumber(string personalNumber, Guid personId)
    {
        if (!_canonicalByPersonalNumber.TryGetValue(personalNumber, out var existing) ||
            personId.CompareTo(existing) < 0)
        {
            _canonicalByPersonalNumber[personalNumber] = personId;
        }
    }

    private void RegisterIdentity(string identityKey, Guid personId)
    {
        if (!_canonicalByIdentity.TryGetValue(identityKey, out var existing) ||
            personId.CompareTo(existing) < 0)
        {
            _canonicalByIdentity[identityKey] = personId;
        }
    }
}