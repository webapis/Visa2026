using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacySoftDeleteQuery
{
    private static readonly IReadOnlyDictionary<string, string> EntityLegacyTables =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person"] = "Person",
            ["Passport"] = "Passport",
            ["Visa"] = "Visa",
            ["Education"] = "Education",
            ["EmployeePositionHistory"] = "WorkHistoryOfEmployee",
            ["EmployeeSalary"] = "Employee",
            ["AddressOfResidence"] = "AddressOfResidence",
            ["Application"] = "Application",
            ["ApplicationItem"] = "PersonInApplication",
            ["ApplicationProgress"] = "Application",
            ["WorkPermit"] = "WorkPermit",
            ["WorkPermitItem"] = "WorkPermit",
            ["Invitation"] = "ApplicationResult",
            ["InvitationItem"] = "PersonInInvitation",
        };

    public static bool TryGetLegacyTable(string entityName, out string tableName) =>
        EntityLegacyTables.TryGetValue(entityName, out tableName!);

    public static async Task<IReadOnlyList<Guid>> LoadSoftDeletedLegacyOidsAsync(
        string legacyConnectionString,
        string legacyTable,
        IReadOnlyCollection<Guid> candidateLegacyOids,
        CancellationToken cancellationToken = default)
    {
        if (candidateLegacyOids.Count == 0)
            return [];

        var deleted = new List<Guid>();
        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var chunk in candidateLegacyOids.Chunk(500))
        {
            var paramNames = chunk.Select((_, i) => $"@p{i}").ToArray();
            var sql = BuildSoftDeletedQuery(legacyTable, paramNames);

            await using var command = new SqlCommand(sql, connection);
            for (int i = 0; i < chunk.Length; i++)
                command.Parameters.AddWithValue(paramNames[i], chunk[i]);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                    deleted.Add(reader.GetGuid(0));
            }
        }

        return deleted;
    }

    private static string BuildSoftDeletedQuery(string legacyTable, IReadOnlyList<string> paramNames)
    {
        var inClause = string.Join(", ", paramNames);

        if (string.Equals(legacyTable, "Employee", StringComparison.OrdinalIgnoreCase))
        {
            return $"""
                SELECT CAST(e.Oid AS uniqueidentifier) AS LegacyOid
                FROM dbo.Employee e
                INNER JOIN dbo.Person p ON p.Oid = e.Oid
                WHERE (p.GCRecord IS NOT NULL AND p.GCRecord <> 0)
                  AND e.Oid IN ({inClause})
                """;
        }

        return $"""
            SELECT CAST(Oid AS uniqueidentifier) AS LegacyOid
            FROM dbo.[{legacyTable}]
            WHERE (GCRecord IS NOT NULL AND GCRecord <> 0)
              AND Oid IN ({inClause})
            """;
    }
}