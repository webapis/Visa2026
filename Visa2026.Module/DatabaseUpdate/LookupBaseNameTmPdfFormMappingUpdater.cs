using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Rewrites PDF form paths from deprecated <c>*.Name</c> to <c>*.NameTm</c> on tenant Turkmen lookups.</summary>
public sealed class LookupBaseNameTmPdfFormMappingUpdater : ModuleUpdater
{
    public LookupBaseNameTmPdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        RewritePdfFormMappingPaths();
    }

    private void RewritePdfFormMappingPaths()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.PdfFormMapping', N'U') IS NULL
    RETURN;

UPDATE dbo.PdfFormMapping
SET PropertyPath = N'CurrentEducation.Specialty.NameTm'
WHERE PropertyPath = N'CurrentEducation.Specialty.Name';

UPDATE dbo.PdfFormMapping
SET PropertyPath = N'CurrentPositionHistory.Position.NameTm'
WHERE PropertyPath = N'CurrentPositionHistory.Position.Name';

UPDATE dbo.PdfFormMapping
SET Expression = REPLACE(Expression, N'CurrentEducation.EducationInstitution.Name', N'CurrentEducation.EducationInstitution.NameTm')
WHERE Expression LIKE N'%CurrentEducation.EducationInstitution.Name%';", false);
    }
}
