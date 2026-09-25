using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops retired <c>EmployeeSalaries.Currency</c> after the property was removed from <see cref="BusinessObjects.EmployeeSalary"/>.
/// </summary>
public sealed class EmployeeSalaryCurrencyColumnDropUpdater : ModuleUpdater
{
    private const string DropPostgres = """
        ALTER TABLE IF EXISTS "EmployeeSalaries" DROP COLUMN IF EXISTS "Currency";
        """;

    public EmployeeSalaryCurrencyColumnDropUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(DropPostgres, false);
    }
}
