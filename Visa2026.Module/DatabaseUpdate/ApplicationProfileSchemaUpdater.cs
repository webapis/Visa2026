using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.ApplicationProfile"/> tables and
/// <c>Applications.ApplicationProfileID</c> exist before EF schema sync on existing databases.
/// </summary>
public sealed class ApplicationProfileSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
        {
            ExecuteNonQueryCommand(ApplicationProfileSchemaSql.EnsureSchemaPostgres, false);
            foreach (var sql in ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsPostgresStatements)
                ExecuteNonQueryCommand(sql, true);
        }
        else
        {
            ExecuteNonQueryCommand(ApplicationProfileSchemaSql.EnsureSchemaSqlServer, false);
            ExecuteNonQueryCommand(ApplicationProfileSchemaSql.EnsureTemplateCatalogColumnsSqlServer, false);
        }
    }
}
