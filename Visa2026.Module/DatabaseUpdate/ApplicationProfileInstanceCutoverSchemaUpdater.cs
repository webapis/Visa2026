using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>§13 Application → ApplicationProfileInstance table cutover (rename/copy + FK columns + drop old).</summary>
public sealed class ApplicationProfileInstanceCutoverSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceCutoverSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        Apply();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        Apply();
    }

    private void Apply()
    {
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(ApplicationProfileInstanceCutoverSchemaSql.EnsureSchemaPostgres, false);
        ExecuteNonQueryCommand(ApplicationProfileInstanceCutoverSchemaSql.RenameChildFkColumnsPostgres, false);
        ExecuteNonQueryCommand(ApplicationProfileInstanceCutoverSchemaSql.DropOldTablesPostgres, false);
    }
}