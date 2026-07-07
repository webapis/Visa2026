using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyAuditChangeQuery
{
    private const string ChangedGuidsSql = """
        SELECT DISTINCT CAST(w.GuidId AS uniqueidentifier) AS LegacyOid
        FROM dbo.AuditDataItemPersistent a
        INNER JOIN dbo.AuditedObjectWeakReference w
            ON a.AuditedObject = w.Oid
        WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
          AND (w.GCRecord IS NULL OR w.GCRecord = 0)
          AND a.ModifiedOn >= @sinceUtc
        """;

    public static async Task<HashSet<Guid>> LoadChangedLegacyOidsAsync(
        string legacyConnectionString,
        DateTime sinceUtc,
        IReadOnlyCollection<Guid>? restrictToLegacyOids,
        CancellationToken cancellationToken = default)
    {
        var result = new HashSet<Guid>();
        if (sinceUtc <= DateTime.UnixEpoch)
            return result;

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(ChangedGuidsSql, connection);
        command.Parameters.AddWithValue("@sinceUtc", sinceUtc);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
                continue;

            var legacyOid = reader.GetGuid(0);
            if (restrictToLegacyOids != null && !restrictToLegacyOids.Contains(legacyOid))
                continue;

            result.Add(legacyOid);
        }

        return result;
    }
}
