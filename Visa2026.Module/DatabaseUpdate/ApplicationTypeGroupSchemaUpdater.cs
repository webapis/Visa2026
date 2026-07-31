using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures ApplicationTypeGroup tables exist before seed (SQL Server + PostgreSQL).
/// </summary>
public sealed class ApplicationTypeGroupSchemaUpdater : ModuleUpdater
{
    public ApplicationTypeGroupSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ApplicationTypeGroupSchemaSql.EnsureTables(ObjectSpace);
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplicationTypeGroupSchemaSql.EnsureTables(ObjectSpace);
    }
}