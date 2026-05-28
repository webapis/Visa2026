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
        /// <summary>Links all <see cref="ProjectContract"/> rows whose <see cref="LookupBase.NameTm"/> contains this substring (case-insensitive).</summary>
        private const string Gt15ProjectContractNameTmSubstring = "GT-15";

        private static readonly (string OldName, string NewName)[] SeedTemplateNameMigrations =
        {
            ("Contract (seed)", "Contract"),
            ("Contract Inv (seed)", "Contract Inv"),
            ("Sanaw (seed)", "Sanaw"),
            ("Gurlusyk (seed)", "Gurlusyk"),
            ("433-ek sanawy (seed)", "433-ek sanawy"),
            ("Sazakow (seed)", "GT-15_Sazakow_uzt"),
            ("433-Elyasow (seed)", "GT-15_Elyasow_uzt"),
            ("433-MINSTROY (seed)", "GT-15_MINSTROY_uzt"),
        };

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
            MigrateSeedTemplateNames();

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

            // Borcnama.docx — per-person commitment form; {{#ds.rows}} with page breaks; all application types.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Borcnama",
                    description: "Seeded from Resources/Templates/Borcnama.docx; ApplicationItem template; visible for all application types.",
                    resourceName: "Visa2026.Module.Resources.Templates.Borcnama.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicableApplicationTypeNames: null,
                    visibilityCriteria: null,
                    sortOrder: 49)
                .GetAwaiter()
                .GetResult();

            // Contract_uzt.docx — ApplicationItem + labor-style {{#ds.rows}}; visa/WP extension family application types.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Contract",
                    description: "Seeded from embedded Resources/Templates/Contract_uzt.docx; ApplicationItem template; visible for App_Visa_and_WP_Ext, App_WP_Ext, and App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Contract_uzt.docx",
                    boType: UserReportBoType.ApplicationItem,
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
                    templateName: "Contract Inv",
                    description: "Seeded from embedded Resources/Templates/Contract_Inv.docx; ApplicationItem template; visible only for application type App_Inv_And_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Contract_Inv.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 51)
                .GetAwaiter()
                .GetResult();

            // GT-15_Sazakow_uzt.docx — GT-15 Çalık branch → Migration letter (Application root).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_Sazakow_uzt",
                    description: "Seeded from Resources/Templates/GT-15_Sazakow_uzt.docx; Application-level; App_Visa_and_WP_Ext; Applicable Project Contracts where NameTm contains GT-15.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_Sazakow_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 52,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // GT-15_Elyasow_uzt.docx — Application-level letter (ministry / Elyasow layout).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_Elyasow_uzt",
                    description: "Seeded from Resources/Templates/GT-15_Elyasow_uzt.docx; Application-level; App_Visa_and_WP_Ext; Applicable Project Contracts where NameTm contains GT-15.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_Elyasow_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 53,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // GT-15_MINSTROY_uzt.docx — Energy → Construction ministry letter (GT-15 project contracts only).
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_MINSTROY_uzt",
                    description: "Seeded from Resources/Templates/GT-15_MINSTROY_uzt.docx; Application-level; App_Visa_and_WP_Ext; Applicable Project Contracts where NameTm contains GT-15.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_MINSTROY_uzt.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 54,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // Sanaw_uzt.docx — Daşary ýurt raýatlarynyň sanawy (Word); App_Visa_and_WP_Ext only.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Sanaw",
                    description: "Seeded from Resources/Templates/Sanaw_uzt.docx; ApplicationItem root with {{#ds.rows}}; visible only for application type App_Visa_and_WP_Ext.",
                    resourceName: "Visa2026.Module.Resources.Templates.Sanaw_uzt.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
                    visibilityCriteria: null,
                    sortOrder: 55)
                .GetAwaiter()
                .GetResult();

            // hasaba_almak_hat.docx — Hasaba almak request letter (Çalık layout); App_Reg_Check_In only.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Hasaba almak hat",
                    description: "Seeded from Resources/Templates/hasaba_almak_hat.docx; Application-level; visible only for application type App_Reg_Check_In.",
                    resourceName: "Visa2026.Module.Resources.Templates.hasaba_almak_hat.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Reg_Check_In" },
                    visibilityCriteria: null,
                    sortOrder: 56)
                .GetAwaiter()
                .GetResult();

            // Forma_16.docx — Daşary ýurt raýatlaryny bellige alyş namasy; ItemRows + page break per ApplicationItem.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Forma 16",
                    description: "Seeded from Resources/Templates/Forma_16.docx; ApplicationItem root; Word layout ItemRows ({{#ds.rows}}); registration application types; {{IMAGE:Person_Photo}}.",
                    resourceName: "Visa2026.Module.Resources.Templates.Forma_16.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicableApplicationTypeNames: new[]
                    {
                        "App_Reg_Check_In",
                        "App_Reg_Check_In_Internal",
                        "App_Reg_Check_Out",
                        "App_Reg_Check_Out_Internal",
                        "App_Reg_ext",
                        "App_Reg_Info_Change_Address",
                        "App_Reg_Info_Change_Passport",
                        "App_Reg_Info_Change_Visa",
                    },
                    visibilityCriteria: null,
                    sortOrder: 57)
                .GetAwaiter()
                .GetResult();

            // GT-15_Elyasow_ckl.docx — Çalık GT-15 letter to Turkmenenergo (Elýasowa); App_Inv_And_WP; static ministry/GT-15 blocks per map.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_Elyasow_ckl",
                    description: "Seeded from Resources/Templates/GT-15_Elyasow_ckl.docx; Application-level AppScalar; App_Inv_And_WP; GT-15 project contracts (NameTm contains GT-15); yellow-only dynamic fields per GT-15_Elyasow_ckl_map.md.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_Elyasow_ckl.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 58,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // GT-15_Elyasow_ckl_only.docx — Çalık GT-15 çakylyk-only letter (Elýasowa); App_Inv; per GT-15_Elyasow_ckl_only_map.md.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_Elyasow_ckl_only",
                    description: "Seeded from Resources/Templates/GT-15_Elyasow_ckl_only.docx; Application-level AppScalar; App_Inv only; GT-15 project contracts; çakylyk-only B2/G1 static per GT-15_Elyasow_ckl_only_map.md.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_Elyasow_ckl_only.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Inv" },
                    visibilityCriteria: null,
                    sortOrder: 60,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // GT-15_Migrasiya_ckl_hat.docx — Energetika → Döwlet migrasiýa gullugy; çakylyk request; static Ministr / A.Saparow; App_Inv + App_Inv_And_WP per map.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "GT-15_Migrasiya_ckl_hat",
                    description: "Seeded from Resources/Templates/GT-15_Migrasiya_ckl_hat.docx; Application AppScalar; App_Inv, App_Inv_And_WP; GT-15; static ministry signatory Ministr / A.Saparow; dynamic Urgency, TotalPersonCount*, VisaPeriod/Category per GT-15_Migrasiya_ckl_hat_map.md.",
                    resourceName: "Visa2026.Module.Resources.Templates.GT-15_Migrasiya_ckl_hat.docx",
                    boType: UserReportBoType.Application,
                    applicableApplicationTypeNames: new[] { "App_Inv", "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 65,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // Sanaw_ckl.docx — Çalık GT-15 Daşary ýurt raýatlarynyň sanawy (14-col ItemRows); App_Inv + App_Inv_And_WP per Sanaw_ckl_map.md.
            EnsureTemplateExists(
                    wordExtractor,
                    wordValidator,
                    templateName: "Sanaw_ckl",
                    description: "Seeded from Resources/Templates/Sanaw_ckl.docx; ApplicationItem root; Word layout ItemRows ({{#ds.rows}}); App_Inv, App_Inv_And_WP; GT-15 project contracts (NameTm contains GT-15); signatory {{ds.Application_CompanyHead_*}} per map.",
                    resourceName: "Visa2026.Module.Resources.Templates.Sanaw_ckl.docx",
                    boType: UserReportBoType.ApplicationItem,
                    applicableApplicationTypeNames: new[] { "App_Inv", "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 59,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // 433_gurlusyk_uzt.xlsx — ministry-style personnel sanawy (Gurlusyk layout); save source .xls as .xlsx before embed.
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "Gurlusyk",
                    description: "Seeded from Resources/Templates/Excel/433_gurlusyk_uzt.xlsx; ApplicationItem list with {{#ds.rows}} / {{.…}} columns; App_WP_Ext, App_Visa_and_WP_Ext, App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.433_gurlusyk_uzt.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
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

            // 433_gurlusyk_ckl.xlsx — Çakylyk sanawy (Gurlusyk layout); static ministry footer; App_Inv_And_WP + GT-15.
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "Gurlusyk ckl",
                    description: "Seeded from Resources/Templates/Excel/433_gurlusyk_ckl.xlsx; ApplicationItem list with {{#ds.rows}} / {{.…}} columns; App_Inv_And_WP only; GT-15; Möhleti column uses Çakylyk + Application_VisaPeriod/Category; static footer Ministr / A.Saparow.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.433_gurlusyk_ckl.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
                    applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 66,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // Sanaw_ckl.xlsx — Çalık GT-15 Daşary ýurt raýatlarynyň sanawy (Excel); App_Inv + App_Inv_And_WP per Excel/Sanaw_ckl_map.md.
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "Sanaw_ckl (Excel)",
                    description: "Seeded from Resources/Templates/Excel/Sanaw_ckl.xlsx; 14-column ApplicationItem list (cols B–N); ItemList merge; App_Inv, App_Inv_And_WP; GT-15; signatory {{ds.Application_CompanyHead_*}} per map.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.Sanaw_ckl.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
                    applicableApplicationTypeNames: new[] { "App_Inv", "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 63,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // Sanaw_ckl_ministr_saparov.xlsx — same sanawy as Sanaw_ckl (Excel); static footer Ministr / A.Saparow per map.
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "Sanaw_ckl_ministr_saparov (Excel)",
                    description: "Seeded from Resources/Templates/Excel/Sanaw_ckl_ministr_saparov.xlsx; same ItemList layout as Sanaw_ckl (Excel); App_Inv, App_Inv_And_WP; GT-15; static signatory Ministr / A.Saparow (no CompanyHead placeholders).",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.Sanaw_ckl_ministr_saparov.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
                    applicableApplicationTypeNames: new[] { "App_Inv", "App_Inv_And_WP" },
                    visibilityCriteria: null,
                    sortOrder: 64,
                    applicableProjectContractNameTmContains: Gt15ProjectContractNameTmSubstring)
                .GetAwaiter()
                .GetResult();

            // 433-ek_uzt.xlsx — 15-column Daşary ýurt raýatlarynyň sanawy (from ministry .xls layout).
            EnsureExcelTemplateExists(
                    excelExtractor,
                    excelValidator,
                    templateName: "433-ek sanawy",
                    description: "Seeded from Resources/Templates/Excel/433-ek_uzt.xlsx; 15-column ApplicationItem list; App_WP_Ext, App_Visa_and_WP_Ext, App_Visa_Ext_According_to_WP.",
                    resourceName: "Visa2026.Module.Resources.Templates.Excel.433-ek_uzt.xlsx",
                    boType: UserReportBoType.ApplicationItem,
                    excelMergeMode: ExcelMergeMode.ItemList,
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
        /// <param name="applicableApplicationTypeNames">When non-empty, links <see cref="ApplicationType"/> rows by <c>Name</c> (e.g. <c>App_Inv_And_WP</c>). Pass <c>null</c> or empty for all application types.</param>
        /// <param name="applicableProjectContractNames">Optional <see cref="ProjectContract"/> <c>Name</c> values for exact-match filtering; <c>null</c> clears links.</param>
        /// <param name="applicableProjectContractNameTmContains">When set, links every <see cref="ProjectContract"/> whose <see cref="LookupBase.NameTm"/> contains this substring (case-insensitive).</param>
        private Task EnsureExcelTemplateExists(
            IExcelTemplatePlaceholderExtractor extractor,
            IExcelReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            ExcelMergeMode excelMergeMode,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            IReadOnlyList<string> applicableProjectContractNames = null,
            string applicableProjectContractNameTmContains = null) =>
            EnsureTemplateExists(
                extractor,
                validator,
                templateName,
                description,
                resourceName,
                boType,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                TemplateOutputFormat.Excel,
                excelMergeMode,
                applicableProjectContractNames,
                applicableProjectContractNameTmContains);

        private async Task EnsureTemplateExists(
            IUserReportPlaceholderExtractor extractor,
            IUserReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat = TemplateOutputFormat.Word,
            ExcelMergeMode excelMergeMode = ExcelMergeMode.ItemList,
            IReadOnlyList<string> applicableProjectContractNames = null,
            string applicableProjectContractNameTmContains = null) =>
            EnsureTemplateExistsCore(
                async stream => (await extractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false)).ToList(),
                async (keys, bo) => await validator.ValidatePlaceholdersAsync(keys, bo).ConfigureAwait(false),
                templateName,
                description,
                resourceName,
                boType,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                outputFormat,
                excelMergeMode,
                applicableProjectContractNames,
                applicableProjectContractNameTmContains);

        private async Task EnsureTemplateExists(
            IExcelTemplatePlaceholderExtractor extractor,
            IExcelReportValidationService validator,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode,
            IReadOnlyList<string> applicableProjectContractNames = null,
            string applicableProjectContractNameTmContains = null) =>
            EnsureTemplateExistsCore(
                async stream => (await extractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false)).ToList(),
                async (keys, bo) => await validator.ValidatePlaceholdersAsync(keys, bo, excelMergeMode).ConfigureAwait(false),
                templateName,
                description,
                resourceName,
                boType,
                applicableApplicationTypeNames,
                visibilityCriteria,
                sortOrder,
                outputFormat,
                excelMergeMode,
                applicableProjectContractNames,
                applicableProjectContractNameTmContains);

        private async Task EnsureTemplateExistsCore(
            Func<Stream, Task<List<string>>> extractAsync,
            Func<List<string>, UserReportBoType, Task<IList<PlaceholderValidationResult>>> validateAsync,
            string templateName,
            string description,
            string resourceName,
            UserReportBoType boType,
            IReadOnlyList<string> applicableApplicationTypeNames,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode,
            IReadOnlyList<string> applicableProjectContractNames,
            string applicableProjectContractNameTmContains)
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
                            fileName: GetSeedTemplateFileName(resourceName),
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
                    applicableApplicationTypeNames,
                    applicableProjectContractNames,
                    applicableProjectContractNameTmContains,
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
            IReadOnlyList<string> applicableApplicationTypeNames,
            IReadOnlyList<string> applicableProjectContractNames,
            string applicableProjectContractNameTmContains,
            string visibilityCriteria,
            int sortOrder,
            TemplateOutputFormat outputFormat,
            ExcelMergeMode excelMergeMode)
        {
            template.Description = description;
            template.RootBoType = boType;
            template.TemplateOutputFormat = outputFormat;
            template.ExcelMergeMode = excelMergeMode;
            template.VisibilityCriteria = visibilityCriteria ?? string.Empty;
            template.SortOrder = sortOrder;
            template.IsActive = true;

            SetApplicableApplicationTypes(template, applicableApplicationTypeNames);
            SetApplicableProjectContracts(
                template,
                applicableProjectContractNames,
                applicableProjectContractNameTmContains);
            ObjectSpace.SetModified(template);
        }

        /// <summary>
        /// Links application types via <see cref="UserReportTemplate.ApplicableTypeLinks"/>.
        /// Pass <c>null</c> or empty to clear links (all application types).
        /// </summary>
        private void SetApplicableApplicationTypes(
            UserReportTemplate template,
            IReadOnlyList<string> applicableApplicationTypeNames)
        {
            ClearApplicableTypeLinks(template);

            if (applicableApplicationTypeNames == null || applicableApplicationTypeNames.Count == 0)
                return;

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

        /// <summary>Renames legacy seed rows so <see cref="EnsureTemplateExists"/> updates in place instead of orphaning.</summary>
        private void MigrateSeedTemplateNames()
        {
            foreach (var (oldName, newName) in SeedTemplateNameMigrations)
            {
                var legacy = ObjectSpace.FirstOrDefault<UserReportTemplate>(t => t.TemplateName == oldName);
                if (legacy == null)
                    continue;

                var existingNew = ObjectSpace.FirstOrDefault<UserReportTemplate>(
                    t => t.TemplateName == newName && t.ID != legacy.ID);
                if (existingNew != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"UserReportTemplateUpdater: '{newName}' already exists; removing duplicate legacy '{oldName}'.");
                    ObjectSpace.Delete(legacy);
                    continue;
                }

                legacy.TemplateName = newName;
                ObjectSpace.SetModified(legacy);
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
        /// Links <see cref="UserReportTemplate.ApplicableProjectContractLinks"/> by exact <see cref="ProjectContract.Name"/>
        /// and/or every contract whose <see cref="LookupBase.NameTm"/> contains <paramref name="applicableProjectContractNameTmContains"/>.
        /// Pass no names and no substring to clear links (no project-contract filter).
        /// </summary>
        private void SetApplicableProjectContracts(
            UserReportTemplate template,
            IReadOnlyList<string> applicableProjectContractNames,
            string applicableProjectContractNameTmContains)
        {
            ClearApplicableProjectContractLinks(template);

            var linkedContractIds = new HashSet<Guid>();

            if (!string.IsNullOrWhiteSpace(applicableProjectContractNameTmContains))
            {
                var needle = applicableProjectContractNameTmContains.Trim();
                foreach (var projectContract in ObjectSpace.GetObjectsQuery<ProjectContract>()
                             .AsEnumerable()
                             .Where(c => !string.IsNullOrWhiteSpace(c.NameTm)
                                         && c.NameTm.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                {
                    AddProjectContractLink(template, projectContract, linkedContractIds);
                }
            }

            if (applicableProjectContractNames == null || applicableProjectContractNames.Count == 0)
                return;

            var contractsByName = ObjectSpace.GetObjectsQuery<ProjectContract>()
                .AsEnumerable()
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

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

                AddProjectContractLink(template, projectContract, linkedContractIds);
            }
        }

        private void AddProjectContractLink(
            UserReportTemplate template,
            ProjectContract projectContract,
            HashSet<Guid> linkedContractIds)
        {
            projectContract = ObjectSpace.GetObject(projectContract);
            if (!linkedContractIds.Add(projectContract.ID))
                return;

            var link = ObjectSpace.CreateObject<UserReportTemplateProjectContract>();
            link.UserReportTemplate = template;
            link.ProjectContract = projectContract;
            template.ApplicableProjectContractLinks.Add(link);
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

        /// <summary>Maps embedded manifest name to a normal file name (e.g. <c>Sanaw_ckl.docx</c>).</summary>
        private static string GetSeedTemplateFileName(string resourceName)
        {
            const string templatesPrefix = "Visa2026.Module.Resources.Templates.";
            if (resourceName.StartsWith(templatesPrefix, StringComparison.Ordinal))
                return resourceName.Substring(templatesPrefix.Length);

            return Path.GetFileName(resourceName);
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
