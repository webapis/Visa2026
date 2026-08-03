using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures Visa.ProcessNumber (ASNumber / Işlenen belgisi) and LegacyPersonInApplicationOid columns.
/// </summary>
public sealed class VisaProcessNumberSchemaUpdater : ModuleUpdater
{
    public VisaProcessNumberSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        // SQL Server script no-ops via OBJECT_ID when Visas is missing.
        // Postgres ALTER TABLE does not — wait for AfterUpdateSchema on greenfield.
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            EnsureColumns();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureColumns();
    }

    private void EnsureColumns()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(VisaProcessNumberSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(VisaProcessNumberSchemaSql.EnsureColumnsSqlServer, false);
    }
}