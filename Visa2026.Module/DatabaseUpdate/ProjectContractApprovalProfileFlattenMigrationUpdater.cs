using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures flatten columns/tables on <c>ProjectContracts</c>, copies legacy profile legs when present,
/// before EF removes profile entities.
/// </summary>
public sealed class ProjectContractApprovalProfileFlattenMigrationUpdater : ModuleUpdater
{
    public ProjectContractApprovalProfileFlattenMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ExecuteNonQueryCommand(EnsureFlattenSchemaSql, false);
        ExecuteNonQueryCommand(MigrateLegacyProfilesSql, false);
    }

    private const string EnsureFlattenSchemaSql = """
IF OBJECT_ID(N'dbo.ProjectContracts', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.ProjectContracts', N'IsActive') IS NULL
    ALTER TABLE dbo.ProjectContracts ADD IsActive bit NOT NULL CONSTRAINT DF_ProjectContracts_IsActive DEFAULT(1);

IF COL_LENGTH(N'dbo.ProjectContracts', N'Description') IS NULL
    ALTER TABLE dbo.ProjectContracts ADD Description nvarchar(500) NULL;

IF OBJECT_ID(N'dbo.ProjectContractMinistryLegs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectContractMinistryLegs (
        ID uniqueidentifier NOT NULL PRIMARY KEY,
        ProjectContractId uniqueidentifier NOT NULL,
        Sequence int NOT NULL,
        ApprovingMinistryId uniqueidentifier NOT NULL,
        OptimisticLockField int NULL,
        GCRecord int NULL
    );
    CREATE INDEX IX_ProjectContractMinistryLegs_ProjectContractId ON dbo.ProjectContractMinistryLegs (ProjectContractId);
END;
""";

    private const string MigrateLegacyProfilesSql = """
IF OBJECT_ID(N'dbo.ProjectContractApprovalProfiles', N'U') IS NULL
    RETURN;

;WITH SingleProfile AS (
    SELECT p.ProjectContractId, MIN(p.ID) AS ProfileId, COUNT(*) AS ProfileCount
    FROM dbo.ProjectContractApprovalProfiles p
    WHERE p.GCRecord IS NULL
    GROUP BY p.ProjectContractId
    HAVING COUNT(*) = 1
)
INSERT INTO dbo.ProjectContractMinistryLegs (ID, ProjectContractId, Sequence, ApprovingMinistryId, OptimisticLockField, GCRecord)
SELECT NEWID(), sp.ProjectContractId, l.Sequence, l.ApprovingMinistryId, 0, NULL
FROM SingleProfile sp
INNER JOIN dbo.ProjectContractApprovalLegs l ON l.ProfileId = sp.ProfileId
WHERE l.GCRecord IS NULL
  AND l.ApprovingMinistryId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.ProjectContractMinistryLegs ml
      WHERE ml.ProjectContractId = sp.ProjectContractId AND ml.GCRecord IS NULL);

UPDATE pc SET NameTm = p.Name, IsActive = 1
FROM dbo.ProjectContracts pc
INNER JOIN (
    SELECT p.ProjectContractId, MIN(p.ID) AS ProfileId
    FROM dbo.ProjectContractApprovalProfiles p
    WHERE p.GCRecord IS NULL
    GROUP BY p.ProjectContractId
    HAVING COUNT(*) = 1
) sp ON sp.ProjectContractId = pc.ID
INNER JOIN dbo.ProjectContractApprovalProfiles p ON p.ID = sp.ProfileId
WHERE p.Name IS NOT NULL AND LTRIM(RTRIM(p.Name)) <> '';
""";
}
