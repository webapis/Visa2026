using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>Moves PDF form mappings from legacy <see cref="Company"/> paths to Application <see cref="Application"/> report aliases.</summary>
    public class OrganizationPdfFormMappingUpdater : ModuleUpdater
    {
        public OrganizationPdfFormMappingUpdater(IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            MigratePropertyPath("Application.Company.Name", "Application.Application_Company_Name");
            MigratePropertyPath("Application.Company.Address", "Application.Application_Company_Address");
            MigratePropertyPath("Application.Company.PhoneNumber", "Application.Application_Company_PhoneNumber");
            MigratePropertyPath("Application.Company.Email", "Application.Application_Company_Email");
            MigrateExpression(
                "Concat(Person.Company.Name, ', ', Person.Company.PhoneNumber)",
                "Concat(Application.Application_Company_Name, ', ', Application.Application_Company_PhoneNumber)");
            ObjectSpace.CommitChanges();
        }

        private void MigratePropertyPath(string from, string to)
        {
            foreach (var mapping in ObjectSpace.GetObjectsQuery<PdfFormMapping>()
                         .Where(m => m.MappingMode == PdfMappingMode.Property && m.PropertyPath == from))
            {
                mapping.PropertyPath = to;
            }
        }

        private void MigrateExpression(string from, string to)
        {
            foreach (var mapping in ObjectSpace.GetObjectsQuery<PdfFormMapping>()
                         .Where(m => m.MappingMode == PdfMappingMode.Expression && m.Expression == from))
            {
                mapping.Expression = to;
            }
        }
    }
}
