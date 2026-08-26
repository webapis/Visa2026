namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures Invitation header columns for legacy-aligned shape and drops obsolete <c>ValidityDuration</c> FK.
/// </summary>
public static class InvitationLegacyShapeSchemaSql
{
    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.Invitations', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.Invitations', N'IsVisaStartAndEndDateDefined') IS NULL
            ALTER TABLE dbo.Invitations ADD IsVisaStartAndEndDateDefined bit NOT NULL
                CONSTRAINT DF_Invitations_IsVisaStartAndEndDateDefined DEFAULT (0);

        IF COL_LENGTH(N'dbo.Invitations', N'VisaStartDate') IS NULL
            ALTER TABLE dbo.Invitations ADD VisaStartDate datetime2 NULL;

        IF COL_LENGTH(N'dbo.Invitations', N'VisaEndDate') IS NULL
            ALTER TABLE dbo.Invitations ADD VisaEndDate datetime2 NULL;

        IF COL_LENGTH(N'dbo.Invitations', N'VisaCategoryID') IS NULL
            ALTER TABLE dbo.Invitations ADD VisaCategoryID uniqueidentifier NULL;

        IF COL_LENGTH(N'dbo.Invitations', N'VisaPeriodID') IS NULL
            ALTER TABLE dbo.Invitations ADD VisaPeriodID uniqueidentifier NULL;

        IF COL_LENGTH(N'dbo.Invitations', N'BorderZoneLocation') IS NULL
            ALTER TABLE dbo.Invitations ADD BorderZoneLocation nvarchar(500) NULL;

        IF COL_LENGTH(N'dbo.Invitations', N'VisaCategoryID') IS NOT NULL
           AND OBJECT_ID(N'dbo.VisaCategories', N'U') IS NOT NULL
           AND NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'dbo.Invitations')
                  AND name = N'FK_Invitations_VisaCategories_VisaCategoryID')
            ALTER TABLE dbo.Invitations
                ADD CONSTRAINT FK_Invitations_VisaCategories_VisaCategoryID
                FOREIGN KEY (VisaCategoryID) REFERENCES dbo.VisaCategories(ID)
                ON DELETE NO ACTION;

        IF COL_LENGTH(N'dbo.Invitations', N'VisaPeriodID') IS NOT NULL
           AND OBJECT_ID(N'dbo.VisaPeriods', N'U') IS NOT NULL
           AND NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'dbo.Invitations')
                  AND name = N'FK_Invitations_VisaPeriods_VisaPeriodID')
            ALTER TABLE dbo.Invitations
                ADD CONSTRAINT FK_Invitations_VisaPeriods_VisaPeriodID
                FOREIGN KEY (VisaPeriodID) REFERENCES dbo.VisaPeriods(ID)
                ON DELETE NO ACTION;
        """;

    internal const string DropValidityDurationSqlServer = """
        IF OBJECT_ID(N'dbo.Invitations', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.Invitations', N'ValidityDurationID') IS NULL
            RETURN;

        DECLARE @sql nvarchar(max);

        SELECT @sql = STRING_AGG(
            CAST(N'ALTER TABLE dbo.Invitations DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
            N'; ')
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Invitations')
          AND c.name = N'ValidityDurationID';

        IF @sql IS NOT NULL AND LEN(@sql) > 0
            EXEC sys.sp_executesql @sql;

        SELECT @sql = STRING_AGG(
            CAST(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.Invitations' AS nvarchar(max)),
            N'; ')
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Invitations')
          AND i.is_primary_key = 0
          AND c.name = N'ValidityDurationID';

        IF @sql IS NOT NULL AND LEN(@sql) > 0
            EXEC sys.sp_executesql @sql;

        ALTER TABLE dbo.Invitations DROP COLUMN ValidityDurationID;
        """;

    // Prefer ADD COLUMN IF NOT EXISTS (no DO $$ early-return) so ModuleUpdater execution cannot skip alters.
    internal const string EnsureIsVisaStartAndEndDateDefinedPostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "IsVisaStartAndEndDateDefined" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureVisaStartDatePostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "VisaStartDate" timestamp without time zone NULL;""";

    internal const string EnsureVisaEndDatePostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "VisaEndDate" timestamp without time zone NULL;""";

    internal const string EnsureVisaCategoryIdPostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "VisaCategoryID" uuid NULL;""";

    internal const string EnsureVisaPeriodIdPostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "VisaPeriodID" uuid NULL;""";

    internal const string EnsureBorderZoneLocationPostgres =
        """ALTER TABLE "Invitations" ADD COLUMN IF NOT EXISTS "BorderZoneLocation" character varying(500) NULL;""";

    internal const string DropValidityDurationPostgres =
        """ALTER TABLE "Invitations" DROP COLUMN IF EXISTS "ValidityDurationID";""";

    internal static readonly string[] EnsureColumnsPostgresStatements =
    [
        EnsureIsVisaStartAndEndDateDefinedPostgres,
        EnsureVisaStartDatePostgres,
        EnsureVisaEndDatePostgres,
        EnsureVisaCategoryIdPostgres,
        EnsureVisaPeriodIdPostgres,
        EnsureBorderZoneLocationPostgres,
    ];

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new Npgsql.NpgsqlConnection(cleaned);
            connection.Open();
            foreach (var sql in EnsureColumnsPostgresStatements)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            return;
        }

        using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureColumnsSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}