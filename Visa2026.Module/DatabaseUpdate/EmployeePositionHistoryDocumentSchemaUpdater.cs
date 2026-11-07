using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <c>EmployeePositionHistoryDocuments</c> exists for order-for-assignment file copies.
/// </summary>
public sealed class EmployeePositionHistoryDocumentSchemaUpdater : ModuleUpdater
{
    public EmployeePositionHistoryDocumentSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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

        ExecuteNonQueryCommand(EmployeePositionHistoryDocumentSchemaSql.EnsureSchemaPostgres, false);
    }
}