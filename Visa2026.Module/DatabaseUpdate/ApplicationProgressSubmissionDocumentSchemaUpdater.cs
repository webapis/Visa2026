using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds the migration-submission document number and file before EF schema sync.
/// </summary>
public sealed class ApplicationProfileInstanceProgressSubmissionDocumentSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressSubmissionDocumentSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureColumns();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureColumns();
    }

    private void EnsureColumns()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.EnsureColumnsSqlServer, false);
    }
}
