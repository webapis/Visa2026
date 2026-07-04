using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ProjectContract.ApprovalLegProfileId"/> FK column.
/// </summary>
public static class ProjectContractApprovalLegProfileSchemaSql
{
    internal const string EnsureApprovalLegProfileIdColumnSql = """
        IF OBJECT_ID(N'dbo.ProjectContracts', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ProjectContracts', N'ApprovalLegProfileId') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.ProjectContracts ADD ApprovalLegProfileId uniqueidentifier NULL;
        """;

    internal const string EnsureApprovalLegProfileIdFkSql = """
        IF OBJECT_ID(N'dbo.ProjectContracts', N'U') IS NULL
            OR OBJECT_ID(N'dbo.ApprovalLegProfiles', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ProjectContracts', N'ApprovalLegProfileId') IS NULL
            RETURN;
        IF EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.ProjectContracts')
              AND name = N'FK_ProjectContracts_ApprovalLegProfiles_ApprovalLegProfileId')
            RETURN;
        ALTER TABLE dbo.ProjectContracts
            ADD CONSTRAINT FK_ProjectContracts_ApprovalLegProfiles_ApprovalLegProfileId
            FOREIGN KEY (ApprovalLegProfileId) REFERENCES dbo.ApprovalLegProfiles(ID)
            ON DELETE SET NULL;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        ExecuteBatch(connection, EnsureApprovalLegProfileIdColumnSql);
        ExecuteBatch(connection, EnsureApprovalLegProfileIdFkSql);
    }

    private static void ExecuteBatch(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}