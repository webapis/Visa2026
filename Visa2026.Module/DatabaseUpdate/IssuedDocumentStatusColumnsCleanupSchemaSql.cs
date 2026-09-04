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

    internal static readonly string[] DropColumnStatements =
    [
        """ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;""",
        """ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsChanged" CASCADE;""",
        """ALTER TABLE "InvitationItems" DROP COLUMN IF EXISTS "IsUsed" CASCADE;""",
        """ALTER TABLE "WorkPermitItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;""",
        """ALTER TABLE "Visas" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;""",
        """ALTER TABLE "Visas" DROP COLUMN IF EXISTS "IsChanged" CASCADE;""",
        """ALTER TABLE "BorderZones" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;""",
        """ALTER TABLE "BorderZoneItems" DROP COLUMN IF EXISTS "IsCancelled" CASCADE;""",
    ];
}
