using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.Person"/> incomplete-data columns exist
/// (SQL Server and PostgreSQL) before Report Dashboard view creation.
/// </summary>
public sealed class PersonIncompleteDataSchemaUpdater : ModuleUpdater
{
    public PersonIncompleteDataSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(PersonIncompleteDataSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(PersonIncompleteDataSchemaSql.EnsureColumnsSqlServer, false);
    }
}