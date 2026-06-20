using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.ApplicationUser"/> theme preference columns exist before EF schema sync
/// (per-user gear-menu theme persistence; safe when properties were added in code before DB migrated).
/// </summary>
public sealed class ApplicationUserThemePreferenceSchemaUpdater : ModuleUpdater
{
    public ApplicationUserThemePreferenceSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
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
        ApplicationUserThemePreferencePermissions.EnsureDefaultRoleSelfWrite(ObjectSpace);
    }

    void ApplySchemaSql()
    {
        ExecuteNonQueryCommand(ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeCaptionColumnSql, false);
        ExecuteNonQueryCommand(ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeModeColumnSql, false);
        ExecuteNonQueryCommand(ApplicationUserThemePreferenceSchemaSql.EnsurePreferredSizeModeColumnSql, false);
    }
}
