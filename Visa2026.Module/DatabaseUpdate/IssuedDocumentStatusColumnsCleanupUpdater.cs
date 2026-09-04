using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops stored <c>IsCancelled</c> / <c>IsChanged</c> / <c>IsUsed</c> on issued documents.
/// Recreates Report Dashboard views in <see cref="ReportDashboardPostgresViewsUpdater"/>.
/// </summary>
public sealed class IssuedDocumentStatusColumnsCleanupUpdater : ModuleUpdater
{
    public IssuedDocumentStatusColumnsCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        foreach (var sql in IssuedDocumentStatusColumnsCleanupSchemaSql.DropViewStatements)
            ExecuteNonQueryCommand(sql, false);
        foreach (var sql in IssuedDocumentStatusColumnsCleanupSchemaSql.DropColumnStatements)
            ExecuteNonQueryCommand(sql, false);
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        foreach (var sql in IssuedDocumentStatusColumnsCleanupSchemaSql.DropColumnStatements)
            ExecuteNonQueryCommand(sql, false);
    }
}
