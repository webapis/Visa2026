using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationProfileInstanceProgress.ExceptionalDate"/> before EF schema sync.
/// </summary>
public sealed class ApplicationProfileInstanceProgressExceptionalDateSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressExceptionalDateSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressExceptionalDateSchemaSql.EnsureColumnPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressExceptionalDateSchemaSql.EnsureColumnSqlServer, false);
    }
}
