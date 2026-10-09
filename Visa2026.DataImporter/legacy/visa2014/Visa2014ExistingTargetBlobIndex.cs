using Npgsql;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Reads file bytes already stored on the Visa2026 target so a file catch-up
/// can skip them. Does not write.
/// </summary>
internal static class Visa2014ExistingTargetBlobIndex
{
    public static async Task<HashSet<Guid>> LoadPersonIdsWithPhotoAsync(
        string targetConnection,
        CancellationToken cancellationToken = default)
    {
        var ids = new HashSet<Guid>();
        await using var connection = Open(targetConnection);
        await using var command = new NpgsqlCommand(
            """
            SELECT "ID"
            FROM "People"
            WHERE "GCRecord" = 0
              AND "Photo" IS NOT NULL
              AND octet_length("Photo") > 0
            """,
            connection);
        command.CommandTimeout = 120;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetGuid(0));
        return ids;
    }

    public static async Task<int> SeedAsync(
        string? targetConnection,
        string documentTable,
        string parentIdColumn,
        HashSet<string> importedBlobKeys,
        Dictionary<Guid, int>? copyIndexByParent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetConnection) || !DatabaseProviderDetector.IsPostgreSql(targetConnection))
            return 0;

        RequireIdentifier(documentTable);
        RequireIdentifier(parentIdColumn);

        var sql = $"""
            SELECT d."{parentIdColumn}", octet_length(f."Content"), encode(sha256(f."Content"), 'hex')
            FROM "{documentTable}" d
            INNER JOIN "FileData" f ON f."ID" = d."FileID"
            WHERE d."GCRecord" = 0
              AND f."Content" IS NOT NULL
              AND octet_length(f."Content") > 0
            """;

        var seeded = 0;
        await using var connection = Open(targetConnection);
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 900;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var parentId = reader.GetGuid(0);
            var length = reader.GetInt32(1);
            var hex = reader.GetString(2);
            importedBlobKeys.Add(Visa2014LegacyBlobDedupeHelper.BuildKey(parentId, length, hex));
            if (copyIndexByParent != null)
            {
                copyIndexByParent[parentId] = copyIndexByParent.TryGetValue(parentId, out var current)
                    ? current + 1
                    : 1;
            }

            seeded++;
        }

        return seeded;
    }

    private static NpgsqlConnection Open(string targetConnection)
    {
        var connection = new NpgsqlConnection(DatabaseProviderDetector.StripEfCoreProvider(targetConnection));
        connection.Open();
        return connection;
    }

    private static void RequireIdentifier(string name)
    {
        if (name.Length == 0 || name.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new ArgumentException($"Unsafe SQL identifier '{name}'.", nameof(name));
    }
}