using System.Collections;
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
        public async Task GenerateAsync(UserReportTemplate template, Application application, Stream outputStream)
        {
            var data = BuildDataDictionary(template, application);
            await RenderTemplateAsync(template, data, outputStream);
        }

        public async Task GenerateAsync(UserReportTemplate template, ApplicationItem applicationItem, Stream outputStream)
        {
            var data = BuildDataDictionary(template, applicationItem);
            await RenderTemplateAsync(template, data, outputStream);
        }

        private Dictionary<string, object> BuildDataDictionary(UserReportTemplate template, object rootObject)
        {
            var data = new Dictionary<string, object>();

            // Map each validated placeholder to its value
            foreach (var placeholder in template.Placeholders.Where(p => p.IsValid && !p.IsRowProperty))
            {
                var value = GetPropertyValue(rootObject, placeholder.ResolvedPropertyPath);
                data[placeholder.PlaceholderKey.TrimStart('#')] = value ?? string.Empty;
            }

            // Handle collection placeholders
            foreach (var collectionPlaceholder in template.Placeholders.Where(p => p.IsCollection && p.IsValid))
            {
                var collectionName = collectionPlaceholder.PlaceholderKey.TrimStart('#');
                var collectionData = GetCollectionData(rootObject, collectionPlaceholder.ResolvedPropertyPath, template);
                data[collectionName] = collectionData;
            }

            return data;
        }

        private List<Dictionary<string, object>> GetCollectionData(object rootObject, string collectionPath, UserReportTemplate template)
        {
            var rows = new List<Dictionary<string, object>>();

            // Get the collection
            var collection = GetPropertyValue(rootObject, collectionPath) as IEnumerable;
            if (collection == null)
                return rows;

            int rowNum = 1;
            foreach (var item in collection)
            {
                var rowDict = new Dictionary<string, object>();
                rowDict["RowNumber"] = rowNum++;

                // Get row properties (those starting with .)
                foreach (var rowPlaceholder in template.Placeholders.Where(p => p.IsRowProperty && p.IsValid))
                {
                    var propertyName = rowPlaceholder.PlaceholderKey.TrimStart('.');
                    var value = GetPropertyValue(item, propertyName);
                    rowDict[propertyName] = value ?? string.Empty;
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

        private Task RenderTemplateAsync(UserReportTemplate template, Dictionary<string, object> data, Stream outputStream)
        {
            var content = template.TemplateFile.Content;
            if (content == null || content.Length == 0)
                throw new InvalidOperationException("User report template file has no content.");

            using var templateStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
            var docxTemplate = new DocxTemplate(templateStream);
            docxTemplate.BindModel("ds", data);
            docxTemplate.Save(outputStream);
            return Task.CompletedTask;
        }
    }
}
