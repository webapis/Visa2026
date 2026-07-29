using System;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Host-start heal for Report Dashboard PostgreSQL views when ModuleUpdater is skipped
/// (ModuleInfo already current). Recreates missing base views (sentinel column) and
/// missing wrapper views (relation existence). Also heals visa app-progress when
/// ProgressStateCode still ignores Application.LatestPrimaryStateCode (terminal drift),
/// and when PassportNumber is missing from Visa dashboard views.
/// </summary>
public static class ReportDashboardPostgresViewsHealSql
{
    /// <summary>Base views: heal when sentinel column is missing (schema drift).</summary>
    private static readonly (string ViewName, string SentinelColumn, string ResourceLeaf)[] BaseViews =
    {
        ("vw_rd_visa_by_period", "PassportNumber", "vw_rd_visa_by_period.postgres.sql"),
        ("vw_rd_visa_by_category", "CategoryLabel", "vw_rd_visa_by_category.postgres.sql"),
        ("vw_rd_visa_by_type", "TypeLabel", "vw_rd_visa_by_type.postgres.sql"),
        ("vw_rd_visa_app_progress", "PassportNumber", "vw_rd_visa_app_progress.postgres.sql"),
        ("vw_rd_visa_extension_required", "PassportNumber", "vw_rd_visa_extension_required.postgres.sql"),
        ("vw_rd_visa_by_days_remaining", "PassportNumber", "vw_rd_visa_by_days_remaining.postgres.sql"),
    };

    /// <summary>Wrappers: heal when the public view is missing entirely or lacks PassportNumber.</summary>
    private static readonly (string ViewName, string ResourceLeaf)[] WrapperViews =
    {
        ("vw_rd_visa_active_by_project", "vw_rd_visa_active_by_project.postgres.sql"),
        ("vw_rd_visa_active_by_period_category_type", "vw_rd_visa_active_by_period_category_type.postgres.sql"),
        ("vw_rd_visa_on_extension", "vw_rd_visa_on_extension.postgres.sql"),
        ("vw_rd_visa_on_extension_by_period_category_type", "vw_rd_visa_on_extension_by_period_category_type.postgres.sql"),
        ("vw_rd_visa_extension_result", "vw_rd_visa_extension_result.postgres.sql"),
        ("vw_rd_visa_extension_result_by_period_category_type", "vw_rd_visa_extension_result_by_period_category_type.postgres.sql"),
    };

    private static readonly string[] VisaAppProgressDependentViews =
    {
        "vw_rd_visa_app_progress.postgres.sql",
        "vw_rd_visa_on_extension.postgres.sql",
        "vw_rd_visa_on_extension_by_period_category_type.postgres.sql",
        "vw_rd_visa_extension_result.postgres.sql",
        "vw_rd_visa_extension_result_by_period_category_type.postgres.sql",
        "vw_rd_visa_extension_required.postgres.sql",
    };

    private static readonly string[] VisaByPeriodDependentViews =
    {
        "vw_rd_visa_by_period.postgres.sql",
        "vw_rd_visa_active_by_project.postgres.sql",
        "vw_rd_visa_active_by_period_category_type.postgres.sql",
    };

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();

        if (NeedsVisaAppProgressPrimaryCodeHeal(connection))
        {
            foreach (var resourceLeaf in VisaAppProgressDependentViews)
                ExecuteEmbeddedSql(connection, resourceLeaf);
        }

        if (NeedsVisaPassportNumberHeal(connection))
        {
            foreach (var resourceLeaf in VisaByPeriodDependentViews)
                ExecuteEmbeddedSql(connection, resourceLeaf);
            ExecuteEmbeddedSql(connection, "vw_rd_visa_by_days_remaining.postgres.sql");
            foreach (var resourceLeaf in VisaAppProgressDependentViews)
                ExecuteEmbeddedSql(connection, resourceLeaf);
        }

        foreach (var (viewName, sentinelColumn, resourceLeaf) in BaseViews)
        {
            if (ColumnExists(connection, viewName, sentinelColumn))
                continue;

            ExecuteEmbeddedSql(connection, resourceLeaf);
        }

        foreach (var (viewName, resourceLeaf) in WrapperViews)
        {
            if (ViewExists(connection, viewName) && ColumnExists(connection, viewName, "PassportNumber"))
                continue;

            ExecuteEmbeddedSql(connection, resourceLeaf);
        }
    }

    /// <summary>
    /// True when On Extension still contains apps whose LatestPrimaryStateCode is terminal
    /// (ProgressStateCode was taken from a lagging latest progress row).
    /// </summary>
    private static bool NeedsVisaAppProgressPrimaryCodeHeal(NpgsqlConnection connection)
    {
        if (!ViewExists(connection, "vw_rd_visa_on_extension"))
            return false;

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM vw_rd_visa_on_extension o
            INNER JOIN "Applications" a ON a."ID" = o."ApplicationOid"
            WHERE a."LatestPrimaryStateCode" IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
               OR RIGHT(BTRIM(COALESCE(a."LatestPrimaryStateCode", '')), 16) = '_REVIEW_REJECTED'
            LIMIT 1;
            """;
        return command.ExecuteScalar() is not null;
    }

    /// <summary>
    /// True when any officer-facing Visa dashboard view lacks PassportNumber.
    /// </summary>
    private static bool NeedsVisaPassportNumberHeal(NpgsqlConnection connection)
    {
        string[] views =
        [
            "vw_rd_visa_active_by_project",
            "vw_rd_visa_by_days_remaining",
            "vw_rd_visa_extension_required",
            "vw_rd_visa_on_extension",
            "vw_rd_visa_extension_result",
        ];

        foreach (var viewName in views)
        {
            if (!ViewExists(connection, viewName))
                continue;
            if (!ColumnExists(connection, viewName, "PassportNumber"))
                return true;
        }

        return false;
    }

    private static void ExecuteEmbeddedSql(NpgsqlConnection connection, string resourceLeaf)
    {
        var sql = LoadEmbeddedSql(resourceLeaf);
        if (string.IsNullOrWhiteSpace(sql))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static bool ViewExists(NpgsqlConnection connection, string viewName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT to_regclass(@qualified) IS NOT NULL;
            """;
        command.Parameters.AddWithValue("qualified", "public." + viewName);
        var result = command.ExecuteScalar();
        return result is true || (result is bool b && b);
    }

    private static bool ColumnExists(NpgsqlConnection connection, string viewName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @viewName
              AND column_name = @columnName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("viewName", viewName);
        command.Parameters.AddWithValue("columnName", columnName);
        return command.ExecuteScalar() is not null;
    }

    private static string LoadEmbeddedSql(string resourceLeaf)
    {
        var assembly = typeof(ReportDashboardPostgresViewsHealSql).Assembly;
        var resourceName = "Visa2026.Module.SqlViews." + resourceLeaf;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException(
                "Missing embedded Report Dashboard SQL resource: " + resourceName);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}