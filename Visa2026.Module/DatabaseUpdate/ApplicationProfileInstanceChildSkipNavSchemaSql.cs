using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Creates skip-navigation join tables for person-related child BOs on ApplicationProfileInstance.
/// Person-child joins backfill from sticky ResolvedLinks.
/// Invitation / WorkPermit / BorderZone headers are 1:N (child FK), not skip-nav — leftover join tables are dropped.
/// </summary>
public static class ApplicationProfileInstanceChildSkipNavSchemaSql
{
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = BuildHealSql();
        command.ExecuteNonQuery();
    }

    internal static string BuildHealSql()
    {
        var joins = new (string Join, string ChildTable, string ChildFk, string ChildPkTable)[]
        {
            ("ApplicationProfileInstancePassports", "Passports", "PassportId", "Passports"),
            ("ApplicationProfileInstanceVisas", "Visas", "VisaId", "Visas"),
            ("ApplicationProfileInstanceEducations", "Educations", "EducationId", "Educations"),
            ("ApplicationProfileInstanceAddressesOfResidence", "AddressesOfResidence", "AddressOfResidenceId", "AddressesOfResidence"),
            ("ApplicationProfileInstanceEmployeePositionHistories", "EmployeePositionHistories", "EmployeePositionHistoryId", "EmployeePositionHistories"),
            ("ApplicationProfileInstanceEmployeeSalaries", "EmployeeSalaries", "EmployeeSalaryId", "EmployeeSalaries"),
            ("ApplicationProfileInstanceMedicalRecords", "MedicalRecords", "MedicalRecordId", "MedicalRecords"),
            ("ApplicationProfileInstanceWorkDuties", "WorkDuties", "WorkDutyId", "WorkDuties"),
            ("ApplicationProfileInstanceInvitationItems", "InvitationItems", "InvitationItemId", "InvitationItems"),
            ("ApplicationProfileInstanceWorkPermitItems", "WorkPermitItems", "WorkPermitItemId", "WorkPermitItems"),
            ("ApplicationProfileInstanceBorderZoneItems", "BorderZoneItems", "BorderZoneItemId", "BorderZoneItems"),
            ("ApplicationProfileInstanceTravelHistories", "TravelHistories", "TravelHistoryId", "TravelHistories"),
        };

        var mistakenHeaderJoins = new[]
        {
            "ApplicationProfileInstanceInvitations",
            "ApplicationProfileInstanceWorkPermits",
            "ApplicationProfileInstanceBorderZones",
        };

        var kinds = new (int Kind, string Join)[]
        {
            (0, "ApplicationProfileInstancePassports"),
            (1, "ApplicationProfileInstanceVisas"),
            (2, "ApplicationProfileInstanceEducations"),
            (3, "ApplicationProfileInstanceAddressesOfResidence"),
            (4, "ApplicationProfileInstanceEmployeePositionHistories"),
            (5, "ApplicationProfileInstanceEmployeeSalaries"),
            (6, "ApplicationProfileInstanceMedicalRecords"),
            (12, "ApplicationProfileInstanceWorkDuties"),
            (7, "ApplicationProfileInstanceInvitationItems"),
            (8, "ApplicationProfileInstanceWorkPermitItems"),
            (9, "ApplicationProfileInstanceBorderZoneItems"),
            (11, "ApplicationProfileInstanceTravelHistories"),
        };

        var sb = new System.Text.StringBuilder();
        // No DbSet historically mapped this as "BorderZoneItem"; InvitationItem/WorkPermitItem use plural.
        // Static SQL is planned even when IF is false (42P01) — use EXECUTE.
        sb.AppendLine("""
            DO $$
            BEGIN
              IF to_regclass('public."BorderZoneItem"') IS NOT NULL
                 AND to_regclass('public."BorderZoneItems"') IS NULL THEN
                EXECUTE 'ALTER TABLE "BorderZoneItem" RENAME TO "BorderZoneItems"';
              END IF;
            END $$;
            """);

        sb.AppendLine("DO $$");
        sb.AppendLine("BEGIN");
        foreach (var j in joins)
        {
            sb.AppendLine($"  IF to_regclass('public.\"{j.Join}\"') IS NULL");
            sb.AppendLine("     AND to_regclass('public.\"ApplicationProfileInstances\"') IS NOT NULL");
            sb.AppendLine($"     AND to_regclass('public.\"{j.ChildPkTable}\"') IS NOT NULL THEN");
            sb.AppendLine("    EXECUTE $create$");
            sb.AppendLine($"    CREATE TABLE \"{j.Join}\" (");
            sb.AppendLine("      \"ApplicationProfileInstanceId\" uuid NOT NULL,");
            sb.AppendLine($"      \"{j.ChildFk}\" uuid NOT NULL,");
            sb.AppendLine($"      CONSTRAINT \"PK_{j.Join}\" PRIMARY KEY (\"ApplicationProfileInstanceId\", \"{j.ChildFk}\"),");
            sb.AppendLine($"      CONSTRAINT \"FK_{j.Join}_ApplicationProfileInstances_ApplicationProfileInstanceId\"");
            sb.AppendLine("        FOREIGN KEY (\"ApplicationProfileInstanceId\") REFERENCES \"ApplicationProfileInstances\" (\"ID\") ON DELETE CASCADE,");
            sb.AppendLine($"      CONSTRAINT \"FK_{j.Join}_{j.ChildPkTable}_{j.ChildFk}\"");
            sb.AppendLine($"        FOREIGN KEY (\"{j.ChildFk}\") REFERENCES \"{j.ChildPkTable}\" (\"ID\") ON DELETE RESTRICT");
            sb.AppendLine("    );");
            sb.AppendLine("    $create$;");
            sb.AppendLine("  END IF;");
        }
        sb.AppendLine("END $$;");

        foreach (var k in kinds)
        {
            var childFk = Array.Find(joins, j => j.Join == k.Join).ChildFk;
            var childTable = Array.Find(joins, j => j.Join == k.Join).ChildPkTable;
            sb.AppendLine("DO $$ BEGIN");
            sb.AppendLine($"  IF to_regclass('public.\"{k.Join}\"') IS NOT NULL");
            sb.AppendLine("     AND to_regclass('public.\"ApplicationProfileInstancePersonResolvedLinks\"') IS NOT NULL");
            sb.AppendLine($"     AND to_regclass('public.\"{childTable}\"') IS NOT NULL THEN");
            sb.AppendLine("    EXECUTE $fill$");
            sb.AppendLine($"    INSERT INTO \"{k.Join}\" (\"ApplicationProfileInstanceId\", \"{childFk}\")");
            sb.AppendLine($"    SELECT DISTINCT rl.\"ApplicationProfileInstanceId\", rl.\"LinkedObjectId\"");
            sb.AppendLine("    FROM \"ApplicationProfileInstancePersonResolvedLinks\" rl");
            sb.AppendLine($"    INNER JOIN \"{childTable}\" c ON c.\"ID\" = rl.\"LinkedObjectId\"");
            sb.AppendLine($"    WHERE rl.\"LinkKind\" = {k.Kind}");
            sb.AppendLine("      AND COALESCE(rl.\"GCRecord\", 0) = 0");
            sb.AppendLine("      AND rl.\"LinkedObjectId\" IS NOT NULL");
            sb.AppendLine("    ON CONFLICT DO NOTHING;");
            sb.AppendLine("    $fill$;");
            sb.AppendLine("  END IF;");
            sb.AppendLine("END $$;");
        }

        sb.AppendLine("DO $$");
        sb.AppendLine("BEGIN");
        foreach (var join in mistakenHeaderJoins)
        {
            sb.AppendLine($"  IF to_regclass('public.\"{join}\"') IS NOT NULL THEN");
            sb.AppendLine($"    EXECUTE 'DROP TABLE IF EXISTS \"{join}\" CASCADE';");
            sb.AppendLine("  END IF;");
        }
        sb.AppendLine("END $$;");

        return sb.ToString();
    }
}