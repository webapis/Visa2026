using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Persists family-member PDF mapping correction: delete legacy <c>_241</c> row and rewrite item 18 lines
/// <c>_181</c>–<c>_183</c>. Also invoked from <see cref="PdfFormMappingUpdater"/> on DB update.
/// </summary>
public sealed class FamilyMembersPdfFormMappingUpdater : ModuleUpdater
{
    public FamilyMembersPdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        MigrateFamilyMemberPdfMappings();
        ObjectSpace.CommitChanges();
    }

    internal static void MigrateFamilyMemberPdfMappings(IObjectSpace objectSpace)
    {
        foreach (var mapping in objectSpace.GetObjectsQuery<PdfFormMapping>()
                     .Where(m => m.PdfFieldKey == PdfFormMappingFamilyFieldKeys.WrongAggregateKey)
                     .ToList())
        {
            objectSpace.Delete(mapping);
        }

        EnsureLineMapping(objectSpace, PdfFormMappingFamilyFieldKeys.Line1Key,
            PdfFormMappingFamilyFieldKeys.MaritalLine1Path, "Family members line 1 (item 18)");
        EnsureLineMapping(objectSpace, PdfFormMappingFamilyFieldKeys.Line2Key,
            PdfFormMappingFamilyFieldKeys.MaritalLine2Path, "Family members line 2 (item 18)");
        EnsureLineMapping(objectSpace, PdfFormMappingFamilyFieldKeys.Line3Key,
            PdfFormMappingFamilyFieldKeys.MaritalLine3Path, "Family members line 3 (item 18)");
    }

    private void MigrateFamilyMemberPdfMappings() => MigrateFamilyMemberPdfMappings(ObjectSpace);

    private static void EnsureLineMapping(IObjectSpace objectSpace, string pdfFieldKey, string propertyPath, string description)
    {
        var mapping = objectSpace.FirstOrDefault<PdfFormMapping>(m => m.PdfFieldKey == pdfFieldKey);
        if (mapping == null)
        {
            mapping = objectSpace.CreateObject<PdfFormMapping>();
            mapping.PdfFieldKey = pdfFieldKey;
        }

        mapping.Description = description;
        mapping.MappingMode = PdfMappingMode.Property;
        mapping.PropertyPath = propertyPath;
        mapping.Expression = null;
        mapping.ConstantValue = null;
    }
}
