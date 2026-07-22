using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationType"/> capability flag columns before/after EF schema sync
/// (SQL Server and PostgreSQL). Required on Postgres pilot where most T-SQL schema helpers are skipped.
/// </summary>
public sealed class ApplicationTypeCapabilityFlagsSchemaUpdater : ModuleUpdater
{
    public ApplicationTypeCapabilityFlagsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsSqlServer, false);
    }
}