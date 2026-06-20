namespace Visa2026.Module.DatabaseUpdate;

using Microsoft.Data.SqlClient;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationUser"/> theme preference columns.
/// </summary>
public static class ApplicationUserThemePreferenceSchemaSql
{
    internal const string EnsurePreferredThemeCaptionColumnSql = """
        IF OBJECT_ID(N'dbo.PermissionPolicyUser', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.PermissionPolicyUser', N'PreferredThemeCaption') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.PermissionPolicyUser ADD PreferredThemeCaption nvarchar(64) NULL;
        """;

    internal const string EnsurePreferredThemeModeColumnSql = """
        IF OBJECT_ID(N'dbo.PermissionPolicyUser', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.PermissionPolicyUser', N'PreferredThemeMode') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.PermissionPolicyUser ADD PreferredThemeMode nvarchar(8) NULL;
        """;

    internal const string EnsurePreferredSizeModeColumnSql = """
        IF OBJECT_ID(N'dbo.PermissionPolicyUser', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.PermissionPolicyUser', N'PreferredSizeMode') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.PermissionPolicyUser ADD PreferredSizeMode nvarchar(16) NULL;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        ExecuteBatch(connection, EnsurePreferredThemeCaptionColumnSql);
        ExecuteBatch(connection, EnsurePreferredThemeModeColumnSql);
        ExecuteBatch(connection, EnsurePreferredSizeModeColumnSql);
    }

    static void ExecuteBatch(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
