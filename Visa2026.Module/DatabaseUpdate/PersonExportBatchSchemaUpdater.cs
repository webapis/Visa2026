using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures the <see cref="BusinessObjects.PersonExportBatch"/> table exists (SQL Server and PostgreSQL).
/// </summary>
public sealed class PersonExportBatchSchemaUpdater : ModuleUpdater
{
    public PersonExportBatchSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(PersonExportBatchSchemaSql.EnsureTablePostgres, false);
        else
            ExecuteNonQueryCommand(PersonExportBatchSchemaSql.EnsureTableSqlServer, false);
    }
}
