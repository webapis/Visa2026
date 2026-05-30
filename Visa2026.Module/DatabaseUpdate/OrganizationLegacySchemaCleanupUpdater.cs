using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Phase 5: copies any remaining legacy organization data into singleton tables (raw SQL),
/// then drops legacy FK columns and organization tables after EF removes legacy types from the model.
/// </summary>
public class OrganizationLegacySchemaCleanupUpdater : ModuleUpdater
{
    public OrganizationLegacySchemaCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        MigrateLegacyOrganizationDataSql();
        // Drop legacy org FKs/tables before EF sync so ALTER/DROP COLUMN is not blocked by indexes
        // (e.g. IX_Companies_CurrentAuthorizedSignatoryID).
        DropLegacyOrganizationForeignKeys();
        DropLegacyOrganizationTables();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        // Idempotent: covers DBs that still had legacy objects after EF ran.
        DropLegacyOrganizationForeignKeys();
        DropLegacyOrganizationTables();
    }

    private void MigrateLegacyOrganizationDataSql()
    {
        // SQL Server validates object names at batch compile time; use dynamic SQL when a table may not exist yet.
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
    RETURN;

IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NOT NULL
    EXEC sys.sp_executesql N'
IF NOT EXISTS (SELECT 1 FROM dbo.CompanyProfiles WHERE Name IS NOT NULL AND LTRIM(RTRIM(Name)) <> N'''')
BEGIN
    INSERT INTO dbo.CompanyProfiles (ID, Name, Code, Address, PhoneNumber, Email, TaxInformation)
    SELECT TOP (1)
        NEWID(),
        c.Name,
        c.Code,
        c.Address,
        c.PhoneNumber,
        c.Email,
        c.TaxInformation
    FROM dbo.Companies c
    WHERE c.GCRecord IS NULL
    ORDER BY CASE WHEN c.IsDefault = 1 THEN 0 ELSE 1 END, c.ID;
END';", false);

        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
    RETURN;

IF OBJECT_ID(N'dbo.ApplicationNumberingProfiles', N'U') IS NOT NULL
    EXEC sys.sp_executesql N'
IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationNumberingProfiles)
    INSERT INTO dbo.ApplicationNumberingProfiles (ID, Name, ApplicationNumberSeed, ApplicationNumberPadding)
    VALUES (NEWID(), N''Default'', 0, 4);

DECLARE @prefix nvarchar(max), @format nvarchar(max), @seed int, @padding int;
SELECT TOP (1)
    @prefix = c.AppNumberPrefix,
    @format = c.AppNumberFormat,
    @seed = c.ApplicationNumberSeed,
    @padding = NULLIF(c.ApplicationNumberPadding, 0)
FROM dbo.Companies c
WHERE c.GCRecord IS NULL
ORDER BY CASE WHEN c.IsDefault = 1 THEN 0 ELSE 1 END, c.ID;

UPDATE n SET
    AppNumberPrefix = CASE WHEN NULLIF(LTRIM(RTRIM(n.AppNumberPrefix)), N'''') IS NULL THEN @prefix ELSE n.AppNumberPrefix END,
    AppNumberFormat = CASE WHEN NULLIF(LTRIM(RTRIM(n.AppNumberFormat)), N'''') IS NULL THEN @format ELSE n.AppNumberFormat END,
    ApplicationNumberSeed = CASE WHEN n.ApplicationNumberSeed = 0 AND @seed <> 0 THEN @seed ELSE n.ApplicationNumberSeed END,
    ApplicationNumberPadding = CASE WHEN n.ApplicationNumberPadding <= 0 AND @padding IS NOT NULL THEN @padding ELSE n.ApplicationNumberPadding END
FROM dbo.ApplicationNumberingProfiles n;';", false);

        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.CompanyHeads', N'U') IS NULL
    OR OBJECT_ID(N'dbo.AuthorizedSignatories', N'U') IS NULL
    RETURN;

EXEC sys.sp_executesql N'
IF EXISTS (SELECT 1 FROM dbo.AuthorizedSignatories WHERE FullName IS NOT NULL AND LTRIM(RTRIM(FullName)) <> N'''')
    RETURN;

INSERT INTO dbo.AuthorizedSignatories (ID, FullName, PositionTitleTm)
SELECT TOP (1)
    NEWID(),
    COALESCE(
        NULLIF(LTRIM(RTRIM(CONCAT(le.FirstName, N'' '', le.LastName))), N''''),
        NULLIF(LTRIM(RTRIM(CONCAT(p.FirstName, N'' '', p.LastName))), N''''),
        N''''),
    ISNULL(pos.NameTm, N'''')
FROM dbo.CompanyHeads ch
LEFT JOIN dbo.LocalEmployees le ON le.ID = ch.LocalEmployeeID
LEFT JOIN dbo.People p ON p.ID = ch.EmployeeID
LEFT JOIN dbo.Positions pos ON pos.ID = ch.PositionID
WHERE ch.GCRecord IS NULL AND ch.IsActive = 1
ORDER BY ch.ID DESC;';", false);

        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Representatives', N'U') IS NULL
    OR OBJECT_ID(N'dbo.AuthorizedRepresentatives', N'U') IS NULL
    RETURN;

EXEC sys.sp_executesql N'
IF EXISTS (SELECT 1 FROM dbo.AuthorizedRepresentatives WHERE FullName IS NOT NULL AND LTRIM(RTRIM(FullName)) <> N'''')
    RETURN;

INSERT INTO dbo.AuthorizedRepresentatives (ID, FullName, PositionTitleTm, Phone)
SELECT TOP (1)
    NEWID(),
    COALESCE(
        NULLIF(LTRIM(RTRIM(CONCAT(le.FirstName, N'' '', le.LastName))), N''''),
        NULLIF(LTRIM(RTRIM(CONCAT(p.FirstName, N'' '', p.LastName))), N''''),
        N''''),
    ISNULL(pos.NameTm, N''''),
    N''''
FROM dbo.Representatives r
LEFT JOIN dbo.LocalEmployees le ON le.ID = r.LocalEmployeeID
LEFT JOIN dbo.People p ON p.ID = r.EmployeeID
LEFT JOIN dbo.EmployeePositionHistories eph ON eph.ID = p.CurrentPositionHistoryID
LEFT JOIN dbo.Positions pos ON pos.ID = eph.PositionID
WHERE r.GCRecord IS NULL AND r.IsActive = 1
ORDER BY r.ID DESC;';", false);
    }

    private void DropLegacyOrganizationForeignKeys()
    {
        DropColumnIfExists("Applications", "CompanyID");
        DropColumnIfExists("Applications", "CompanyHeadID");
        DropColumnIfExists("Applications", "RepresentativeID");
        DropColumnIfExists("People", "CompanyID");
        DropColumnIfExists("People", "IsSubcontractorEmployee");
        DropColumnIfExists("ProjectContracts", "CompanyID");
        DropColumnIfExists("Lodgings", "CompanyID");
    }

    private void DropLegacyOrganizationTables()
    {
        DropTableIfExists("CompanyHeadDocuments");
        DropTableIfExists("CompanyHeadImages");
        DropTableIfExists("RepresentativeDocuments");
        DropTableIfExists("RepresentativeImages");
        DropTableIfExists("CompanyDocuments");
        DropTableIfExists("CompanyImages");
        DropTableIfExists("CompanyHeads");
        DropTableIfExists("Representatives");
        DropTableIfExists("LocalEmployees");
        DropTableIfExists("Companies");
    }

    private void DropColumnIfExists(string tableName, string columnName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

-- Foreign keys referencing this column
SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.{tableName} DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{tableName}')
  AND c.name = N'{columnName}';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

SET @sql = NULL;

-- Indexes / unique constraints that include this column (blocks DROP COLUMN)
SELECT @sql = STRING_AGG(
    CAST(
        CASE
            WHEN i.is_primary_key = 1 THEN NULL
            WHEN i.is_unique_constraint = 1 THEN
                N'ALTER TABLE dbo.{tableName} DROP CONSTRAINT ' + QUOTENAME(kc.name)
            ELSE
                N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.{tableName}'
        END AS nvarchar(max)),
    N'; ')
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
WHERE i.object_id = OBJECT_ID(N'dbo.{tableName}')
  AND c.name = N'{columnName}'
  AND i.type > 0
  AND i.is_primary_key = 0;

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

EXEC sys.sp_executesql N'ALTER TABLE dbo.{tableName} DROP COLUMN [{columnName}];';", false);
    }

    private void DropTableIfExists(string tableName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);
SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.{tableName}');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DROP TABLE dbo.{tableName};", false);
    }
}
