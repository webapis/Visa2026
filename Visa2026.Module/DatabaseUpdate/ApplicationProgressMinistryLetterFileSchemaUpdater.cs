using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationProfileInstanceProgress.MinistryLetterFile"/> before EF schema sync on existing databases.
/// </summary>
public sealed class ApplicationProfileInstanceProgressMinistryLetterFileSchemaUpdater : ModuleUpdater
{
    public ApplicationProfileInstanceProgressMinistryLetterFileSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        ExecuteNonQueryCommand(ApplicationProfileInstanceProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileIdColumnSql, false);
        ExecuteNonQueryCommand(ApplicationProfileInstanceProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileFkSql, false);
    }
}
