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
using Visa2026.Module.Services.ExcelReports;
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
        /// <summary>Matches former <c>AppVisaWPExtEnergyToConstructionMinistryLetterReportDef</c> GT-15 project filter.</summary>
        private const string Gt15ProjectContractVisibilityCriteria =
            "Iif(IsNull([ProjectContract.NameTm]), false, Contains(Upper([ProjectContract.NameTm]), 'GT-15'))";

        private readonly XafApplication _application;

        public UserReportTemplateUpdater(XafApplication application, IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            EnsureFilteredUniqueLinkIndexes();

            if (_application.ServiceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UserReportTemplateUpdater: XafApplication.ServiceProvider is null; skipping seed.");
                ObjectSpace.CommitChanges();
                return;
            }

            // Host registers extractor/validator as scoped — resolve from a scope (root provider will not return them).
            using IServiceScope scope = _application.ServiceProvider.CreateScope();
            var wordExtractor = scope.ServiceProvider.GetService<IUserReportPlaceholderExtractor>();
            var wordValidator = scope.ServiceProvider.GetService<IUserReportValidationService>();
            var excelExtractor = scope.ServiceProvider.GetService<IExcelTemplatePlaceholderExtractor>();
            var excelValidator = scope.ServiceProvider.GetService<IExcelReportValidationService>();
            if (wordExtractor == null || wordValidator == null || excelExtractor == null || excelValidator == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "UserReportTemplateUpdater: user report extractors/validators not registered; skipping seed.");
                ObjectSpace.CommitChanges();
                return;
            }

            // Contract_uzt.docx — ApplicationItem + labor-style {{#ds.rows}}; visa/WP extension family application types.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Contract (seed)",
                    description: "Seeded from embedded Resources/Templates/Contract_uzt.docx; ApplicationItem template; visible for App_Visa_and_WP_Ext, App_WP_Ext, and App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Contract_uzt.docx",
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
                    wordExtractor,
                    wordValidator,
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

            // Sazakow_uzt.docx — GT-15 Çalık branch → Migration letter (Application root).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Sazakow (seed)",
                    description: "Seeded from Resources/Templates/Sazakow_uzt.docx; Application-level; App_Visa_and_WP_Ext when ProjectContract.NameTm contains GT-15.",
                    resourceName: "Visa2026.Module.Resources.Templates.Sazakow_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: Gt15ProjectContractVisibilityCriteria,
                    sortOrder: 52)
                .GetAwaiter()
                .GetResult();

            // 433_Elyasow_uzt.docx — Application-level letter (ministry / Elyasow layout).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "433-Elyasow (seed)",
                    description: "Seeded from Resources/Templates/433_Elyasow_uzt.docx; Application-level; App_Visa_and_WP_Ext only.",
                    resourceName: "Visa2026.Module.Resources.Templates.433_Elyasow_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 53)
                .GetAwaiter()
                .GetResult();

            // 433_MINSTROY_uzt.docx — Energy → Construction ministry letter (GT-15 project contracts only).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "433-MINSTROY (seed)",
                    description: "Seeded from Resources/Templates/433_MINSTROY_uzt.docx; Application-level; App_Visa_and_WP_Ext when ProjectContract.NameTm contains GT-15.",
                    resourceName: "Visa2026.Module.Resources.Templates.433_MINSTROY_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: Gt15ProjectContractVisibilityCriteria,
                    sortOrder: 54)
                .GetAwaiter()
                .GetResult();

            // Sanaw_uzt.docx — Daşary ýurt raýatlarynyň sanawy (Word); App_Visa_and_WP_Ext only.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Sanaw (seed)",
                    description: "Seeded from Resources/Templates/Sanaw_uzt.docx; ApplicationItem root with {{#ds.rows}}; visible only for application type App_Visa_and_WP_Ext.",
                    resourceName: "Visa2026.Module.Resources.Templates.Sanaw_uzt.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 55)
                .GetAwaiter()
                .GetResult();

            // 433_gurlusyk_uzt.xlsx — ministry-style personnel sanawy (Gurlusyk layout); save source .xls as .xlsx before embed.
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "Gurlusyk (seed)",
                    description: "Seeded from Resources/Templates/Excel/433_gurlusyk_uzt.xlsx; ApplicationItem list with {{#ds.rows}} / {{.…}} columns; App_WP_Ext, App_Visa_and_WP_Ext, App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.433_gurlusyk_uzt.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[]
                    {
                        "App_WP_Ext",
                        "App_Visa_and_WP_Ext",
                        "App_Visa_Ext_According_to_WP",
                    },
                    visibilityCriteria: null,
                    sortOrder: 61)
                .GetAwaiter()
                .GetResult();

            // 433-ek_uzt.xlsx — 15-column Daşary ýurt raýatlarynyň sanawy (from ministry .xls layout).
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "433-ek sanawy (seed)",
                    description: "Seeded from Resources/Templates/Excel/433-ek_uzt.xlsx; 15-column ApplicationItem list; App_WP_Ext, App_Visa_and_WP_Ext, App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.433-ek_uzt.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
                    applicabilityMode: ApplicabilityMode.SpecificTypes,
                    applicableApplicationTypeNames: new[]
                    {
                        "App_WP_Ext",
                        "App_Visa_and_WP_Ext",
                        "App_Visa_Ext_According_to_WP",
                    },
                    visibilityCriteria: null,
                    sortOrder: 62)
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
        /// <param name="applicableProjectContractNames">Optional <see cref="ProjectContract"/> <c>Name</c> values for exact-match filtering; <c>null</c> clears links.</param>
        private Task EnsureExcelTemplateExists(
            IExcelTemplatePlaceholderExtractor extractor,
            IExcelReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ExcelMergeMode excelMergeMode,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            IReadOnlyList<string> applicableProjectContractNames = null) =>
            EnsureTemplateExists(
                extractor,
                validator,
                templateName,
                description,
                resourceName,
                boType,
                applicabilityMode,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                TemplateOutputFormat.Excel,
                excelMergeMode,
                applicableProjectContractNames);

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
            int sortOrder,
            TemplateOutputFormat outputFormat = TemplateOutputFormat.Word,
            ExcelMergeMode excelMergeMode = ExcelMergeMode.ItemList,
            IReadOnlyList<string> applicableProjectContractNames = null) =>
            EnsureTemplateExistsCore(
                async stream => (await extractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false)).ToList(),
                async (keys, bo) => await validator.ValidatePlaceholdersAsync(keys, bo).ConfigureAwait(false),
                templateName,
                description,
                resourceName,
                boType,
                applicabilityMode,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                outputFormat,
                excelMergeMode,
                applicableProjectContractNames);

        private async Task EnsureTemplateExists(
            IExcelTemplatePlaceholderExtractor extractor,
            IExcelReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode,
            IReadOnlyList<string> applicableProjectContractNames = null) =>
            EnsureTemplateExistsCore(
                async stream => (await extractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false)).ToList(),
                async (keys, bo) => await validator.ValidatePlaceholdersAsync(keys, bo, excelMergeMode).ConfigureAwait(false),
                templateName,
                description,
                resourceName,
                boType,
                applicabilityMode,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                outputFormat,
                excelMergeMode,
                applicableProjectContractNames);

        private async Task EnsureTemplateExistsCore(
            Func<Stream, Task<List<string>>> extractAsync,
            Func<List<string>, UserReportBoType, Task<IList<PlaceholderValidationResult>>> validateAsync,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode,
            IReadOnlyList<string> applicableProjectContractNames)
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

            try
            {
                if (shouldUpdateFile)
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
                        var placeholders = await extractAsync(ms).ConfigureAwait(false);
                        var validationResults = await validateAsync(placeholders, boType).ConfigureAwait(false);

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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error seeding user report template '{templateName}': {ex}");
            }
            finally
            {
                ApplyTemplateSeedMetadata(
                    template,
                    description,
                    boType,
                    applicabilityMode,
                    applicableApplicationTypeNames,
                    applicableProjectContractNames,
                    visibilityCriteria,
                    sortOrder,
                    outputFormat,
                    excelMergeMode);
            }
        }

        private void ApplyTemplateSeedMetadata(
            UserReportTemplate template,
            string description,
            UserReportBoType boType,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
            IReadOnlyList<string> applicableProjectContractNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode)
        {
            template.Description = description;
            template.RootBoType = boType;
            template.TemplateOutputFormat = outputFormat;
            template.ExcelMergeMode = excelMergeMode;
            template.ApplicabilityMode = applicabilityMode;
            template.VisibilityCriteria = visibilityCriteria ?? string.Empty;
            template.SortOrder = sortOrder;
            template.IsActive = true;

            SetApplicableApplicationTypes(template, applicabilityMode, applicableApplicationTypeNames);
            SetApplicableProjectContracts(template, applicableProjectContractNames);
            ObjectSpace.SetModified(template);
        }

        /// <summary>
        /// Links application types via <see cref="UserReportTemplate.ApplicableTypeLinks"/> when
        /// <paramref name="applicabilityMode"/> is <see cref="ApplicabilityMode.SpecificTypes"/>.
        /// </summary>
        private void SetApplicableApplicationTypes(
            UserReportTemplate template,
            ApplicabilityMode applicabilityMode,
            IReadOnlyList<string> applicableApplicationTypeNames)
        {
            ClearApplicableTypeLinks(template);

            if (applicabilityMode != ApplicabilityMode.SpecificTypes
                || applicableApplicationTypeNames == null
                || applicableApplicationTypeNames.Count == 0)
            {
                return;
            }

            var typesByName = ObjectSpace.GetObjectsQuery<ApplicationType>()
                .AsEnumerable()
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var linkedTypeIds = new HashSet<Guid>();

            foreach (var typeName in applicableApplicationTypeNames)
            {
                if (string.IsNullOrWhiteSpace(typeName))
                    continue;

                if (!typesByName.TryGetValue(typeName.Trim(), out var appType))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"UserReportTemplateUpdater: ApplicationType '{typeName}' not found — '{template.TemplateName}' has no link for that name.");
                    continue;
                }

                appType = ObjectSpace.GetObject(appType);
                if (!linkedTypeIds.Add(appType.ID))
                    continue;

                var link = ObjectSpace.CreateObject<UserReportTemplateApplicationType>();
                link.UserReportTemplate = template;
                link.ApplicationType = appType;
                template.ApplicableTypeLinks.Add(link);
            }
        }

        private void ClearApplicableTypeLinks(UserReportTemplate template)
        {
            if (!ObjectSpace.IsNewObject(template))
            {
                // Physical delete: soft-deleted (GCRecord) rows still occupy the unique index.
                ExecuteNonQueryCommand(
                    $"DELETE FROM dbo.UserReportTemplateApplicationTypes WHERE UserReportTemplateId = '{template.ID:D}'",
                    silent: true);
                ObjectSpace.ReloadObject(template);
                return;
            }

            foreach (var link in template.ApplicableTypeLinks.ToList())
            {
                ObjectSpace.Delete(link);
                template.ApplicableTypeLinks.Remove(link);
            }
        }

        /// <summary>
        /// Links <see cref="UserReportTemplate.ApplicableProjectContractLinks"/> by <see cref="ProjectContract.Name"/>.
        /// Pass <c>null</c> or empty to clear links (no project-contract filter).
        /// </summary>
        private void SetApplicableProjectContracts(
            UserReportTemplate template,
            IReadOnlyList<string> applicableProjectContractNames)
        {
            ClearApplicableProjectContractLinks(template);

            if (applicableProjectContractNames == null || applicableProjectContractNames.Count == 0)
                return;

            var contractsByName = ObjectSpace.GetObjectsQuery<ProjectContract>()
                .AsEnumerable()
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var linkedContractIds = new HashSet<Guid>();

            foreach (var contractName in applicableProjectContractNames)
            {
                if (string.IsNullOrWhiteSpace(contractName))
                    continue;

                if (!contractsByName.TryGetValue(contractName.Trim(), out var projectContract))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"UserReportTemplateUpdater: ProjectContract '{contractName}' not found — '{template.TemplateName}' has no link for that name.");
                    continue;
                }

                projectContract = ObjectSpace.GetObject(projectContract);
                if (!linkedContractIds.Add(projectContract.ID))
                    continue;

                var link = ObjectSpace.CreateObject<UserReportTemplateProjectContract>();
                link.UserReportTemplate = template;
                link.ProjectContract = projectContract;
                template.ApplicableProjectContractLinks.Add(link);
            }
        }

        private void ClearApplicableProjectContractLinks(UserReportTemplate template)
        {
            if (!ObjectSpace.IsNewObject(template))
            {
                ExecuteNonQueryCommand(
                    $"DELETE FROM dbo.UserReportTemplateProjectContracts WHERE UserReportTemplateId = '{template.ID:D}'",
                    silent: true);
                ObjectSpace.ReloadObject(template);
                return;
            }

            foreach (var link in template.ApplicableProjectContractLinks.ToList())
            {
                ObjectSpace.Delete(link);
                template.ApplicableProjectContractLinks.Remove(link);
            }
        }

        /// <summary>
        /// Recreates link-table unique indexes with <c>WHERE [GCRecord] IS NULL</c> so soft-deleted rows do not block re-linking.
        /// </summary>
        private void EnsureFilteredUniqueLinkIndexes()
        {
            ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.UserReportTemplateApplicationTypes', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserReportTemplateApplicationTypes')
          AND name = N'IX_UserReportTemplateApplicationTypes_UserReportTemplateId_ApplicationTypeId'
          AND has_filter = 0)
        DROP INDEX [IX_UserReportTemplateApplicationTypes_UserReportTemplateId_ApplicationTypeId]
            ON [dbo].[UserReportTemplateApplicationTypes];

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserReportTemplateApplicationTypes')
          AND name = N'IX_UserReportTemplateApplicationTypes_UserReportTemplateId_ApplicationTypeId')
        CREATE UNIQUE INDEX [IX_UserReportTemplateApplicationTypes_UserReportTemplateId_ApplicationTypeId]
            ON [dbo].[UserReportTemplateApplicationTypes] ([UserReportTemplateId], [ApplicationTypeId])
            WHERE [GCRecord] IS NULL;
END

IF OBJECT_ID(N'dbo.UserReportTemplateProjectContracts', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserReportTemplateProjectContracts')
          AND name = N'IX_UserReportTemplateProjectContracts_UserReportTemplateId_ProjectContractId'
          AND has_filter = 0)
        DROP INDEX [IX_UserReportTemplateProjectContracts_UserReportTemplateId_ProjectContractId]
            ON [dbo].[UserReportTemplateProjectContracts];

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserReportTemplateProjectContracts')
          AND name = N'IX_UserReportTemplateProjectContracts_UserReportTemplateId_ProjectContractId')
        CREATE UNIQUE INDEX [IX_UserReportTemplateProjectContracts_UserReportTemplateId_ProjectContractId]
            ON [dbo].[UserReportTemplateProjectContracts] ([UserReportTemplateId], [ProjectContractId])
            WHERE [GCRecord] IS NULL;
END
", silent: true);
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
