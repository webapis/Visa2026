using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Converts legacy <see cref="BusinessObjects.Education.GraduationYear"/> from int to nvarchar
/// before EF schema sync (plain text year entry in the UI).
/// </summary>
public sealed class EducationGraduationYearStringUpdater : ModuleUpdater
{
    public EducationGraduationYearStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        RecoverPartialEducationGraduationYearMigration();
        EnsureEducationGraduationYearTextColumn();
        CopyEducationGraduationYearToGraduationYearText();
        FinalizeEducationGraduationYearStringMigration();
    }

    private void RecoverPartialEducationGraduationYearMigration()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Educations', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYear') IS NULL
   AND COL_LENGTH(N'dbo.Educations', N'GraduationYearText') IS NOT NULL
BEGIN
    EXEC sys.sp_rename N'dbo.Educations.GraduationYearText', N'GraduationYear', N'COLUMN';
    ALTER TABLE dbo.Educations ALTER COLUMN GraduationYear nvarchar(8) NULL;
END;", false);
    }

    private void EnsureEducationGraduationYearTextColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Educations', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYear') IS NULL
    RETURN;

DECLARE @yearType sysname;
SELECT @yearType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Educations')
  AND c.name = N'GraduationYear';

IF @yearType IN (N'nvarchar', N'varchar', N'nchar', N'char')
    RETURN;

IF @yearType NOT IN (N'int', N'smallint', N'bigint', N'tinyint')
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYearText') IS NULL
    ALTER TABLE dbo.Educations ADD GraduationYearText nvarchar(8) NULL;", false);
    }

    private void CopyEducationGraduationYearToGraduationYearText()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Educations', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYear') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYearText') IS NULL
    RETURN;

DECLARE @yearType sysname;
SELECT @yearType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Educations')
  AND c.name = N'GraduationYear';

IF @yearType NOT IN (N'int', N'smallint', N'bigint', N'tinyint')
    RETURN;

DECLARE @copySql nvarchar(max) = N'
UPDATE e
SET e.GraduationYearText = LEFT(CONVERT(nvarchar(8), e.GraduationYear), 8)
FROM dbo.Educations e
WHERE e.GraduationYear IS NOT NULL
  AND e.GraduationYearText IS NULL;';

EXEC sys.sp_executesql @copySql;", false);
    }

    private void FinalizeEducationGraduationYearStringMigration()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Educations', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYear') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Educations', N'GraduationYearText') IS NULL
    RETURN;

DECLARE @yearType sysname;
SELECT @yearType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Educations')
  AND c.name = N'GraduationYear';

IF @yearType NOT IN (N'int', N'smallint', N'bigint', N'tinyint')
    RETURN;

DECLARE @dropYearSql nvarchar(max) = N'ALTER TABLE dbo.Educations DROP COLUMN GraduationYear;';
EXEC sys.sp_executesql @dropYearSql;

EXEC sys.sp_rename N'dbo.Educations.GraduationYearText', N'GraduationYear', N'COLUMN';", false);
    }
}
