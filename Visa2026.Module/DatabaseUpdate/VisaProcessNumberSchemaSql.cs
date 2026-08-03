namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for Visa.ProcessNumber (legacy ASNumber / Işlenen belgisi string)
/// and Visa.LegacyPersonInApplicationOid (legacy ProcessNumber PIA FK Guid).
/// </summary>
public static class VisaProcessNumberSchemaSql
{
    /// <summary>
    /// If an earlier mistaken deploy created ProcessNumber as uuid, rename it to LegacyPersonInApplicationOid,
    /// then ensure string ProcessNumber + Guid lineage columns exist.
    /// </summary>
    internal const string EnsureColumnsPostgres = """
        DO $migrate$
        BEGIN
          IF to_regclass('public."Visas"') IS NULL THEN
            RETURN;
          END IF;

          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Visas'
              AND column_name = 'ProcessNumber' AND data_type = 'uuid'
          ) AND NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Visas'
              AND column_name = 'LegacyPersonInApplicationOid'
          ) THEN
            ALTER TABLE public."Visas" RENAME COLUMN "ProcessNumber" TO "LegacyPersonInApplicationOid";
          END IF;

          ALTER TABLE public."Visas" ADD COLUMN IF NOT EXISTS "ProcessNumber" character varying(100) NULL;
          ALTER TABLE public."Visas" ADD COLUMN IF NOT EXISTS "LegacyPersonInApplicationOid" uuid NULL;
        END
        $migrate$;
        """;

    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.Visas', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Visas', N'ProcessNumber') IS NOT NULL
               AND COL_LENGTH(N'dbo.Visas', N'LegacyPersonInApplicationOid') IS NULL
               AND EXISTS (
                    SELECT 1 FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.Visas')
                      AND c.name = N'ProcessNumber'
                      AND t.name = N'uniqueidentifier'
               )
                EXEC sp_rename N'dbo.Visas.ProcessNumber', N'LegacyPersonInApplicationOid', N'COLUMN';

            IF COL_LENGTH(N'dbo.Visas', N'ProcessNumber') IS NULL
                ALTER TABLE dbo.Visas ADD ProcessNumber nvarchar(100) NULL;

            IF COL_LENGTH(N'dbo.Visas', N'LegacyPersonInApplicationOid') IS NULL
                ALTER TABLE dbo.Visas ADD LegacyPersonInApplicationOid uniqueidentifier NULL;
        END
        """;
}