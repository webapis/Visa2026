using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Copies rows from the legacy EF implicit many-to-many join table into
/// <see cref="BusinessObjects.UserReportTemplateApplicationType"/> (one row per template + type).
/// </summary>
public class UserReportTemplateApplicableTypesMigrationUpdater : ModuleUpdater
{
    public UserReportTemplateApplicableTypesMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        // Column names on the legacy join table vary by EF / XAF version — discover them at runtime.
        // silent: true so a mismatched legacy table never blocks application startup (seeds repopulate links).
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.UserReportTemplateApplicationType', N'U') IS NULL
    RETURN;

IF EXISTS (SELECT 1 FROM dbo.UserReportTemplateApplicationType)
    RETURN;

DECLARE @JoinTable sysname = NULL;
DECLARE @TemplateCol sysname = NULL;
DECLARE @AppTypeCol sysname = NULL;
DECLARE @sql nvarchar(max);

SELECT TOP 1 @JoinTable = t.name
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = N'dbo'
  AND t.name <> N'UserReportTemplateApplicationType'
  AND (
        t.name IN (
            N'ApplicationTypeUserReportTemplate',
            N'UserReportTemplateApplicableTypesApplicationType',
            N'UserReportTemplateApplicableTypes',
            N'ApplicationTypeUserReportTemplates')
        OR (t.name LIKE N'%UserReportTemplate%' AND t.name LIKE N'%ApplicationType%')
      )
ORDER BY CASE t.name
    WHEN N'ApplicationTypeUserReportTemplate' THEN 0
    WHEN N'UserReportTemplateApplicableTypesApplicationType' THEN 1
    ELSE 2
END;

IF @JoinTable IS NULL
    RETURN;

SELECT @TemplateCol = c.name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(@JoinTable))
  AND (
        c.name IN (
            N'UserReportTemplateId', N'UserReportTemplateID',
            N'UserReportTemplatesId', N'UserReportTemplatesID',
            N'ApplicableTypesId', N'ApplicableTypesID')
        OR (c.name LIKE N'%UserReportTemplate%' AND c.name LIKE N'%Id')
        OR (c.name LIKE N'%ApplicableTypes%' AND c.name LIKE N'%Id' AND c.name NOT LIKE N'%ApplicationType%')
      );

SELECT @AppTypeCol = c.name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(@JoinTable))
  AND (
        c.name IN (
            N'ApplicationTypeId', N'ApplicationTypeID',
            N'ApplicationTypesId', N'ApplicationTypesID')
        OR (c.name LIKE N'%ApplicationType%' AND c.name LIKE N'%Id' AND c.name NOT LIKE N'%UserReport%')
      );

IF @TemplateCol IS NULL OR @AppTypeCol IS NULL
BEGIN
    DECLARE @GuidCols TABLE (ColName sysname, ColOrder int);

    INSERT INTO @GuidCols (ColName, ColOrder)
    SELECT c.name, c.column_id
    FROM sys.columns c
    INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
    WHERE c.object_id = OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(@JoinTable))
      AND ty.name = N'uniqueidentifier'
      AND c.name NOT IN (N'ID', N'Oid', N'OptimisticLockField');

    IF (SELECT COUNT(*) FROM @GuidCols) = 2
    BEGIN
        SELECT @TemplateCol = ColName
        FROM @GuidCols
        WHERE ColName LIKE N'%UserReport%' OR ColName LIKE N'%Applicable%'
        ORDER BY ColOrder;

        SELECT @AppTypeCol = ColName
        FROM @GuidCols
        WHERE ColName LIKE N'%ApplicationType%'
        ORDER BY ColOrder;

        IF @TemplateCol IS NULL OR @AppTypeCol IS NULL
        BEGIN
            SELECT @TemplateCol = MIN(ColName) FROM @GuidCols;
            SELECT @AppTypeCol = MAX(ColName) FROM @GuidCols;
        END
    END
END

IF @TemplateCol IS NULL OR @AppTypeCol IS NULL
    RETURN;

SET @sql = N'
INSERT INTO dbo.UserReportTemplateApplicationType (ID, UserReportTemplateId, ApplicationTypeId)
SELECT NEWID(), j.' + QUOTENAME(@TemplateCol) + N', j.' + QUOTENAME(@AppTypeCol) + N'
FROM dbo.' + QUOTENAME(@JoinTable) + N' j
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.UserReportTemplateApplicationType x
    WHERE x.UserReportTemplateId = j.' + QUOTENAME(@TemplateCol) + N'
      AND x.ApplicationTypeId = j.' + QUOTENAME(@AppTypeCol) + N'
);';

EXEC sp_executesql @sql;
", silent: true);

        ObjectSpace.CommitChanges();
    }
}
