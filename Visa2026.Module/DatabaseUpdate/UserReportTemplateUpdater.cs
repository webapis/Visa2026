using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Seeds initial user-defined Word report templates on application startup.
    /// Similar to MailMergeUpdater but for UserReportTemplate BO.
    /// Templates are stored as Embedded Resources in Visa2026.Module/Resources/
    /// </summary>
    public class UserReportTemplateUpdater : ModuleUpdater
    {
        private readonly IUserReportPlaceholderExtractor _extractor;
        private readonly IUserReportValidationService _validator;

        public UserReportTemplateUpdater(
            IObjectSpace objectSpace,
            Version currentDBVersion,
            IUserReportPlaceholderExtractor extractor,
            IUserReportValidationService validator) :
            base(objectSpace, currentDBVersion)
        {
            _extractor = extractor;
            _validator = validator;
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // Example: Seed a default Application cover letter template
            // Uncomment and modify when ready to ship templates
            /*
            EnsureTemplateExists(
                templateName: "Default Application Cover Letter",
                description: "Standard cover letter for visa applications",
                resourceName: "Visa2026.Module.Resources.Templates.DefaultCoverLetter.docx",
                boType: UserReportBoType.Application,
                applicabilityMode: ApplicabilityMode.AllTypes,
                visibilityCriteria: null,
                sortOrder: 100
            );
            */

            ObjectSpace.CommitChanges();
        }

        /// <summary>
        /// Ensures a UserReportTemplate exists with the specified configuration.
        /// In DEBUG: Always overwrites template file content to pick up changes.
        /// In Production: Only creates if missing (preserves user edits to metadata).
        /// </summary>
        private async void EnsureTemplateExists(
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ApplicabilityMode applicabilityMode,
            string visibilityCriteria,
            int sortOrder)
        {
            var template = ObjectSpace.FirstOrDefault<UserReportTemplate>(
                t => t.TemplateName == templateName);

            bool isNew = template == null;

            if (isNew)
            {
                template = ObjectSpace.CreateObject<UserReportTemplate>();
                template.TemplateName = templateName;
            }

#if DEBUG
            // Always overwrite in development to pick up template file changes
            bool shouldUpdateFile = true;
#else
            // In production, only load if template file is missing
            bool shouldUpdateFile = isNew || template.TemplateFile == null;
#endif

            if (shouldUpdateFile)
            {
                try
                {
                    // Load template bytes from embedded resource
                    byte[] templateBytes = GetResourceBytes(resourceName);

                    // Create or update FileData
                    if (template.TemplateFile == null)
                    {
                        template.TemplateFile = ObjectSpace.CreateObject<FileData>();
                    }

                    // Store file content
                    using (var ms = new MemoryStream(templateBytes))
                    {
                        template.TemplateFile.LoadFromStream(
                            fileName: Path.GetFileName(resourceName),
                            stream: ms);
                    }

                    // Extract and validate placeholders
                    using (var ms = new MemoryStream(templateBytes))
                    {
                        var placeholders = await _extractor.ExtractPlaceholdersAsync(ms);
                        var validationResults = await _validator.ValidatePlaceholdersAsync(
                            placeholders.ToList(), boType);

                        // Clear existing placeholders
                        template.Placeholders.Clear();

                        // Add validated placeholders
                        foreach (var result in validationResults)
                        {
                            var placeholder = ObjectSpace.CreateObject<UserReportPlaceholder>();
                            placeholder.Template = template;
                            placeholder.PlaceholderKey = result.PlaceholderKey;
                            placeholder.IsValid = result.IsValid;
                            placeholder.ResolvedPropertyPath = result.ResolvedPath;
                            placeholder.ExampleValue = result.ExampleValue;
                            placeholder.ValidationError = result.ErrorMessage;
                            template.Placeholders.Add(placeholder);
                        }
                    }

                    // Update metadata
                    template.Description = description;
                    template.RootBoType = boType;
                    template.ApplicabilityMode = applicabilityMode;
                    template.VisibilityCriteria = visibilityCriteria;
                    template.SortOrder = sortOrder;
                    template.IsActive = true;
                }
                catch (Exception ex)
                {
                    // Log error but don't fail startup
                    System.Diagnostics.Debug.WriteLine(
                        $"Error seeding template '{templateName}': {ex.Message}");
                }
            }
        }

        private byte[] GetResourceBytes(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string[] resources = assembly.GetManifestResourceNames();
                    string available = string.Join(Environment.NewLine, resources);
                    throw new FileNotFoundException(
                        $"Template resource '{resourceName}' not found. " +
                        $"Ensure Build Action is 'Embedded Resource'.{Environment.NewLine}" +
                        $"Available: {available}");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
