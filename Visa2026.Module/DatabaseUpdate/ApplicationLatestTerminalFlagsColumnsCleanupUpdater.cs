using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops <c>Applications.LatestIsCancelled</c> and <c>LatestIsRejected</c>; workflow terminals are progress-only.
/// </summary>
public sealed class ApplicationLatestTerminalFlagsColumnsCleanupUpdater : ModuleUpdater
{
    public ApplicationLatestTerminalFlagsColumnsCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsSqlServer, false);
    }
}
