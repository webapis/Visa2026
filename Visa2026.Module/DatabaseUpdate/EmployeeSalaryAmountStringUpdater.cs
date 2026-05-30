using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Converts legacy <see cref="BusinessObjects.EmployeeSalary.Amount"/> from decimal to nvarchar
/// before EF schema sync (string salary entry for labor-contract reports).
/// </summary>
public sealed class EmployeeSalaryAmountStringUpdater : ModuleUpdater
{
    public EmployeeSalaryAmountStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        RecoverPartialEmployeeSalaryAmountMigration();
        EnsureEmployeeSalaryAmountTextColumn();
        CopyEmployeeSalaryAmountToAmountText();
        FinalizeEmployeeSalaryAmountStringMigration();
    }

    private void RecoverPartialEmployeeSalaryAmountMigration()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.EmployeeSalaries', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'Amount') IS NULL
   AND COL_LENGTH(N'dbo.EmployeeSalaries', N'AmountText') IS NOT NULL
BEGIN
    EXEC sys.sp_rename N'dbo.EmployeeSalaries.AmountText', N'Amount', N'COLUMN';
    ALTER TABLE dbo.EmployeeSalaries ALTER COLUMN Amount nvarchar(32) NOT NULL;
END;", false);
    }

    private void EnsureEmployeeSalaryAmountTextColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.EmployeeSalaries', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'Amount') IS NULL
    RETURN;

DECLARE @amountType sysname;
SELECT @amountType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.EmployeeSalaries')
  AND c.name = N'Amount';

IF @amountType IN (N'nvarchar', N'varchar', N'nchar', N'char')
    RETURN;

IF @amountType NOT IN (N'decimal', N'numeric')
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'AmountText') IS NULL
    ALTER TABLE dbo.EmployeeSalaries ADD AmountText nvarchar(32) NULL;", false);
    }

    private void CopyEmployeeSalaryAmountToAmountText()
    {
        // Dynamic SQL: AmountText must not be referenced in a static batch before it exists (SQL Server compile-time validation).
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.EmployeeSalaries', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'Amount') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'AmountText') IS NULL
    RETURN;

DECLARE @amountType sysname;
SELECT @amountType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.EmployeeSalaries')
  AND c.name = N'Amount';

IF @amountType NOT IN (N'decimal', N'numeric')
    RETURN;

DECLARE @copySql nvarchar(max) = N'
UPDATE es
SET es.AmountText = LEFT(CONVERT(nvarchar(32), es.Amount), 32)
FROM dbo.EmployeeSalaries es
WHERE es.AmountText IS NULL;

UPDATE dbo.EmployeeSalaries
SET AmountText = N''0''
WHERE AmountText IS NULL;';

EXEC sys.sp_executesql @copySql;", false);
    }

    private void FinalizeEmployeeSalaryAmountStringMigration()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.EmployeeSalaries', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'Amount') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.EmployeeSalaries', N'AmountText') IS NULL
    RETURN;

DECLARE @amountType sysname;
SELECT @amountType = ty.name
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.EmployeeSalaries')
  AND c.name = N'Amount';

IF @amountType NOT IN (N'decimal', N'numeric')
    RETURN;

DECLARE @defaultConstraint sysname;
SELECT @defaultConstraint = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.EmployeeSalaries')
  AND c.name = N'Amount';

IF @defaultConstraint IS NOT NULL
BEGIN
    DECLARE @dropConstraintSql nvarchar(max) =
        N'ALTER TABLE dbo.EmployeeSalaries DROP CONSTRAINT ' + QUOTENAME(@defaultConstraint) + N';';
    EXEC sys.sp_executesql @dropConstraintSql;
END;

DECLARE @dropAmountSql nvarchar(max) = N'ALTER TABLE dbo.EmployeeSalaries DROP COLUMN Amount;';
EXEC sys.sp_executesql @dropAmountSql;

EXEC sys.sp_rename N'dbo.EmployeeSalaries.AmountText', N'Amount', N'COLUMN';

ALTER TABLE dbo.EmployeeSalaries ALTER COLUMN Amount nvarchar(32) NOT NULL;", false);
    }
}
