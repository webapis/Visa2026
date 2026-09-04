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

    [Fact]
    public void HostStart_AddsNestedTemplateRecycleBinColumns()
    {
        Assert.Contains(
            ApplicationProfileSchemaSql.EnsureTemplateRecycledAtUtcPostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains(
            ApplicationProfileSchemaSql.EnsureTemplateRecycledByUserNamePostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains("RecycledAtUtc", ApplicationProfileSchemaSql.EnsureTemplateRecycledAtUtcPostgres, StringComparison.Ordinal);
        Assert.Contains("RecycledByUserName", ApplicationProfileSchemaSql.EnsureTemplateRecycledByUserNamePostgres, StringComparison.Ordinal);
        Assert.Contains("RecycledAtUtc", ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsSqlServer, StringComparison.Ordinal);
        Assert.Contains("RecycledByUserName", ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsSqlServer, StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_AddsInstanceLetterheadColumns()
    {
        foreach (var sql in ApplicationProfileSchemaSql.EnsureInstanceLetterheadPostgresStatements)
            Assert.Contains(sql, ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains("LetterheadCopied", ApplicationProfileSchemaSql.EnsureInstanceLetterheadPostgres, StringComparison.Ordinal);
        Assert.Contains("LetterheadCompanyName", ApplicationProfileSchemaSql.EnsureInstanceLetterheadPostgres, StringComparison.Ordinal);
        Assert.Contains("LetterheadSignatoryFullName", ApplicationProfileSchemaSql.EnsureInstanceLetterheadPostgres, StringComparison.Ordinal);
        Assert.Contains("LetterheadRepresentativeFullName", ApplicationProfileSchemaSql.EnsureInstanceLetterheadPostgres, StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_AddsOrganizationCatalogColumns()
    {
        foreach (var sql in ApplicationProfileSchemaSql.EnsureOrganizationCatalogPostgresStatements)
            Assert.Contains(sql, ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains("IsDefault", ApplicationProfileSchemaSql.EnsureOrganizationCatalogPostgres, StringComparison.Ordinal);
        Assert.Contains("OrganizationCompanyId", ApplicationProfileSchemaSql.EnsureOrganizationCatalogPostgres, StringComparison.Ordinal);
        Assert.Contains("OrganizationSignatoryId", ApplicationProfileSchemaSql.EnsureOrganizationCatalogPostgres, StringComparison.Ordinal);
        Assert.Contains("OrganizationRepresentativeId", ApplicationProfileSchemaSql.EnsureOrganizationCatalogPostgres, StringComparison.Ordinal);
        Assert.Contains(ApplicationProfileSchemaSql.SeedDemoOrganizationCatalogPostgres,
            ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements);
        Assert.Contains("DEM", ApplicationProfileSchemaSql.SeedDemoOrganizationCatalogPostgres, StringComparison.Ordinal);
        Assert.Contains("Ali Demir", ApplicationProfileSchemaSql.SeedDemoOrganizationCatalogPostgres, StringComparison.Ordinal);
    }
}
