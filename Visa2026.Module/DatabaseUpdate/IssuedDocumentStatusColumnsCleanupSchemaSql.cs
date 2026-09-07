using System;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops persisted cancel/change/used flags. Lifecycle is derived from completed
/// Cancellation / Change profile instances and visa issuing links.
/// </summary>
public static class IssuedDocumentStatusColumnsCleanupSchemaSql
{
    internal static readonly string[] DropViewStatements =
    [
        """DROP VIEW IF EXISTS vw_rd_invitation_used CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_invitation_ready CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_invitation_valid_until CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_person_search CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_visa_by_days_remaining CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_visa_by_category CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_visa_by_period CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_visa_by_type CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_visa_extension_required CASCADE;""",
        """DROP VIEW IF EXISTS "View_VisaExtensionStatus" CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_work_permit CASCADE;""",
        """DROP VIEW IF EXISTS vw_rd_work_permit_active CASCADE;""",
    ];

    // ALTER TABLE ... DROP COLUMN IF EXISTS still requires the table.
    // Greenfield DROP DATABASE + CREATE DATABASE has no tables yet — skip via to_regclass.
    internal static readonly string[] DropColumnStatements =
    [
        """
        DO $$
        BEGIN
          IF to_regclass('public."InvitationItems"') IS NOT NULL THEN
            ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;
            ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsChanged" CASCADE;
            ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsUsed" CASCADE;
          END IF;
          IF to_regclass('public."WorkPermitItems"') IS NOT NULL THEN
            ALTER TABLE "WorkPermitItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;
          END IF;
          IF to_regclass('public."Visas"') IS NOT NULL THEN
            ALTER TABLE "Visas" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;
            ALTER TABLE "Visas" DROP COLUMN IF EXISTS "IsChanged" CASCADE;
          END IF;
          IF to_regclass('public."BorderZones"') IS NOT NULL THEN
            ALTER TABLE "BorderZones" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;
          END IF;
          IF to_regclass('public."BorderZoneItems"') IS NOT NULL THEN
            ALTER TABLE "BorderZoneItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;
          END IF;
        END $$;
        """,
    ];
}
