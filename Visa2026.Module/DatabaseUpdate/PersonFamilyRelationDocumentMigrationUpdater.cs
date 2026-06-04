using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="PersonFamilyRelationDocument"/> table exists, then moves legacy
/// <see cref="PersonDocument"/> rows on family members (<see cref="Person.IsEmployee"/> false).
/// </summary>
public sealed class PersonFamilyRelationDocumentMigrationUpdater : ModuleUpdater
{
    public PersonFamilyRelationDocumentMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        EnsurePersonFamilyRelationDocumentsTable();
        base.UpdateDatabaseBeforeUpdateSchema();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        EnsurePersonFamilyRelationDocumentsTable();
        base.UpdateDatabaseAfterUpdateSchema();

        var legacyDocs = ObjectSpace.GetObjectsQuery<PersonDocument>()
            .Where(d => d.Person != null && !d.Person.IsEmployee)
            .ToList();

        if (legacyDocs.Count == 0)
        {
            return;
        }

        foreach (var legacy in legacyDocs)
        {
            var migrated = ObjectSpace.CreateObject<PersonFamilyRelationDocument>();
            migrated.Person = legacy.Person;
            migrated.File = legacy.File;
            migrated.Description = legacy.Description;
            ObjectSpace.Delete(legacy);
        }

        ObjectSpace.CommitChanges();
        Tracing.Tracer.LogText(
            $"PersonFamilyRelationDocumentMigrationUpdater: migrated {legacyDocs.Count} PersonDocument row(s) to PersonFamilyRelationDocument.");
    }

    private void EnsurePersonFamilyRelationDocumentsTable()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.PersonFamilyRelationDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PersonFamilyRelationDocuments (
        ID uniqueidentifier NOT NULL,
        GCRecord int NOT NULL CONSTRAINT DF_PersonFamilyRelationDocuments_GCRecord DEFAULT 0,
        OptimisticLockField int NOT NULL CONSTRAINT DF_PersonFamilyRelationDocuments_OptimisticLockField DEFAULT 0,
        FileID uniqueidentifier NULL,
        Description nvarchar(255) NULL,
        PersonID uniqueidentifier NULL,
        CONSTRAINT PK_PersonFamilyRelationDocuments PRIMARY KEY CLUSTERED (ID)
    );
END", false);

        EnsurePersonFamilyRelationDocumentsBaseObjectColumns();
        CopyForeignKeysFromPersonDocumentsTemplate();
    }

    /// <summary>
    /// Aligns with <see cref="PersonDocument"/> / other BaseObject tables: NOT NULL + default 0.
    /// Nullable columns (from an earlier repair) break EF read-back after INSERT (SqlNullValueException on OptimisticLockField).
    /// </summary>
    private void EnsurePersonFamilyRelationDocumentsBaseObjectColumns()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.PersonFamilyRelationDocuments', N'U') IS NULL
    RETURN;

DECLARE @table int = OBJECT_ID(N'dbo.PersonFamilyRelationDocuments');

UPDATE dbo.PersonFamilyRelationDocuments SET GCRecord = 0 WHERE GCRecord IS NULL;
UPDATE dbo.PersonFamilyRelationDocuments SET OptimisticLockField = 0 WHERE OptimisticLockField IS NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = @table AND c.name = N'GCRecord')
    ALTER TABLE dbo.PersonFamilyRelationDocuments
        ADD CONSTRAINT DF_PersonFamilyRelationDocuments_GCRecord DEFAULT 0 FOR GCRecord;

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = @table AND c.name = N'OptimisticLockField')
    ALTER TABLE dbo.PersonFamilyRelationDocuments
        ADD CONSTRAINT DF_PersonFamilyRelationDocuments_OptimisticLockField DEFAULT 0 FOR OptimisticLockField;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = @table AND name = N'GCRecord' AND is_nullable = 1)
    ALTER TABLE dbo.PersonFamilyRelationDocuments ALTER COLUMN GCRecord int NOT NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = @table AND name = N'OptimisticLockField' AND is_nullable = 1)
    ALTER TABLE dbo.PersonFamilyRelationDocuments ALTER COLUMN OptimisticLockField int NOT NULL;", false);
    }

    private void CopyForeignKeysFromPersonDocumentsTemplate()
    {
        ExecuteNonQueryCommand(@"
DECLARE @template int = OBJECT_ID(N'dbo.PersonDocuments');
DECLARE @target int = OBJECT_ID(N'dbo.PersonFamilyRelationDocuments');
IF @template IS NULL OR @target IS NULL
    RETURN;

DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = @target AND name = N''' +
    REPLACE(fk.name, 'PersonDocuments', 'PersonFamilyRelationDocuments') + N''')
    ALTER TABLE dbo.PersonFamilyRelationDocuments ADD CONSTRAINT ' +
    REPLACE(fk.name, 'PersonDocuments', 'PersonFamilyRelationDocuments') + N' FOREIGN KEY (' +
    QUOTENAME(pc.name) + N') REFERENCES ' +
    QUOTENAME(OBJECT_SCHEMA_NAME(fk.referenced_object_id)) + N'.' +
    QUOTENAME(OBJECT_NAME(fk.referenced_object_id)) + N' (' +
    QUOTENAME(rc.name) + N');'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = @template;

IF LEN(@sql) > 0
    EXEC sp_executesql @sql, N'@target int', @target = @target;", false);
    }
}
