using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops <c>ProjectContracts.ApprovalLegProfileId</c> and the join table
/// <c>ProjectContractApprovalLegProfiles</c> before EF schema sync.
/// </summary>
public sealed class ProjectContractApprovalLegProfileSchemaUpdater : ModuleUpdater
{
    public ProjectContractApprovalLegProfileSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(ProjectContractApprovalLegProfileSchemaSql.DropPostgres, false);
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        ExecuteNonQueryCommand(ProjectContractApprovalLegProfileSchemaSql.DropPostgres, false);
    }
}
