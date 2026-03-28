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

            // Load content if the template is new or empty
            if (template.Template == null || template.Template.Length == 0)
            {
                template.Template = GetResourceBytes(resourceName);
            }
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