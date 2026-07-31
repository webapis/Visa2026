using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Removes <c>LocationID</c> from <c>ApplicationProgresses</c> (progress is state-only).
/// </summary>
public sealed class ApplicationProgressLocationDropSchemaUpdater : ModuleUpdater
{
    public ApplicationProgressLocationDropSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationProgressLocationDropSchemaSql.DropLocationFkAndColumnPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProgressLocationDropSchemaSql.DropLocationFkAndColumnSqlServer, false);
    }
}