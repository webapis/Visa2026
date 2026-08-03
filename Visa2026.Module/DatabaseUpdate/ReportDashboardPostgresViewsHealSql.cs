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

    /// <summary>Standalone dashboard views (no PassportNumber): heal when the relation is missing.
    /// Order: bases before wrappers; invitation on-process (P) before its (V) wrapper.</summary>
    private static readonly (string ViewName, string ResourceLeaf)[] StandaloneViews =
    {
        ("vw_rd_application_via_ministry_invitation_on_process",
            "vw_rd_application_via_ministry_invitation_on_process.postgres.sql"),
        ("vw_rd_application_via_ministry_invitation_on_process_by_period_category_type",
            "vw_rd_application_via_ministry_invitation_on_process_by_period_category_type.postgres.sql"),
        ("vw_rd_application_via_ministry_invitation_completed_base",
            "vw_rd_application_via_ministry_invitation_completed_base.postgres.sql"),
        ("vw_rd_application_via_ministry_invitation_completed",
            "vw_rd_application_via_ministry_invitation_completed.postgres.sql"),
        ("vw_rd_application_via_ministry_invitation_completed_by_period_category_type",
            "vw_rd_application_via_ministry_invitation_completed_by_period_category_type.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_on_process_base",
            "vw_rd_application_via_ministry_visa_extension_on_process_base.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_on_process",
            "vw_rd_application_via_ministry_visa_extension_on_process.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_on_process_by_period_category_type",
            "vw_rd_application_via_ministry_visa_extension_on_process_by_period_category_type.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_completed_base",
            "vw_rd_application_via_ministry_visa_extension_completed_base.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_completed",
            "vw_rd_application_via_ministry_visa_extension_completed.postgres.sql"),
        ("vw_rd_application_via_ministry_visa_extension_completed_by_period_category_type",
            "vw_rd_application_via_ministry_visa_extension_completed_by_period_category_type.postgres.sql"),
        ("vw_rd_application_via_ministry_other_on_process",
            "vw_rd_application_via_ministry_other_on_process.postgres.sql"),
        ("vw_rd_application_via_ministry_other_completed",
            "vw_rd_application_via_ministry_other_completed.postgres.sql"),
        ("vw_rd_application_direct_migration_on_process_a",
            "vw_rd_application_direct_migration_on_process_a.postgres.sql"),
        ("vw_rd_application_direct_migration_process_complete",
            "vw_rd_application_direct_migration_process_complete.postgres.sql"),
    };

    /// <summary>
    /// Requires <see cref="PersonIncompleteDataSchemaSql"/> columns on People first.
    /// Kept out of <see cref="StandaloneViews"/> so a missing incomplete view does not
    /// force a full via-ministry standalone re-heal.
    /// </summary>
    private const string IncompletePersonsViewName = "vw_rd_incomplete_persons_by_missing_area";
    private const string IncompletePersonsResourceLeaf =
        "vw_rd_incomplete_persons_by_missing_area.postgres.sql";

    /// <summary>
    /// Person search reads only base tables, so it heals on relation existence alone.
    /// Kept out of <see cref="StandaloneViews"/> so it does not trigger a via-ministry re-heal.
    /// </summary>
    private const string PersonSearchViewName = "vw_rd_person_search";
    private const string PersonSearchResourceLeaf = "vw_rd_person_search.postgres.sql";

    /// <summary>
    /// Work-permit dashboard views: heal when missing (ModuleUpdater may be skipped).
    /// Kept separate from <see cref="StandaloneViews"/> to avoid via-ministry bulk re-heal.
    /// </summary>
    private static readonly (string ViewName, string ResourceLeaf)[] WorkPermitViews =
    {
        ("vw_rd_work_permit", "vw_rd_work_permit.postgres.sql"),
        ("vw_rd_work_permit_active", "vw_rd_work_permit_active.postgres.sql"),
        ("vw_rd_work_permit_app_progress", "vw_rd_work_permit_app_progress.postgres.sql"),
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

        // Empty greenfield DB: XAF creates base tables first; skip view heal until Visas exists.
        using (var existsCmd = connection.CreateCommand())
        {
            existsCmd.CommandText = """SELECT to_regclass('public."Visas"') IS NOT NULL;""";
            if (existsCmd.ExecuteScalar() is not true)
                return;
        }

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

        if (NeedsViaMinistryStandaloneHeal(connection))
        {
            foreach (var (_, resourceLeaf) in StandaloneViews)
                ExecuteEmbeddedSql(connection, resourceLeaf);
        }
        else
        {
            foreach (var (viewName, resourceLeaf) in StandaloneViews)
            {
                if (ViewExists(connection, viewName))
                    continue;

                ExecuteEmbeddedSql(connection, resourceLeaf);
            }
        }

        HealIncompletePersonsViewIfReady(connection);
        HealWorkPermitViewsIfNeeded(connection);
        HealPersonSearchViewIfNeeded(connection);
    }

    private static void HealWorkPermitViewsIfNeeded(NpgsqlConnection connection)
    {
        foreach (var (viewName, resourceLeaf) in WorkPermitViews)
        {
            if (ViewExists(connection, viewName))
                continue;

            ExecuteEmbeddedSql(connection, resourceLeaf);
        }
    }

    private static void HealPersonSearchViewIfNeeded(NpgsqlConnection connection)
    {
        if (!ViewExists(connection, PersonSearchViewName)
            || NeedsPersonSearchFoldHeal(connection))
        {
            ExecuteEmbeddedSql(connection, PersonSearchResourceLeaf);
        }
    }

    /// <summary>
    /// Recreate when SearchText is not diacritic-folded (pre-fold view body has no translate).
    /// </summary>
    private static bool NeedsPersonSearchFoldHeal(NpgsqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pg_get_viewdef('public.vw_rd_person_search'::regclass, true);
            """;
        var def = command.ExecuteScalar() as string;
        if (string.IsNullOrEmpty(def))
            return true;

        return def.IndexOf("translate(", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static void HealIncompletePersonsViewIfReady(NpgsqlConnection connection)
    {
        if (ViewExists(connection, IncompletePersonsViewName))
            return;

        // People columns must exist before CREATE VIEW (host schema heal runs first in Startup).
        if (!ColumnExists(connection, "People", "IncompleteMissingPersonalData")
            && !ColumnExists(connection, "People", "incompletemissingpersonaldata"))
            return;

        ExecuteEmbeddedSql(connection, IncompletePersonsResourceLeaf);
    }

    private static bool NeedsViaMinistryStandaloneHeal(NpgsqlConnection connection)
    {
        foreach (var (viewName, _) in StandaloneViews)
        {
            if (!ViewExists(connection, viewName))
                return true;
        }

        // Postgres information_schema column names are lowercased unless quoted identifiers preserved.
        if (!ColumnExists(connection, "vw_rd_application_via_ministry_invitation_on_process", "ApplicationItemOid")
            && !ColumnExists(connection, "vw_rd_application_via_ministry_invitation_on_process", "applicationitemoid"))
            return true;
        if (!ColumnExists(connection, "vw_rd_application_via_ministry_invitation_on_process", "VisaPeriodLabel")
            && !ColumnExists(connection, "vw_rd_application_via_ministry_invitation_on_process", "visaperiodlabel"))
            return true;
        if (!ColumnExists(connection, "vw_rd_application_via_ministry_visa_extension_completed", "IssuedVisaNumber")
            && !ColumnExists(connection, "vw_rd_application_via_ministry_visa_extension_completed", "issuedvisanumber"))
            return true;
        if (!ColumnExists(connection, "vw_rd_application_via_ministry_invitation_completed", "InvitationNumber")
            && !ColumnExists(connection, "vw_rd_application_via_ministry_invitation_completed", "invitationnumber"))
            return true;
        if (!ColumnExists(connection, "vw_rd_application_direct_migration_on_process_a", "ApplicationTypeLabel")
            && !ColumnExists(connection, "vw_rd_application_direct_migration_on_process_a", "applicationtypelabel"))
            return true;

        return false;
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