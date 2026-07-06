using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyTableColumnResolver
{
    public static async Task<string?> FindColumnNameAsync(
        SqlConnection connection,
        string tableName,
        string namePrefix)
    {
        const string sql = """
            SELECT TOP 1 c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(@table)
              AND c.name LIKE @prefix + '%'
            ORDER BY c.name
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@table", tableName);
        command.Parameters.AddWithValue("@prefix", namePrefix);

        var scalar = await command.ExecuteScalarAsync();
        return scalar is string name && !string.IsNullOrWhiteSpace(name) ? name : null;
    }

    public static string Bracket(string columnName) =>
        $"[{columnName.Replace("]", "]]")}]";
}
