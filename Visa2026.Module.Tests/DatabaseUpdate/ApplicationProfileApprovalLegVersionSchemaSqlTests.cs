using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileApprovalLegVersionSchemaSqlTests
{
    [Fact]
    public void VersionsTable_GcRecordMatchesSiblingApplicationProfileTables()
    {
        Assert.Contains(
            "\"GCRecord\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSchemaSql.EnsureApprovalLegVersionsTablePostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"OptimisticLockField\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSchemaSql.EnsureApprovalLegVersionsTablePostgres,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"GCRecord\" integer NULL",
            ApplicationProfileSchemaSql.EnsureApprovalLegVersionsTablePostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_HealsNullableGcRecordOnExistingVersionsTable()
    {
        Assert.Contains(
            ApplicationProfileSchemaSql.HealApprovalLegVersionsGcRecordPostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains(
            "ALTER COLUMN \"GCRecord\" SET NOT NULL",
            ApplicationProfileSchemaSql.HealApprovalLegVersionsGcRecordPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Backfill_UsesZeroGcRecordLikeLiveApplicationProfileRows()
    {
        Assert.Contains("SELECT gen_random_uuid(), 0, 0,", ApplicationProfileSchemaSql.BackfillApprovalLegVersionsPostgres, StringComparison.Ordinal);
        Assert.Contains("COALESCE(p.\"GCRecord\", 0) = 0", ApplicationProfileSchemaSql.BackfillApprovalLegVersionsPostgres, StringComparison.Ordinal);
        Assert.DoesNotContain("p.\"GCRecord\" IS NULL", ApplicationProfileSchemaSql.BackfillApprovalLegVersionsPostgres, StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_ConvertsMovementPermitLocationFkToString()
    {
        Assert.Contains(
            ApplicationProfileSchemaSql.EnsureDefaultWorkPermitLocationPostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains(
            ApplicationProfileSchemaSql.ConvertInstanceMovementPermitLocationToStringPostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains(
            "ADD COLUMN IF NOT EXISTS \"MovementPermitLocation\" character varying(500)",
            ApplicationProfileSchemaSql.ConvertInstanceMovementPermitLocationToStringPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "DefaultWorkPermitLocation",
            ApplicationProfileSchemaSql.EnsureDefaultWorkPermitLocationPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_AddsDefaultApprovalLegProfileForeignKey()
    {
        Assert.Contains(
            ApplicationProfileSchemaSql.EnsureDefaultApprovalLegProfilePostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains(
            "DefaultApprovalLegProfileId",
            ApplicationProfileSchemaSql.EnsureDefaultApprovalLegProfilePostgres,
            StringComparison.Ordinal);
    }
}
