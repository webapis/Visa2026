using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.Visa.IssuingApplication"/> column exists and is backfilled from legacy item FK.
/// </summary>
public sealed class VisaIssuingApplicationSchemaUpdater : ModuleUpdater
{
    public VisaIssuingApplicationSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(VisaIssuingApplicationSchemaSql.EnsureSchemaPostgres, false);
        else
            ExecuteNonQueryCommand(VisaIssuingApplicationSchemaSql.EnsureSchemaSqlServer, false);
    }
}
