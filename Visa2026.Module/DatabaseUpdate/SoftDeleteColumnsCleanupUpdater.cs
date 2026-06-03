using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Purges rows flagged with legacy soft delete, then drops <c>IsDeleted</c>, <c>DateDeleted</c>,
/// and <c>DeletedByID</c> columns after soft delete is removed from the application model.
/// </summary>
public sealed class SoftDeleteColumnsCleanupUpdater : ModuleUpdater
{
    /// <summary>
    /// Parents whose optional <c>Current*</c> FKs are cleared before children that reference them are removed.
    /// </summary>
    private static readonly string[] TablesInPurgeOrder =
    [
        "TravelHistories",
        "BorderZoneItems",
        "InvitationItems",
        "Educations",
        "MedicalRecords",
        "AddressOfResidences",
        "EmployeeContracts",
        "EmployeePositionHistories",
        "EmployeeSalaries",
        "WorkDuties",
        "BorderZones",
        "Passports",
        "Visas",
        "Invitations",
        "WorkPermits",
        "WorkPermitItems",
        "ApplicationItems",
        "ContractTemplates",
        "BusinessTripPlans",
        "Cities",
        "Applications",
        "People",
    ];

    private const int PurgePassCount = 10;

    public SoftDeleteColumnsCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        PurgeSoftDeletedRows();
        DropSoftDeleteColumns();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropSoftDeleteColumns();
    }

    private void PurgeSoftDeletedRows()
    {
        for (int pass = 0; pass < PurgePassCount; pass++)
        {
            foreach (string table in TablesInPurgeOrder)
            {
                BreakReferencesToSoftDeletedParents(table);
                DeleteSoftDeletedRowsInTable(table);
            }
        }
    }

    /// <summary>
    /// For every FK to <paramref name="referencedTable"/>.<c>ID</c>: null nullable columns, then delete remaining child rows.
    /// </summary>
    private void BreakReferencesToSoftDeletedParents(string referencedTable)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{referencedTable}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{referencedTable}', N'IsDeleted') IS NULL
    RETURN;

DECLARE @parent nvarchar(128) = N'{referencedTable}';
DECLARE @childSchema sysname;
DECLARE @childTable sysname;
DECLARE @childColumn sysname;
DECLARE @isNullable bit;
DECLARE @stmt nvarchar(max);

DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT
    OBJECT_SCHEMA_NAME(fk.parent_object_id),
    OBJECT_NAME(fk.parent_object_id),
    pc.name,
    pc.is_nullable
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fkc.referenced_object_id = OBJECT_ID(N'dbo.' + @parent)
  AND rc.name = N'ID';

OPEN fk_cursor;
FETCH NEXT FROM fk_cursor INTO @childSchema, @childTable, @childColumn, @isNullable;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF @isNullable = 1
    BEGIN
        SET @stmt = N'UPDATE ' + QUOTENAME(@childSchema) + N'.' + QUOTENAME(@childTable) +
            N' SET ' + QUOTENAME(@childColumn) + N' = NULL WHERE ' + QUOTENAME(@childColumn) +
            N' IN (SELECT ID FROM dbo.' + QUOTENAME(@parent) + N' WHERE IsDeleted = 1)';
        EXEC sys.sp_executesql @stmt;
    END;

    SET @stmt = N'DELETE FROM ' + QUOTENAME(@childSchema) + N'.' + QUOTENAME(@childTable) +
        N' WHERE ' + QUOTENAME(@childColumn) +
        N' IN (SELECT ID FROM dbo.' + QUOTENAME(@parent) + N' WHERE IsDeleted = 1)';
    EXEC sys.sp_executesql @stmt;

    FETCH NEXT FROM fk_cursor INTO @childSchema, @childTable, @childColumn, @isNullable;
END;

CLOSE fk_cursor;
DEALLOCATE fk_cursor;", false);
    }

    private void DeleteSoftDeletedRowsInTable(string table)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{table}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{table}', N'IsDeleted') IS NULL
    RETURN;
DELETE FROM dbo.{table} WHERE IsDeleted = 1;", false);
    }

    private void DropSoftDeleteColumns()
    {
        foreach (string table in TablesInPurgeOrder)
        {
            DropColumnIfExists(table, "DeletedByID");
            DropColumnIfExists(table, "DateDeleted");
            DropColumnIfExists(table, "IsDeleted");
        }

        RecreatePeoplePersonalNumberIndex();
    }

    private void DropColumnIfExists(string tableName, string columnName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

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

SELECT @sql = STRING_AGG(
    CAST(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.{tableName}' AS nvarchar(max)),
    N'; ')
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.{tableName}')
  AND i.is_primary_key = 0
  AND c.name = N'{columnName}';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

SELECT @sql = STRING_AGG(
    CAST(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.{tableName}' AS nvarchar(max)),
    N'; ')
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.{tableName}')
  AND i.has_filter = 1
  AND i.filter_definition LIKE N'%{columnName}%'
  AND i.is_primary_key = 0;

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

ALTER TABLE dbo.{tableName} DROP COLUMN [{columnName}];", false);
    }

    /// <summary>
    /// Recreates the filtered unique index after <c>IsDeleted</c> is removed (matches <see cref="Visa2026EFCoreDbContext"/>).
    /// </summary>
    private void RecreatePeoplePersonalNumberIndex()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.People', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.People', N'PersonalNumber') IS NULL
    RETURN;
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.People') AND name = N'IX_People_PersonalNumber')
    RETURN;

CREATE UNIQUE NONCLUSTERED INDEX IX_People_PersonalNumber ON dbo.People(PersonalNumber)
WHERE [PersonalNumber] IS NOT NULL AND [PersonalNumber] <> N'' AND [PersonalNumber] <> N'0';", false);
    }
}
