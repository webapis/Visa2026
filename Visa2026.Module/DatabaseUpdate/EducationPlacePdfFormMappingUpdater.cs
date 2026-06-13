using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Rewrites item 21 (<c>_21</c> Okan ýeri) from legacy expression mapping to <see cref="ApplicationItem.Pdf_EducationPlaceOfStudy"/>.</summary>
public sealed class EducationPlacePdfFormMappingUpdater : ModuleUpdater
{
    public EducationPlacePdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        MigrateEducationPlacePdfMapping(ObjectSpace);
        ObjectSpace.CommitChanges();
    }

    internal static void MigrateEducationPlacePdfMapping(IObjectSpace objectSpace)
    {
        var mapping = objectSpace.FirstOrDefault<PdfFormMapping>(m =>
            m.PdfFieldKey == PdfFormMappingFamilyFieldKeys.EducationPlaceKey);
        if (mapping == null)
        {
            mapping = objectSpace.CreateObject<PdfFormMapping>();
            mapping.PdfFieldKey = PdfFormMappingFamilyFieldKeys.EducationPlaceKey;
        }

        mapping.Description = "Education place of study (item 21)";
        mapping.MappingMode = PdfMappingMode.Property;
        mapping.PropertyPath = PdfFormMappingFamilyFieldKeys.EducationPlacePath;
        mapping.Expression = null;
        mapping.ConstantValue = null;
    }
}
