using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ProjectContract.ApprovalLegProfileId"/> before EF schema sync.
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
        ApplySchemaSql();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplySchemaSql();
    }

    private void ApplySchemaSql()
    {
        ExecuteNonQueryCommand(ProjectContractApprovalLegProfileSchemaSql.EnsureApprovalLegProfileIdColumnSql, false);
        ExecuteNonQueryCommand(ProjectContractApprovalLegProfileSchemaSql.EnsureApprovalLegProfileIdFkSql, false);
    }
}