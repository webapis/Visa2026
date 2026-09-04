using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Renames Visas.InvitationItemID → IssuingInvitationItemID before/after EF schema update.
/// </summary>
public sealed class VisaIssuingInvitationItemSchemaUpdater : ModuleUpdater
{
    public VisaIssuingInvitationItemSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureSchema();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        // throwException: true — silent false hid failed renames and left apps querying a missing column.
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(VisaIssuingInvitationItemSchemaSql.EnsureSchemaPostgres, true);
        else
            ExecuteNonQueryCommand(VisaIssuingInvitationItemSchemaSql.EnsureSchemaSqlServer, true);
    }
}