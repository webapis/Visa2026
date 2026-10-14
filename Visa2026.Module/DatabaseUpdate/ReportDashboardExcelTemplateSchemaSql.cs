using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.ReportDashboardExcelTemplate"/>.
/// </summary>
/// <remarks>
/// This repository does not use EF migrations. When ModuleInfo is already current, XAF skips
/// the schema pass and a newly added table is never created.
/// </remarks>
public static class ReportDashboardExcelTemplateSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ReportDashboardExcelTemplates"') IS NOT NULL THEN
            RETURN;
          END IF;

          CREATE TABLE "ReportDashboardExcelTemplates" (
            "ID" uuid NOT NULL CONSTRAINT "PK_ReportDashboardExcelTemplates" PRIMARY KEY,
            "Category" integer NOT NULL,
            "SubReportKey" character varying(80) NOT NULL,
            "TemplateFileID" uuid NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0
          );

          CREATE UNIQUE INDEX "IX_ReportDashboardExcelTemplates_Category_SubReportKey"
            ON "ReportDashboardExcelTemplates" ("Category", "SubReportKey");
          CREATE INDEX "IX_ReportDashboardExcelTemplates_TemplateFileID"
            ON "ReportDashboardExcelTemplates" ("TemplateFileID");

          IF to_regclass('public."FileData"') IS NOT NULL THEN
            ALTER TABLE "ReportDashboardExcelTemplates"
              ADD CONSTRAINT "FK_ReportDashboardExcelTemplates_FileData_TemplateFileID"
              FOREIGN KEY ("TemplateFileID") REFERENCES "FileData"("ID");
          END IF;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
            || !DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        if (!PostgresRelationExists.All(connectionString, "People"))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureTablePostgres;
        command.ExecuteNonQuery();
    }
}
