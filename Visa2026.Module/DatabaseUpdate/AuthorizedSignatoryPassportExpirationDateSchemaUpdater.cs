using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.AuthorizedSignatory.PassportExpirationDate"/> exists
/// before EF queries the organization singleton.
/// </summary>
public sealed class AuthorizedSignatoryPassportExpirationDateSchemaUpdater : ModuleUpdater
{
    public AuthorizedSignatoryPassportExpirationDateSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
            ExecuteNonQueryCommand(AuthorizedSignatoryPassportExpirationDateSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(AuthorizedSignatoryPassportExpirationDateSchemaSql.EnsureColumnsSqlServer, false);
    }
}