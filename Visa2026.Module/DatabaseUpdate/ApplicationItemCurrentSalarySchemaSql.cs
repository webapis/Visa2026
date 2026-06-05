using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationType.ShowCurrentSalary"/> and
/// <see cref="BusinessObjects.ApplicationItem.CurrentSalary"/> schema columns.
/// </summary>
public static class ApplicationItemCurrentSalarySchemaSql
{
    internal const string EnsureShowCurrentSalaryColumnSql = """
        IF OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationTypes', N'ShowCurrentSalary') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.ApplicationTypes
            ADD ShowCurrentSalary bit NOT NULL
                CONSTRAINT DF_ApplicationTypes_ShowCurrentSalary DEFAULT (0);
        """;

    internal const string EnsureApplicationItemCurrentSalaryIdColumnSql = """
        IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationItems', N'CurrentSalaryId') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.ApplicationItems ADD CurrentSalaryId uniqueidentifier NULL;
        """;

    /// <summary>Application types that expose <c>ApplicationItem.CurrentSalary</c> per catalog.</summary>
    internal static readonly string[] ShowCurrentSalaryApplicationTypeNames =
    {
        "App_Inv_According_to_WP",
        "App_Inv_And_WP",
        "App_Visa_Ext_According_to_WP",
        "App_Visa_and_WP_Ext",
        "App_WP_Ext",
        "App_Additional_WP_location",
    };

    internal const string SyncShowCurrentSalaryFlagsSql = """
        IF OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationTypes', N'ShowCurrentSalary') IS NULL
            RETURN;
        UPDATE dbo.ApplicationTypes
        SET ShowCurrentSalary = 1
        WHERE Name IN (
            N'App_Inv_According_to_WP',
            N'App_Inv_And_WP',
            N'App_Visa_Ext_According_to_WP',
            N'App_Visa_and_WP_Ext',
            N'App_WP_Ext',
            N'App_Additional_WP_location');
        UPDATE dbo.ApplicationTypes
        SET ShowCurrentSalary = 0
        WHERE Name NOT IN (
            N'App_Inv_According_to_WP',
            N'App_Inv_And_WP',
            N'App_Visa_Ext_According_to_WP',
            N'App_Visa_and_WP_Ext',
            N'App_WP_Ext',
            N'App_Additional_WP_location');
        """;

    internal const string EnsureApplicationItemCurrentSalaryFkSql = """
        IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
            OR OBJECT_ID(N'dbo.EmployeeSalaries', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationItems', N'CurrentSalaryId') IS NULL
            RETURN;
        IF EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.ApplicationItems')
              AND name = N'FK_ApplicationItems_EmployeeSalaries_CurrentSalaryId')
            RETURN;
        ALTER TABLE dbo.ApplicationItems
            ADD CONSTRAINT FK_ApplicationItems_EmployeeSalaries_CurrentSalaryId
            FOREIGN KEY (CurrentSalaryId) REFERENCES dbo.EmployeeSalaries(ID)
            ON DELETE NO ACTION;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        ExecuteBatch(connection, EnsureShowCurrentSalaryColumnSql);
        ExecuteBatch(connection, SyncShowCurrentSalaryFlagsSql);
        ExecuteBatch(connection, EnsureApplicationItemCurrentSalaryIdColumnSql);
        ExecuteBatch(connection, EnsureApplicationItemCurrentSalaryFkSql);
    }

    private static void ExecuteBatch(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
