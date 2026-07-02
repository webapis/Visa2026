using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops legacy <c>ProjectContractMinistryLegs</c> after VISA2014 import has set <see cref="Application.ApprovalLegProfile"/>.
/// Ministry chains live on <see cref="BusinessObjects.ApprovalLegProfile"/> only.
/// </summary>
public sealed class ProjectContractMinistryLegsSchemaCleanupUpdater : ModuleUpdater
{
    public ProjectContractMinistryLegsSchemaCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        if (!TableExists("ProjectContractMinistryLegs"))
            return;

        ExecuteNonQueryCommand("DROP TABLE dbo.ProjectContractMinistryLegs;", false);
        Tracing.Tracer.LogText("ProjectContractMinistryLegsSchemaCleanupUpdater: dropped dbo.ProjectContractMinistryLegs.");
    }

    private bool TableExists(string tableName)
    {
        var result = ExecuteScalarCommand(
            $"SELECT CASE WHEN OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL THEN 1 ELSE 0 END",
            false);
        return result is int i && i == 1 || result is long l && l == 1;
    }
}
