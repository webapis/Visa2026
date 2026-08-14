using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Removes <c>LocationID</c> from <c>ApplicationProfileInstanceProgresses</c> (progress is state-only).
/// </summary>
public sealed class ApplicationProfileInstanceProgressLocationDropSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressLocationDropSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressLocationDropSchemaSql.DropLocationFkAndColumnPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressLocationDropSchemaSql.DropLocationFkAndColumnSqlServer, false);
    }
}