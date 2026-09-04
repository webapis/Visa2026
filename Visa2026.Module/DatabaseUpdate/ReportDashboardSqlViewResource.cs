using System;
using System.IO;
using System.Reflection;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Loads Report Dashboard SQL view scripts embedded under Visa2026.Module.SqlViews.*.
/// </summary>
internal static class ReportDashboardSqlViewResource
{
    public static string Load(string resourceLeaf)
    {
        if (string.IsNullOrWhiteSpace(resourceLeaf))
            throw new ArgumentException("Resource leaf is required.", nameof(resourceLeaf));

        var rosterSql = LoadRosterSqlBody(resourceLeaf);
        if (rosterSql is not null)
            return rosterSql;

        var assembly = typeof(ReportDashboardSqlViewResource).Assembly;
        var resourceName = "Visa2026.Module.SqlViews." + resourceLeaf;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                "Missing embedded Report Dashboard SQL resource: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return text.Replace(
            ReportDashboardPostgresRosterSql.MinistryRosterCtePlaceholder,
            ReportDashboardPostgresRosterSql.CteMinistryRosterLines(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Views whose canonical M2M body lives in <see cref="ReportDashboardPostgresRosterSql"/>:
    /// the embedded .postgres.sql files are placeholders so ModuleUpdater and the host-start heal
    /// share one definition. Null for every other leaf.
    /// </summary>
    private static string? LoadRosterSqlBody(string resourceLeaf) => resourceLeaf switch
    {
        "View_VisaExtensionStatus.postgres.sql" =>
            "DROP VIEW IF EXISTS \"View_VisaExtensionStatus\" CASCADE;\n"
            + ReportDashboardPostgresRosterSql.ViewVisaExtensionStatusSql,
        "vw_rd_visa_app_progress.postgres.sql" =>
            "DROP VIEW IF EXISTS vw_rd_visa_app_progress CASCADE;\n"
            + ReportDashboardPostgresRosterSql.VisaAppProgressViewSql,
        "vw_rd_work_permit_app_progress.postgres.sql" =>
            "DROP VIEW IF EXISTS vw_rd_work_permit_app_progress CASCADE;\n"
            + ReportDashboardPostgresRosterSql.WorkPermitAppProgressViewSql,
        "vw_rd_registration.postgres.sql" =>
            "DROP VIEW IF EXISTS vw_rd_registration CASCADE;\n"
            + ReportDashboardPostgresRosterSql.RegistrationViewSql,
        "vw_rd_visa_state.postgres.sql" =>
            "DROP VIEW IF EXISTS vw_rd_visa_state CASCADE;\n"
            + ReportDashboardPostgresRosterSql.VisaStateViewSql,
        _ => null,
    };
}
