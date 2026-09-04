using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <c>ProcessNumber</c> on <c>ApplicationProfileInstanceProgresses</c> and denormalized <c>Applications.ProcessNumber</c>
/// before/after EF schema sync (SQL Server and PostgreSQL).
/// </summary>
public sealed class ApplicationProfileInstanceProgressProcessNumberSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressProcessNumberSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.EnsureColumnsSqlServer, false);
    }

    private void Backfill()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
        {
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionPostgres, false);
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.BackfillApplicationFromProgressPostgres, false);
        }
        else
        {
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionSqlServer, false);
            ExecuteNonQueryCommand(ApplicationProfileInstanceProgressProcessNumberSchemaSql.BackfillApplicationFromProgressSqlServer, false);
        }
    }
}