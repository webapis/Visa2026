using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.Person.PreviousWorkplacesInTurkmenistan"/> exists
/// before EF queries Person.
/// </summary>
public sealed class PersonPreviousWorkplacesInTurkmenistanSchemaUpdater : ModuleUpdater
{
    public PersonPreviousWorkplacesInTurkmenistanSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(PersonPreviousWorkplacesInTurkmenistanSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(PersonPreviousWorkplacesInTurkmenistanSchemaSql.EnsureColumnsSqlServer, false);
    }
}