using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// ListView query indexes on Applications, ApplicationProfileInstanceProgresses, and approval-leg snapshots.
/// </summary>
public sealed class ApplicationListQueryPerformanceSchemaUpdater : ModuleUpdater
{
    public ApplicationListQueryPerformanceSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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

    private void ApplySchemaSql() =>
        ExecuteNonQueryCommand(ApplicationListQueryPerformanceSchemaSql.EnsureIndexesSql, false);
}