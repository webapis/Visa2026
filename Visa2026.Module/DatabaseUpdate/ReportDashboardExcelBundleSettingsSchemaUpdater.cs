using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.ReportDashboardExcelBundleSettings"/> exists before EF schema sync.
/// </summary>
public sealed class ReportDashboardExcelBundleSettingsSchemaUpdater : ModuleUpdater
{
    public ReportDashboardExcelBundleSettingsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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

        ExecuteNonQueryCommand(ReportDashboardExcelBundleSettingsSchemaSql.EnsureTablePostgres, false);
    }
}