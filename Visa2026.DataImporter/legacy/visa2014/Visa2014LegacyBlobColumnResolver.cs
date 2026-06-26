using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyBlobColumnResolver
{
    private const int VarbinarySystemTypeId = 165;

    public static string GetVarbinaryColumnName(SqlConnection connection, string tableName)
    {
        using var command = new SqlCommand(
            """
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(@table)
              AND c.system_type_id = @varbinaryType
            ORDER BY c.column_id
            """,
            connection);
        command.Parameters.AddWithValue("@table", tableName);
        command.Parameters.AddWithValue("@varbinaryType", VarbinarySystemTypeId);

        var names = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            names.Add(reader.GetString(0));

        if (names.Count == 0)
            throw new InvalidOperationException($"No varbinary column on {tableName}.");

        // PassportCopy also has Education FK — first varbinary is the scan blob (Göçürme).
        return names[0];
    }
}
