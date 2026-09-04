using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.CompanyProfile.RegistrationDate"/> exists
/// before EF queries the organization singleton.
/// </summary>
public sealed class CompanyProfileRegistrationDateSchemaUpdater : ModuleUpdater
{
    public CompanyProfileRegistrationDateSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(CompanyProfileRegistrationDateSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(CompanyProfileRegistrationDateSchemaSql.EnsureColumnsSqlServer, false);
    }
}