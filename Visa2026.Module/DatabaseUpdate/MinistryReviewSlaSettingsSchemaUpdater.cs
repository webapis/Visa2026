using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.MinistryReviewSlaSettings"/> exists before EF schema sync and seeding.
/// </summary>
public sealed class MinistryReviewSlaSettingsSchemaUpdater : ModuleUpdater
{
    public MinistryReviewSlaSettingsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        ExecuteNonQueryCommand(MinistryReviewSlaSettingsSchemaSql.EnsureTableSql, false);
        ExecuteNonQueryCommand(MinistryReviewSlaSettingsSchemaSql.EnsureDefaultRowSql, false);
    }
}
