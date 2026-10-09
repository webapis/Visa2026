using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationProfileInstanceProgress.ResultNumber"/> before EF schema sync.
/// </summary>
public sealed class ApplicationProfileInstanceProgressResultNumberSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressResultNumberSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureColumn();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureColumn();
    }

    private void EnsureColumn()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnSqlServer, false);
    }
}