using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Copies application numbering from legacy <c>SystemSettings</c> or <c>CompanyProfiles</c> columns
/// onto <see cref="BusinessObjects.ApplicationNumberingProfile"/> when tenant fields are still empty.
/// </summary>
public sealed class ApplicationNumberingProfileMigrationUpdater : ModuleUpdater
{
    public ApplicationNumberingProfileMigrationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        if (!MigratedAnyRows())
            return;

        Tracing.Tracer.LogText(
            "ApplicationNumberingProfileMigrationUpdater: copied application numbering from legacy SystemSettings/CompanyProfiles.");
    }

    private bool MigratedAnyRows()
    {
        const string sql = @"
IF COL_LENGTH('dbo.ApplicationNumberingProfiles', 'AppNumberPrefix') IS NULL
    RETURN;

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationNumberingProfiles)
BEGIN
    INSERT INTO dbo.ApplicationNumberingProfiles (ID, Name, ApplicationNumberSeed, ApplicationNumberPadding)
    VALUES (NEWID(), N'Default', 0, 4);
END;

DECLARE @prefix nvarchar(max) = NULL;
DECLARE @format nvarchar(max) = NULL;
DECLARE @seed int = 0;
DECLARE @padding int = NULL;

IF COL_LENGTH('dbo.CompanyProfiles', 'AppNumberPrefix') IS NOT NULL
BEGIN
    SELECT TOP (1)
        @prefix = NULLIF(LTRIM(RTRIM(AppNumberPrefix)), N''),
        @format = NULLIF(LTRIM(RTRIM(AppNumberFormat)), N''),
        @seed = ApplicationNumberSeed,
        @padding = NULLIF(ApplicationNumberPadding, 0)
    FROM dbo.CompanyProfiles;
END;

IF @prefix IS NULL AND COL_LENGTH('dbo.SystemSettings', 'AppNumberPrefix') IS NOT NULL
BEGIN
    SELECT TOP (1)
        @prefix = NULLIF(LTRIM(RTRIM(AppNumberPrefix)), N''),
        @format = COALESCE(@format, NULLIF(LTRIM(RTRIM(AppNumberFormat)), N'')),
        @seed = CASE WHEN @seed = 0 AND ApplicationNumberSeed <> 0 THEN ApplicationNumberSeed ELSE @seed END,
        @padding = COALESCE(@padding, NULLIF(ApplicationNumberPadding, 0))
    FROM dbo.SystemSettings;
END;

UPDATE t SET
    AppNumberPrefix = CASE WHEN NULLIF(LTRIM(RTRIM(t.AppNumberPrefix)), N'') IS NULL THEN @prefix ELSE t.AppNumberPrefix END,
    AppNumberFormat = CASE WHEN NULLIF(LTRIM(RTRIM(t.AppNumberFormat)), N'') IS NULL THEN @format ELSE t.AppNumberFormat END,
    ApplicationNumberSeed = CASE WHEN t.ApplicationNumberSeed = 0 AND @seed <> 0 THEN @seed ELSE t.ApplicationNumberSeed END,
    ApplicationNumberPadding = CASE WHEN t.ApplicationNumberPadding <= 0 AND @padding IS NOT NULL THEN @padding ELSE t.ApplicationNumberPadding END,
    Name = CASE WHEN NULLIF(LTRIM(RTRIM(t.Name)), N'') IS NULL THEN N'Default' ELSE t.Name END
FROM dbo.ApplicationNumberingProfiles t
WHERE (
    (NULLIF(LTRIM(RTRIM(t.AppNumberPrefix)), N'') IS NULL AND @prefix IS NOT NULL)
    OR (NULLIF(LTRIM(RTRIM(t.AppNumberFormat)), N'') IS NULL AND @format IS NOT NULL)
    OR (t.ApplicationNumberSeed = 0 AND @seed <> 0)
    OR (t.ApplicationNumberPadding <= 0 AND @padding IS NOT NULL)
);";

        try
        {
            ExecuteNonQueryCommand(sql, false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
