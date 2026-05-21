using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DocxTemplater;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <inheritdoc cref="IUserReportGenerator"/>
    public class UserReportGenerator : IUserReportGenerator
    {
        private readonly IUserReportPlaceholderExtractor _placeholderExtractor;

        public UserReportGenerator(IUserReportPlaceholderExtractor placeholderExtractor)
        {
            _placeholderExtractor = placeholderExtractor;
        }

        public async Task GenerateAsync(
            UserReportTemplate template,
            Application application,
            Stream outputStream,
            IList<ApplicationItem>? applicationItems = null)
        {
            var data = BuildDataDictionary(template, application, applicationItems);
            PopulateSyntheticRowsIfNeeded(template, application, data, applicationItems);

            await EnsureDataCoversDocumentPlaceholdersAsync(template, application, data, applicationItems)
                .ConfigureAwait(false);
            await RenderTemplateAsync(template, data, outputStream, applicationItems);
        }

        public async Task GenerateAsync(UserReportTemplate template, ApplicationItem applicationItem, Stream outputStream)
        {
            var data = BuildDataDictionary(template, applicationItem);
            if (template.RootBoType == UserReportBoType.ApplicationItem)
                data["rows"] = new List<Dictionary<string, object>> { BuildLaborContractRowDictionary(applicationItem) };

            await EnsureDataCoversDocumentPlaceholdersAsync(template, applicationItem, data).ConfigureAwait(false);
            await RenderTemplateAsync(template, data, outputStream);
        }

        private Dictionary<string, object> BuildDataDictionary(
            UserReportTemplate template,
            object rootObject,
            IList<ApplicationItem>? applicationItems = null)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Map each validated placeholder to its value
            foreach (var placeholder in template.Placeholders.Where(p => p.IsValid && !p.IsRowProperty))
            {
                var bindKey = NormalizeBindModelKey(placeholder.PlaceholderKey.TrimStart('#'));
                if (bindKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = GetPropertyValue(rootObject, placeholder.ResolvedPropertyPath);
                data[bindKey] = UserReportPlaceholderBindingHelper.CoerceMergeValue(value, placeholder.ResolvedPropertyPath);
            }

            // Handle collection placeholders
            foreach (var collectionPlaceholder in template.Placeholders.Where(p => p.IsCollection && p.IsValid))
            {
                var collectionName = NormalizeBindModelKey(collectionPlaceholder.PlaceholderKey.TrimStart('#'));
                if (string.Equals(collectionName, "rows", StringComparison.OrdinalIgnoreCase)
                    && (template.RootBoType == UserReportBoType.ApplicationItem
                        || template.RootBoType == UserReportBoType.Application))
                {
                    continue;
                }

                var collectionData = GetCollectionData(
                    rootObject,
                    collectionPlaceholder.ResolvedPropertyPath,
                    template,
                    applicationItems);
                data[collectionName] = collectionData;
            }

            return data;
        }

        /// <summary>
        /// Fills <c>{{#ds.rows}}</c> from <see cref="Application.ApplicationItems"/> when the template does not map to a real collection property.
        /// </summary>
        private static void PopulateSyntheticRowsIfNeeded(
            UserReportTemplate template,
            Application application,
            Dictionary<string, object> data,
            IList<ApplicationItem>? applicationItems = null)
        {
            if (!TemplateUsesSyntheticRowsCollection(template))
                return;

            data["rows"] = UserReportMergeDataHelper.TemplateUsesPersonListRowPlaceholders(template.Placeholders)
                ? UserReportMergeDataHelper.BuildSanawyStyleRows(application, applicationItems)
                : BuildLaborContractStyleRows(application, applicationItems);
        }

        private static bool TemplateUsesSyntheticRowsCollection(UserReportTemplate template)
        {
            if (template.Placeholders == null || !template.Placeholders.Any())
                return false;

            return template.Placeholders.Any(p =>
                p.IsValid
                && (p.IsCollection
                    && string.Equals(
                        NormalizeBindModelKey(p.PlaceholderKey.TrimStart('#')),
                        "rows",
                        StringComparison.OrdinalIgnoreCase)
                    || p.PlaceholderKey.Contains("rows.", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Row dictionary keys aligned with the built-in labor contract item Word report and Contract.docx.
        /// </summary>
        private static List<Dictionary<string, object>> BuildLaborContractStyleRows(
            Application application,
            IList<ApplicationItem>? applicationItems = null)
        {
            var items = applicationItems != null && applicationItems.Count > 0
                ? applicationItems.Where(i => i != null && !i.IsDeleted)
                : (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                    .Where(i => i != null && !i.IsDeleted);

            return items.Select(BuildLaborContractRowDictionary).ToList();
        }

        private static Dictionary<string, object> BuildLaborContractRowDictionary(ApplicationItem item)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_FullName"] = item.Person_FullName ?? string.Empty,
                ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
                ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
                ["Passport_Number"] = item.Passport_Number ?? string.Empty,
                ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
                ["Application_SponsorSignatory"] = item.Application_SponsorSignatory ?? string.Empty,
                ["Application_CompanyAddress"] = item.Application_CompanyAddress ?? string.Empty,
                ["Application_CompanyRegistryAddressLine"] = item.Application_CompanyRegistryAddressLine ?? string.Empty,
                ["CompanyHead_FullName"] = item.CompanyHead_FullName ?? string.Empty,
                ["CompanyHead_PassportLine"] = item.CompanyHead_PassportLine ?? string.Empty,
                ["Representative_FullName"] = item.Representative_FullName ?? string.Empty,
                ["Representative_PassportLine"] = item.Representative_PassportLine ?? string.Empty,
                ["Contract_StartDateText"] = item.Contract_StartDateText ?? string.Empty,
                ["Contract_ExpirationDateText"] = item.Contract_ExpirationDateText ?? string.Empty,
                ["Contract_PeriodFallbackText"] = item.Contract_PeriodFallbackText ?? string.Empty,
                ["Contract_SalaryText"] = item.Contract_SalaryText ?? string.Empty,
                ["Salary_CurrencyCode"] = item.Salary_CurrencyCode ?? string.Empty,
            };
        }

        /// <summary>
        /// DocxTemplater <c>BindModel("ds", data)</c> expects dictionary keys to be property names <em>under</em> <c>ds</c>
        /// (e.g. <c>FullApplicationNumber</c>), while authors often type <c>{{ds.FullApplicationNumber}}</c> in Word,
        /// which extracts as <c>ds.FullApplicationNumber</c>. Strip a leading <c>ds.</c> so merge matches the template.
        /// </summary>
        private static string NormalizeBindModelKey(string placeholderKeyWithoutLoopPrefix)
        {
            if (string.IsNullOrWhiteSpace(placeholderKeyWithoutLoopPrefix))
                return placeholderKeyWithoutLoopPrefix ?? string.Empty;

            var k = placeholderKeyWithoutLoopPrefix.Trim();
            if (k.StartsWith("ds.", StringComparison.OrdinalIgnoreCase) && k.Length > 3)
                return k.Substring(3);

            return k;
        }

        /// <summary>
        /// DocxTemplater requires every <c>{{ds.*}}</c> token in the file to exist on the bound model. Placeholder rows
        /// may be missing or invalid after edits; merge tokens from a live scan of the template so generation still works.
        /// </summary>
        private async Task EnsureDataCoversDocumentPlaceholdersAsync(
            UserReportTemplate template,
            object rootObject,
            Dictionary<string, object> data,
            IList<ApplicationItem>? applicationItems = null)
        {
            var content = template.TemplateFile.Content;
            if (content == null || content.Length == 0)
                return;

            using var scanStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
            var tokens = await _placeholderExtractor.ExtractPlaceholdersAsync(scanStream).ConfigureAwait(false);

            foreach (var raw in tokens.Distinct(StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                // {{/ds.Collection}} close markers — not bind keys
                if (raw.StartsWith("/", StringComparison.Ordinal))
                    continue;

                // Row tokens are merged inside collection row dictionaries only
                if (raw.StartsWith(".", StringComparison.Ordinal))
                    continue;

                var withoutHash = raw.TrimStart('#');
                var bindKey = NormalizeBindModelKey(withoutHash);
                if (string.IsNullOrEmpty(bindKey))
                    continue;

                if (raw.StartsWith("#", StringComparison.Ordinal))
                {
                    if (!data.ContainsKey(bindKey))
                        data[bindKey] = GetCollectionData(rootObject, bindKey, template, applicationItems);
                    continue;
                }

                if (bindKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase)
                    && data.ContainsKey("rows"))
                {
                    continue;
                }

                if (bindKey.StartsWith("ApplicationItems.", StringComparison.OrdinalIgnoreCase)
                    && data.ContainsKey("ApplicationItems"))
                {
                    continue;
                }

                if (UserReportPlaceholderBindingHelper.IsImageInjectorToken(raw.Trim()))
                    continue;

                if (!data.ContainsKey(bindKey))
                {
                    var value = GetPropertyValue(rootObject, bindKey);
                    data[bindKey] = UserReportPlaceholderBindingHelper.CoerceMergeValue(value, bindKey);
                }
            }
        }

        private object GetCollectionData(
            object rootObject,
            string collectionPath,
            UserReportTemplate template,
            IList<ApplicationItem>? applicationItems = null)
        {
            if (string.Equals(collectionPath, "rows", StringComparison.OrdinalIgnoreCase))
            {
                if (rootObject is Application application)
                    return UserReportMergeDataHelper.BuildSanawyStyleRows(application, applicationItems);
                if (rootObject is ApplicationItem item && item.Application != null)
                    return UserReportMergeDataHelper.BuildSanawyStyleRows(item.Application, applicationItems);
            }

            // Photo roster templates: typed rows with byte[] for WordUserReportImageInjector ({{IMAGE:Person_Photo}}).
            if (string.Equals(collectionPath, "ApplicationItems", StringComparison.OrdinalIgnoreCase))
            {
                var items = new List<object>();
                IEnumerable<ApplicationItem> source = applicationItems != null
                    ? applicationItems.Where(i => i != null && !i.IsDeleted)
                    : (GetPropertyValue(rootObject, collectionPath) as IEnumerable)?
                        .OfType<ApplicationItem>()
                        .Where(i => !i.IsDeleted)
                    ?? Enumerable.Empty<ApplicationItem>();

                foreach (var ai in source)
                    items.Add(ApplicationItemPhotoMergeRow.From(ai));

                return items;
            }

            var collection = GetPropertyValue(rootObject, collectionPath) as IEnumerable;
            if (collection == null)
                return new List<Dictionary<string, object>>();

            var rows = new List<Dictionary<string, object>>();
            int rowNum = 1;
            foreach (var item in collection)
            {
                var rowDict = new Dictionary<string, object>();
                rowDict["RowNumber"] = rowNum++;

                foreach (var rowPlaceholder in template.Placeholders.Where(p => p.IsRowProperty && p.IsValid))
                {
                    var placeholderKey = rowPlaceholder.PlaceholderKey.TrimStart('.');
                    var propertyName = UserReportPlaceholderBindingHelper.StripFormatterSuffix(placeholderKey);
                    var value = GetPropertyValue(item, propertyName);
                    rowDict[propertyName] = UserReportPlaceholderBindingHelper.CoerceMergeValue(value, propertyName);
                }

                rows.Add(rowDict);
            }

            return rows;
        }

        private object? GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrEmpty(propertyPath))
                return null;

            var parts = propertyPath.Split('.');
            var current = obj;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                    return null;

                current = property.GetValue(current);
            }

            return current;
        }

        private Task RenderTemplateAsync(
            UserReportTemplate template,
            Dictionary<string, object> data,
            Stream outputStream,
            IList<ApplicationItem>? applicationItems = null)
        {
            var content = template.TemplateFile.Content;
            if (content == null || content.Length == 0)
                throw new InvalidOperationException("User report template file has no content.");

            using var templateStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
            var docxTemplate = DocxTemplateFactory.Open(templateStream);
            docxTemplate.BindModel("ds", data);

            using var merged = new MemoryStream();
            docxTemplate.Save(merged);
            merged.Position = 0;

            var photosByKey = WordUserReportMergeImageExtractor.FromBindData(data);
            if (photosByKey.Count == 0 && applicationItems is { Count: > 0 })
                photosByKey = WordUserReportMergeImageExtractor.FromApplicationItems(applicationItems);

            if (TemplateUsesImageInjector(template, content))
            {
                using var injected = new MemoryStream();
                WordUserReportImageInjector.Inject(merged, injected, photosByKey);
                injected.Position = 0;
                injected.CopyTo(outputStream);
            }
            else
            {
                merged.CopyTo(outputStream);
            }

            return Task.CompletedTask;
        }

        private static bool TemplateUsesImageInjector(UserReportTemplate template, byte[] templateContent)
        {
            if (template.Placeholders != null
                && template.Placeholders.Any(p =>
                    UserReportPlaceholderBindingHelper.IsImageInjectorToken(p.PlaceholderKey)))
            {
                return true;
            }

            // Placeholder rows may be stale; scan embedded template bytes for {{IMAGE:…}}.
            return System.Text.Encoding.UTF8.GetString(templateContent)
                .Contains("{{IMAGE:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
