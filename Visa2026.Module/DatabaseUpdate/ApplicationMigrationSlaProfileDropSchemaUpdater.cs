using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Backfills <c>ApplicationProfile.MigrationSlaDays</c> from the retired type lookup, then drops the table.
/// </summary>
public sealed class ApplicationMigrationSlaProfileDropSchemaUpdater : ModuleUpdater
{
    public ApplicationMigrationSlaProfileDropSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(ApplicationMigrationSlaProfileDropSchemaSql.DropPostgres, false);
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(ApplicationMigrationSlaProfileDropSchemaSql.DropPostgres, false);
    }
}