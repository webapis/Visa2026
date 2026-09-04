using System.Linq;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// String-contract guards for host-start schema heal SQL that runs when ModuleInfo
/// already reports current (ModuleUpdater skipped). Drift here breaks officer features
/// after deploy without a full DB recreate.
/// </summary>
public sealed class HostStartSchemaSqlContractTests
{
    [Fact]
    public void CapabilityFlags_BothProviders_AddCanIssueColumns()
    {
        var pg = ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsPostgres;
        var ss = ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsSqlServer;

        foreach (var col in new[] { "CanIssueVisa", "CanIssueInvitation", "CanIssueWorkPermit" })
        {
            Assert.Contains(col, pg);
            Assert.Contains(col, ss);
        }

        Assert.Contains("boolean NOT NULL DEFAULT false", pg);
        Assert.Contains("bit NOT NULL", ss);
        Assert.Contains("DF_ApplicationTypes_CanIssueVisa", ss);
    }

    [Fact]
    public void CapabilityFlags_ApplyIfMissing_Blank_IsNoOp()
    {
        ApplicationTypeCapabilityFlagsSchemaSql.ApplyIfMissing(null!);
        ApplicationTypeCapabilityFlagsSchemaSql.ApplyIfMissing("");
        ApplicationTypeCapabilityFlagsSchemaSql.ApplyIfMissing("   ");
    }

    [Fact]
    public void PersonExportBatch_BothProviders_CreateQueueTableShape()
    {
        var pg = PersonExportBatchSchemaSql.EnsureTablePostgres;
        var ss = PersonExportBatchSchemaSql.EnsureTableSqlServer;

        Assert.Contains("PersonExportBatches", pg);
        Assert.Contains("PersonExportBatches", ss);
        Assert.Contains("\"ZipFileID\"", pg);
        Assert.Contains("ZipFileID", ss);
        Assert.Contains("\"ExportNotes\"", pg);
        Assert.Contains("ExportNotes", ss);
        Assert.Contains("\"PersonDisplayName\"", pg);
        Assert.Contains("PersonDisplayName", ss);
        Assert.Contains("FK_PersonExportBatches_FileData_ZipFileID", pg);
        Assert.Contains("IX_PersonExportBatches_CreatedOnUtc", ss);
    }

    [Fact]
    public void PersonExportBatch_ApplyIfMissing_Blank_IsNoOp()
    {
        PersonExportBatchSchemaSql.ApplyIfMissing(null!);
        PersonExportBatchSchemaSql.ApplyIfMissing(" ");
    }

    [Fact]
    public void InvitationLegacyShape_AddsVisaDateColumns_AndDropsValidityDuration()
    {
        var ss = InvitationLegacyShapeSchemaSql.EnsureColumnsSqlServer;
        Assert.Contains("IsVisaStartAndEndDateDefined", ss);
        Assert.Contains("VisaStartDate", ss);
        Assert.Contains("VisaEndDate", ss);
        Assert.Contains("VisaCategoryID", ss);
        Assert.Contains("VisaPeriodID", ss);
        Assert.Contains("FK_Invitations_VisaCategories_VisaCategoryID", ss);
        Assert.Contains("FK_Invitations_VisaPeriods_VisaPeriodID", ss);

        Assert.Contains("ValidityDurationID", InvitationLegacyShapeSchemaSql.DropValidityDurationSqlServer);
        Assert.Contains("DROP COLUMN ValidityDurationID", InvitationLegacyShapeSchemaSql.DropValidityDurationSqlServer);
        Assert.Contains(
            "DROP COLUMN IF EXISTS \"ValidityDurationID\"",
            InvitationLegacyShapeSchemaSql.DropValidityDurationPostgres);

        Assert.Equal(5, InvitationLegacyShapeSchemaSql.EnsureColumnsPostgresStatements.Length);
        Assert.Contains(
            InvitationLegacyShapeSchemaSql.EnsureColumnsPostgresStatements,
            s => s.Contains("VisaStartDate", System.StringComparison.Ordinal));
        Assert.Contains(
            InvitationLegacyShapeSchemaSql.EnsureColumnsPostgresStatements,
            s => s.Contains("VisaPeriodID", System.StringComparison.Ordinal));
    }

    [Fact]
    public void CurrentSalary_CatalogNames_MatchSyncSql_AndFk()
    {
        var names = ApplicationItemCurrentSalarySchemaSql.ShowCurrentSalaryApplicationTypeNames;
        Assert.Equal(6, names.Length);
        Assert.Contains("App_Inv_And_WP", names);
        Assert.Contains("App_WP_Ext", names);

        var sync = ApplicationItemCurrentSalarySchemaSql.SyncShowCurrentSalaryFlagsSql;
        foreach (var name in names)
        {
            Assert.Contains($"N'{name}'", sync);
        }

        Assert.Contains("ShowCurrentSalary = 1", sync);
        Assert.Contains("ShowCurrentSalary = 0", sync);
        Assert.Contains(
            "FK_ApplicationItems_EmployeeSalaries_CurrentSalaryId",
            ApplicationItemCurrentSalarySchemaSql.EnsureApplicationItemCurrentSalaryFkSql);
        Assert.Contains(
            "CurrentSalaryId uniqueidentifier NULL",
            ApplicationItemCurrentSalarySchemaSql.EnsureApplicationItemCurrentSalaryIdColumnSql);
    }

    [Fact]
    public void CurrentSalary_ApplyIfMissing_Blank_IsNoOp()
    {
        ApplicationItemCurrentSalarySchemaSql.ApplyIfMissing(null!);
        ApplicationItemCurrentSalarySchemaSql.ApplyIfMissing("");
    }

    [Fact]
    public void ProgressOrder_Backfill_UsesStateCodeOrdering()
    {
        var backfill = ApplicationProgressOrderSchemaSql.BackfillProgressOrderSql;
        var recompute = ApplicationProgressOrderSchemaSql.RecomputeAllProgressOrderSql;

        foreach (var sql in new[] { backfill, recompute })
        {
            Assert.Contains("IS_BEING_PREPARED", sql);
            Assert.Contains("PROCESS_STARTED", sql);
            Assert.Contains("PROCESS_ISSUED", sql);
            Assert.Contains("PROCESS_CANCELLED", sql);
            Assert.Contains("PROCESS_REJECTED", sql);
            Assert.Contains("[1-5]_REVIEW_STARTED", sql);
            Assert.Contains("PARTITION BY ap.ApplicationID", sql);
            Assert.Contains("ROW_NUMBER()", sql);
        }

        Assert.Contains(
            "ProgressOrder int NOT NULL",
            ApplicationProgressOrderSchemaSql.EnsureProgressOrderColumnSql);
        Assert.Contains("WHERE ap.ProgressOrder = 0", backfill);
        Assert.DoesNotContain("WHERE ap.ProgressOrder = 0", recompute);
    }

    [Fact]
    public void ProgressOrder_ApplyIfMissing_Blank_IsNoOp()
    {
        ApplicationProgressOrderSchemaSql.ApplyIfMissing("  ");
    }

    [Fact]
    public void ApplicationTypeGroup_BothProviders_CreateGroupsMembersAndTemplateLinks()
    {
        var pg = ApplicationTypeGroupSchemaSql.EnsureTablesPostgres;
        var ss = ApplicationTypeGroupSchemaSql.EnsureTablesSqlServer;

        Assert.Contains("ApplicationTypeGroups", pg);
        Assert.Contains("ApplicationTypeGroupMembers", pg);
        Assert.Contains("UserReportTemplateApplicationTypeGroups", pg);
        Assert.Contains("ApplicationTypeGroups", ss);
        Assert.Contains("ApplicationTypeGroupMembers", ss);
        Assert.Contains("UserReportTemplateApplicationTypeGroups", ss);
        Assert.Contains("ON DELETE CASCADE", pg);
        Assert.Contains("ON DELETE CASCADE", ss);
        Assert.Contains("IsDefault", pg);
        Assert.Contains("IsDefault", ss);
    }

    [Fact]
    public void MinistryLetterFile_AddsNullableFkToFileData()
    {
        Assert.Contains(
            "MinistryLetterFileID uniqueidentifier NULL",
            ApplicationProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileIdColumnSql);
        Assert.Contains(
            "FK_ApplicationProgresses_FileData_MinistryLetterFileID",
            ApplicationProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileFkSql);
        Assert.Contains("ON DELETE NO ACTION", ApplicationProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileFkSql);

        ApplicationProgressMinistryLetterFileSchemaSql.ApplyIfMissing(null!);
        ApplicationProgressMinistryLetterFileSchemaSql.ApplyIfMissing("");
    }

    [Fact]
    public void ApprovalLegProfile_AddsNullableFkWithSetNullDelete()
    {
        Assert.Contains(
            "ApprovalLegProfileId uniqueidentifier NULL",
            ProjectContractApprovalLegProfileSchemaSql.EnsureApprovalLegProfileIdColumnSql);
        Assert.Contains(
            "FK_ProjectContracts_ApprovalLegProfiles_ApprovalLegProfileId",
            ProjectContractApprovalLegProfileSchemaSql.EnsureApprovalLegProfileIdFkSql);
        Assert.Contains("ON DELETE SET NULL", ProjectContractApprovalLegProfileSchemaSql.EnsureApprovalLegProfileIdFkSql);

        ProjectContractApprovalLegProfileSchemaSql.ApplyIfMissing(" ");
    }

    [Fact]
    public void MinistryReviewSlaSettings_CreatesTableWithDefaultRow()
    {
        Assert.Contains("MaxDaysInReview int NOT NULL", MinistryReviewSlaSettingsSchemaSql.EnsureTableSql);
        Assert.Contains("DEFAULT (4)", MinistryReviewSlaSettingsSchemaSql.EnsureTableSql);
        Assert.Contains("VALUES (NEWID(), 4, 1, 0, 0)", MinistryReviewSlaSettingsSchemaSql.EnsureDefaultRowSql);

        MinistryReviewSlaSettingsSchemaSql.ApplyIfMissing(null!);
    }

    [Fact]
    public void RuntimeLogSchema_CreatesTable_AndResolutionColumns()
    {
        var create = ApplicationRuntimeLogSchemaSql.EnsureTableSql;
        Assert.Contains("ApplicationRuntimeLogs", create);
        Assert.Contains("ErrorCode nvarchar(64)", create);
        Assert.Contains("RelatedBatchId", create);
        Assert.Contains("ResolutionStatus", create);
        Assert.Contains("SentryEventId", create);

        Assert.Contains("SentryEventId nvarchar(32)", ApplicationRuntimeLogSchemaSql.EnsureSentryEventIdColumnSql);
        Assert.Contains("FixCommitHash", ApplicationRuntimeLogSchemaSql.EnsureResolutionColumnsSql);
        Assert.Contains("AgentRunId", ApplicationRuntimeLogSchemaSql.EnsureResolutionColumnsSql);
        Assert.Contains(
            "IX_ApplicationRuntimeLogs_ResolutionStatus",
            ApplicationRuntimeLogSchemaSql.EnsureResolutionColumnsSql);

        ApplicationRuntimeLogSchemaSql.ApplyIfMissing("");
    }

    [Fact]
    public void LocationDrop_BothProviders_DropLocationId()
    {
        Assert.Contains("LocationID", ApplicationProgressLocationDropSchemaSql.DropLocationFkAndColumnSqlServer);
        Assert.Contains(
            "DROP COLUMN LocationID",
            ApplicationProgressLocationDropSchemaSql.DropLocationFkAndColumnSqlServer);
        Assert.Contains(
            "DROP COLUMN IF EXISTS \"LocationID\" CASCADE",
            ApplicationProgressLocationDropSchemaSql.DropLocationFkAndColumnPostgres);
    }

    [Fact]
    public void ThemePreference_AddsThreeUserColumns()
    {
        Assert.Contains(
            "PreferredThemeCaption nvarchar(64)",
            ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeCaptionColumnSql);
        Assert.Contains(
            "PreferredThemeMode nvarchar(8)",
            ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeModeColumnSql);
        Assert.Contains(
            "PreferredSizeMode nvarchar(16)",
            ApplicationUserThemePreferenceSchemaSql.EnsurePreferredSizeModeColumnSql);

        ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing(null!);
        ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing("   ");
    }
}
