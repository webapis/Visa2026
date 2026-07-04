using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyAuditIssueDateHelper
{
    private const string MinObjectCreatedSql = """
        SELECT MIN(a.ModifiedOn)
        FROM dbo.AuditedObjectWeakReference w
        INNER JOIN dbo.AuditDataItemPersistent a
            ON a.AuditedObject = w.Oid AND a.GCRecord IS NULL
        WHERE w.GuidId IN (@oid1, @oid2)
          AND a.OperationType = N'ObjectCreated'
        """;

    public static async Task<DateTime?> TryGetUploadIssueDateAsync(
        SqlConnection connection,
        Guid copyOid,
        Guid fileDataOid,
        CancellationToken cancellationToken = default)
    {
        await using var command = new SqlCommand(MinObjectCreatedSql, connection);
        command.Parameters.AddWithValue("@oid1", copyOid);
        command.Parameters.AddWithValue("@oid2", fileDataOid);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is DBNull or null)
            return null;

        return value is DateTime dt ? dt.Date : null;
    }
}
