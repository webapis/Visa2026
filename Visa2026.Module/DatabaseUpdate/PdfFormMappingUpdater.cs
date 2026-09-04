using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate
{
    public class PdfFormMappingUpdater : ModuleUpdater
    {
        public PdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            SeedPdfFormMappings();
            ObjectSpace.CommitChanges();
        }

        private void SeedPdfFormMappings()
        {
            foreach (var seed in PdfFormMappingSeedCatalog.Core)
            {
                CreateMappingIfNotExists(
                    seed.PdfFieldKey,
                    seed.PropertyPath,
                    seed.Description,
                    seed.Mode,
                    seed.ExpressionOrConstant);
            }

            CreateMappingIfNotExists(PdfFormMappingFamilyFieldKeys.EducationPlaceKey, PdfFormMappingFamilyFieldKeys.EducationPlacePath, "Education place of study (item 21)", PdfMappingMode.Property);
            CreateMappingIfNotExists(PdfFormMappingFamilyFieldKeys.Line1Key, PdfFormMappingFamilyFieldKeys.MaritalLine1Path, "Family members line 1 (item 18)", PdfMappingMode.Property);
            CreateMappingIfNotExists(PdfFormMappingFamilyFieldKeys.Line2Key, PdfFormMappingFamilyFieldKeys.MaritalLine2Path, "Family members line 2 (item 18)", PdfMappingMode.Property);
            CreateMappingIfNotExists(PdfFormMappingFamilyFieldKeys.Line3Key, PdfFormMappingFamilyFieldKeys.MaritalLine3Path, "Family members line 3 (item 18)", PdfMappingMode.Property);

            MigrateFamilyMemberPdfMappings();
            MigrateEducationPlacePdfMapping();
        }

        private void MigrateFamilyMemberPdfMappings() =>
            FamilyMembersPdfFormMappingUpdater.MigrateFamilyMemberPdfMappings(ObjectSpace);

        private void MigrateEducationPlacePdfMapping() =>
            EducationPlacePdfFormMappingUpdater.MigrateEducationPlacePdfMapping(ObjectSpace);

        private void CreateMappingIfNotExists(string pdfKey, string propertyPath, string description, PdfMappingMode mode, string expressionOrConstant = null)
        {
            var existingMapping = ObjectSpace.FirstOrDefault<PdfFormMapping>(m => m.PdfFieldKey == pdfKey);
            if (existingMapping == null)
            {
                var newMapping = ObjectSpace.CreateObject<PdfFormMapping>();
                newMapping.PdfFieldKey = pdfKey;
                newMapping.Description = description;
                newMapping.MappingMode = mode;

                if (mode == PdfMappingMode.Property)
                {
                    newMapping.PropertyPath = propertyPath;
                }
                else if (mode == PdfMappingMode.Expression)
                {
                    newMapping.Expression = expressionOrConstant;
                }
                else if (mode == PdfMappingMode.Constant)
                {
                    newMapping.ConstantValue = expressionOrConstant;
                }
            }
        }
    }
}