using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <c>ProcessNumber</c> on <c>ApplicationProgresses</c> and denormalized <c>Applications.ProcessNumber</c>
/// before/after EF schema sync (SQL Server and PostgreSQL).
/// </summary>
public sealed class ApplicationProgressProcessNumberSchemaUpdater : ModuleUpdater
{
    public ApplicationProgressProcessNumberSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureColumns();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureColumns();
        Backfill();
    }

    private void EnsureColumns()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.EnsureColumnsSqlServer, false);
    }

    private void Backfill()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
        {
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionPostgres, false);
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.BackfillApplicationFromProgressPostgres, false);
        }
        else
        {
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionSqlServer, false);
            ExecuteNonQueryCommand(ApplicationProgressProcessNumberSchemaSql.BackfillApplicationFromProgressSqlServer, false);
        }
    }
}