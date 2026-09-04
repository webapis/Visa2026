using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileInstanceProgressOrderSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressOrderSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        ExecuteNonQueryCommand(ApplicationProfileInstanceProgressOrderSchemaSql.EnsureProgressOrderColumnSql, false);
        ExecuteNonQueryCommand(ApplicationProfileInstanceProgressOrderSchemaSql.BackfillProgressOrderSql, false);
    }
}