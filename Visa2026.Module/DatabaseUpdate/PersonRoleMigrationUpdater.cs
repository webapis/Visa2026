using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Adds <c>PersonRole</c> column and backfills from legacy <c>IsEmployee</c>.</summary>
public sealed class PersonRoleMigrationUpdater : ModuleUpdater
{
    // Required for ALTER on dbo.People (filtered unique index IX_People_PersonalNumber).
    private const string SqlSessionSettings = @"
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
";

    public PersonRoleMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        EnsurePersonRoleColumn();
        base.UpdateDatabaseBeforeUpdateSchema();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        EnsurePersonRoleColumn();
        BackfillPersonRole();
        base.UpdateDatabaseAfterUpdateSchema();
    }

    private void EnsurePersonRoleColumn()
    {
        ExecuteNonQueryCommand(SqlSessionSettings + @"
IF OBJECT_ID(N'dbo.People', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.People', N'PersonRole') IS NULL
BEGIN
    ALTER TABLE dbo.People ADD PersonRole int NOT NULL
        CONSTRAINT DF_People_PersonRole DEFAULT (1);
END", false);
    }

    private void BackfillPersonRole()
    {
        ExecuteNonQueryCommand(SqlSessionSettings + @"
IF OBJECT_ID(N'dbo.People', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.People', N'PersonRole') IS NULL
    RETURN;
UPDATE dbo.People SET PersonRole = 0 WHERE IsEmployee = 1;", false);
        ExecuteNonQueryCommand(SqlSessionSettings + @"
IF OBJECT_ID(N'dbo.People', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.People', N'PersonRole') IS NULL
    RETURN;
UPDATE dbo.People SET PersonRole = 1 WHERE IsEmployee = 0 AND PersonRole <> 2;", false);
    }
}
