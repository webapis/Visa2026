using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <c>BorderZones.SourceVisaID</c> and drops obsolete <c>ValidityDurationID</c>.
/// </summary>
public sealed class BorderZoneSourceVisaSchemaUpdater : ModuleUpdater
{
    public BorderZoneSourceVisaSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        foreach (var sql in BorderZoneSourceVisaSchemaSql.EnsureColumnsPostgresStatements)
            ExecuteNonQueryCommand(sql, false);
    }
}