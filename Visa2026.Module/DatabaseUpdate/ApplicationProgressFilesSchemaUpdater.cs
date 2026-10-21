using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Creates <c>ApplicationProfileInstanceProgressFiles</c> before EF schema sync.
/// </summary>
public sealed class ApplicationProfileInstanceProgressFilesSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressFilesSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureTable();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureTable();
    }

    private void EnsureTable()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressFilesSchemaSql.EnsureTablePostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressFilesSchemaSql.EnsureTableSqlServer, false);
    }
}
