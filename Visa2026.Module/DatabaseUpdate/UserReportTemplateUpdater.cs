using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Seeds initial user-defined Word report templates on application startup.
    /// Similar to <see cref="MailMergeUpdater"/> but for <see cref="UserReportTemplate"/>.
    /// Templates are stored as embedded resources under <c>Visa2026.Module/Resources/Templates/</c>
    /// and copied into <see cref="FileData"/> on the template row.
    /// </summary>
    public class UserReportTemplateUpdater : ModuleUpdater
    {
        private readonly XafApplication _application;

        public UserReportTemplateUpdater(XafApplication application, IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            if (_application.ServiceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UserReportTemplateUpdater: XafApplication.ServiceProvider is null; skipping seed.");
                ObjectSpace.CommitChanges();
                return;
            }

            // Host registers extractor/validator as scoped — resolve from a scope (root provider will not return them).
            using IServiceScope scope = _application.ServiceProvider.CreateScope();
            var extractor = scope.ServiceProvider.GetService<IUserReportPlaceholderExtractor>();
            var validator = scope.ServiceProvider.GetService<IUserReportValidationService>();
            if (extractor == null || validator == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UserReportTemplateUpdater: IUserReportPlaceholderExtractor or IUserReportValidationService not registered; skipping seed.");
                ObjectSpace.CommitChanges();
                return;
            }

            // Contract.docx — ApplicationItem + labor-style {{#ds.rows}}; only visa/WP extension family application types.
            EnsureTemplateExists(
                    extractor,
                    validator,
                    templateName: "Contract (seed)",
                    description: "Seeded from embedded Resources/Templates/Contract.docx; ApplicationItem template; visible for App_Visa_and_WP_Ext, App_WP_Ext, and App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Contract.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[]
                    {
                        "App_Visa_and_WP_Ext",
                        "App_WP_Ext",
                        "App_Visa_Ext_According_to_WP",
                    },
                    visibilityCriteria: null,
                    sortOrder: 50)
                .GetAwaiter()
                .GetResult();

            // Invitation + work permit (App_Inv_And_WP) — ApplicationItem root, same rows merge as labor contract family.
            EnsureTemplateExists(
                    extractor,
                    validator,
                    templateName: "Contract Inv (seed)",
                    description: "Seeded from embedded Resources/Templates/Contract_Inv.docx; ApplicationItem template; visible only for application type App_Inv_And_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Contract_Inv.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 51)
                .GetAwaiter()
                .GetResult();

            // Sazakow_uzt.docx — Application root ({{ds.*}}); visa + work permit extension applications only.
            EnsureTemplateExists(
                    extractor,
                    validator,
                    templateName: "Sazakow (seed)",
                    description: "Seeded from embedded Resources/Templates/Sazakow_uzt.docx; Application-level template; visible only for application type App_Visa_and_WP_Ext.",
                    resourceName: "Visa2026.Module.Resources.Templates.Sazakow_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 52)
                .GetAwaiter()
                .GetResult();

            ObjectSpace.CommitChanges();
        }

        /// <summary>
        /// Ensures a <see cref="UserReportTemplate"/> exists with the specified configuration.
        /// In DEBUG: Always overwrites template file content to pick up changes.
        /// In Production: Only creates if missing (preserves user edits to metadata).
        /// </summary>
        /// <param name="applicableApplicationTypeNames">When <paramref name="applicabilityMode"/> is <see cref="ApplicabilityMode.SpecificTypes"/>,
        /// lookup <see cref="ApplicationType"/> rows to link (by <c>Name</c>, e.g. <c>App_Inv_And_WP</c>). Otherwise pass <c>null</c> and any existing links are cleared.</param>
        private async Task EnsureTemplateExists(
            IUserReportPlaceholderExtractor extractor,
            IUserReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
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

            if (!shouldUpdateFile)
                return;

            try
            {
                byte[] templateBytes = GetResourceBytes(resourceName);

                if (template.TemplateFile == null)
                    template.TemplateFile = ObjectSpace.CreateObject<FileData>();

                using (var ms = new MemoryStream(templateBytes))
                {
                    template.TemplateFile.LoadFromStream(
                        fileName: Path.GetFileName(resourceName),
                        stream: ms);
                }

                using (var ms = new MemoryStream(templateBytes))
                {
                    var placeholders = await extractor.ExtractPlaceholdersAsync(ms).ConfigureAwait(false);
                    var validationResults = await validator
                        .ValidatePlaceholdersAsync(placeholders.ToList(), boType)
                        .ConfigureAwait(false);

                    foreach (var existing in template.Placeholders.ToList())
                        ObjectSpace.Delete(existing);

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

                template.Description = description;
                template.RootBoType = boType;
                template.ApplicabilityMode = applicabilityMode;
                template.VisibilityCriteria = visibilityCriteria ?? string.Empty;
                template.SortOrder = sortOrder;
                template.IsActive = true;

                SetApplicableApplicationTypes(template, applicabilityMode, applicableApplicationTypeNames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error seeding user report template '{templateName}': {ex}");
            }
        }

        /// <summary>
        /// Links <see cref="UserReportTemplate.ApplicableTypes"/> when <paramref name="applicabilityMode"/> is
        /// <see cref="ApplicabilityMode.SpecificTypes"/>; clears links for other modes.
        /// </summary>
        private void SetApplicableApplicationTypes(
            UserReportTemplate template,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames)
        {
            foreach (var linked in template.ApplicableTypes.ToList())
                template.ApplicableTypes.Remove(linked);

            if (applicabilityMode != ApplicabilityMode.SpecificTypes || applicableApplicationTypeNames == null)
                return;

            foreach (var typeName in applicableApplicationTypeNames)
            {
                var appType = ObjectSpace.FirstOrDefault<ApplicationType>(t => t.Name == typeName);
                if (appType != null)
                    template.ApplicableTypes.Add(appType);
                else
                    System.Diagnostics.Debug.WriteLine(
                        $"UserReportTemplateUpdater: ApplicationType '{typeName}' not found — '{template.TemplateName}' has no link for that name.");
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
                        $"Available:{Environment.NewLine}{available}");
                }

                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
