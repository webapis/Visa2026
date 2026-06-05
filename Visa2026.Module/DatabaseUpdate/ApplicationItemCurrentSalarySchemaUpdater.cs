using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationType.ShowCurrentSalary"/> and
/// <see cref="BusinessObjects.ApplicationItem.CurrentSalary"/> columns before EF schema sync.
/// </summary>
public sealed class ApplicationItemCurrentSalarySchemaUpdater : ModuleUpdater
{
    public ApplicationItemCurrentSalarySchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ApplySchemaSql();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplySchemaSql();
    }

    private void ApplySchemaSql()
    {
        ExecuteNonQueryCommand(ApplicationItemCurrentSalarySchemaSql.EnsureShowCurrentSalaryColumnSql, false);
        ExecuteNonQueryCommand(ApplicationItemCurrentSalarySchemaSql.SyncShowCurrentSalaryFlagsSql, false);
        ExecuteNonQueryCommand(ApplicationItemCurrentSalarySchemaSql.EnsureApplicationItemCurrentSalaryIdColumnSql, false);
        ExecuteNonQueryCommand(ApplicationItemCurrentSalarySchemaSql.EnsureApplicationItemCurrentSalaryFkSql, false);
    }
}
