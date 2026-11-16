using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.VisaIssueWindowSettings"/> exists before EF schema sync.
/// </summary>
public sealed class VisaIssueWindowSettingsSchemaUpdater : ModuleUpdater
{
    public VisaIssueWindowSettingsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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

        ExecuteNonQueryCommand(VisaIssueWindowSettingsSchemaSql.EnsureTablePostgres, false);
    }
}
