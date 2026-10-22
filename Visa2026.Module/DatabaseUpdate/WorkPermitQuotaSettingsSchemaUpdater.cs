using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.WorkPermitQuotaSettings"/> exists before EF schema sync.
/// </summary>
public sealed class WorkPermitQuotaSettingsSchemaUpdater : ModuleUpdater
{
    public WorkPermitQuotaSettingsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(WorkPermitQuotaSettingsSchemaSql.EnsureTablePostgres, false);
    }
}