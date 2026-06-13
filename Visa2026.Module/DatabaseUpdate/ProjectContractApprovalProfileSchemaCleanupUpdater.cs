using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Migrates legacy approval-profile rows to <see cref="BusinessObjects.ProjectContractMinistryLeg"/>,
/// then drops profile tables and <c>Applications.ContractApprovalProfileId</c>.
/// </summary>
public sealed class ProjectContractApprovalProfileSchemaCleanupUpdater : ModuleUpdater
{
    public ProjectContractApprovalProfileSchemaCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        DropLegacyApprovalProfileArtifacts();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropLegacyApprovalProfileArtifacts();
    }

    private void DropLegacyApprovalProfileArtifacts()
    {
        ExecuteNonQueryCommand(@"
IF COL_LENGTH(N'dbo.Applications', N'ContractApprovalProfileId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Applications_ProjectContractApprovalProfiles_ContractApprovalProfileId')
        ALTER TABLE dbo.Applications DROP CONSTRAINT FK_Applications_ProjectContractApprovalProfiles_ContractApprovalProfileId;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Applications_ContractApprovalProfileId' AND object_id = OBJECT_ID(N'dbo.Applications'))
        DROP INDEX IX_Applications_ContractApprovalProfileId ON dbo.Applications;
    ALTER TABLE dbo.Applications DROP COLUMN ContractApprovalProfileId;
END

IF OBJECT_ID(N'dbo.ProjectContractApprovalLegs', N'U') IS NOT NULL
    DROP TABLE dbo.ProjectContractApprovalLegs;

IF OBJECT_ID(N'dbo.ProjectContractApprovalProfiles', N'U') IS NOT NULL
    DROP TABLE dbo.ProjectContractApprovalProfiles;", false);
    }
}
