using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.DatabaseUpdate
{
    public class MailMergeUpdater : ModuleUpdater
    {
        public MailMergeUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // Ensure the actual Template objects exist
            EnsureTemplateExists(
                name: "Visa Grant Letter",
                dataType: typeof(Visa2026.Module.BusinessObjects.Application),
                resourceName: "Visa2026.Module.Resources.Visa_Grant_Letter.docx"
            );

            EnsureTemplateExists(
                name: "Rejection Notice",
                dataType: typeof(Visa2026.Module.BusinessObjects.Application),
                resourceName: "Visa2026.Module.Resources.Rejection_Notice.docx"
            );

            EnsureTemplateExists(
                name: "Greeting",
                dataType: typeof(Visa2026.Module.BusinessObjects.Application),
                resourceName: "Visa2026.Module.Resources.Greeting.docx"
            );
            EnsureTemplateExists(
                name: "Form_16",
                dataType: typeof(Visa2026.Module.BusinessObjects.Registration),
                resourceName: "Visa2026.Module.Resources.Form_16.docx"
            );
            EnsureTemplateExists(
                name: "App_Reg_Check_In",
                dataType: typeof(Visa2026.Module.BusinessObjects.Application),
                resourceName: "Visa2026.Module.Resources.App_Reg_Check_In.docx"
            );
               EnsureTemplateExists(
                name: "Registration",
                dataType: typeof(Visa2026.Module.BusinessObjects.Registration),
                resourceName: "Visa2026.Module.Resources.Registration.docx"
            );

            // "Visa Grant Letter" - Only visible for Approved applications
            CreateMailMergeVisibility(
                templateName: "Visa Grant Letter",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Approved'"
            );

            // "Rejection Notice" - Only visible for Rejected applications
            CreateMailMergeVisibility(
                templateName: "Rejection Notice",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Rejected'"
            );

            // "Passport Verification Form" - Visible for all states except Draft
            CreateMailMergeVisibility(
                templateName: "Greeting",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: null
            );
            //Forma 16
        CreateMailMergeVisibility(
                templateName: "Form_16",
                targetType: typeof(Visa2026.Module.BusinessObjects.Registration),
                criteria: null
            );

               CreateMailMergeVisibility(
                templateName: "Registration",
                targetType: typeof(Visa2026.Module.BusinessObjects.Registration),
                criteria: null
            );

             CreateMailMergeVisibility(
                templateName: "App_Reg_Check_In",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_In'"
            );

            CreateMailMergeVisibility(
                templateName: "Registration",
                targetType: typeof(Visa2026.Module.BusinessObjects.Registration),
                criteria: null
            );
            ObjectSpace.CommitChanges();
        }

        private void EnsureTemplateExists(string name, Type dataType, string resourceName)
        {
            var template = ObjectSpace.FirstOrDefault<RichTextMailMergeData>(t => t.Name == name);
            if (template == null)
            {
                template = ObjectSpace.CreateObject<RichTextMailMergeData>();
                template.Name = name;
                template.DataType = dataType;
            }

#if DEBUG
            // Always overwrite during development to pick up changes in the .docx resource files
            template.Template = GetResourceBytes(resourceName);
#else
            // In production, only load if the template is missing to preserve user edits made in the UI
            if (template.Template == null || template.Template.Length == 0)
            {
                template.Template = GetResourceBytes(resourceName);
            }
#endif
        }

        private byte[] GetResourceBytes(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    // Listing available resources helps debug naming mismatches (e.g. spaces vs underscores)
                    string[] resources = assembly.GetManifestResourceNames();
                    string available = string.Join(Environment.NewLine, resources);
                    throw new FileNotFoundException($"Mail Merge template '{resourceName}' was not found in assembly resources. Ensure the file Build Action is 'Embedded Resource'.{Environment.NewLine}Available resources found:{Environment.NewLine}{available}");
                }
                
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private void CreateMailMergeVisibility(string templateName, Type targetType, string criteria)
        {
            // FIX 3: Find by TemplateName only (same pattern as ReportsUpdater),
            // then unconditionally update all fields — avoids duplicates if TargetType ever changes.
            var visibility = ObjectSpace.FirstOrDefault<MailMergeVisibility>(v => v.TemplateName == templateName)
                             ?? ObjectSpace.CreateObject<MailMergeVisibility>();

            visibility.TemplateName = templateName;
            visibility.TargetTypeFullName = targetType.FullName;
            visibility.VisibilityCriteria = criteria;
        }
    }
}