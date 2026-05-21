using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <inheritdoc cref="IUserReportValidationService"/>
    public class UserReportValidationService : IUserReportValidationService
    {
        private readonly ITypesInfo _typesInfo;

        public UserReportValidationService(ITypesInfo typesInfo)
        {
            _typesInfo = typesInfo;
        }

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(IList<string> placeholders, UserReportBoType boType)
        {
            var rootType = GetRootType(boType);
            var results = new List<PlaceholderValidationResult>();

            foreach (var placeholder in placeholders)
            {
                var result = ValidateSinglePlaceholder(placeholder, rootType);
                results.Add(result);
            }

            return Task.FromResult<IList<PlaceholderValidationResult>>(results);
        }

        private PlaceholderValidationResult ValidateSinglePlaceholder(string placeholder, Type rootType)
        {
            var result = new PlaceholderValidationResult
            {
                PlaceholderKey = placeholder,
                IsCollection = placeholder.StartsWith("#"),
                IsRowProperty = placeholder.StartsWith(".")
            };

            // Clean the placeholder (remove # or . prefix)
            var cleanPlaceholder = placeholder.TrimStart('#', '.');

            try
            {
                // Word templates use {{ds.Property}} — "ds" is DocxTemplater's model name, not an Application member.
                var propertyPath = UserReportPlaceholderBindingHelper.StripFormatterSuffix(
                    StripDocxModelPrefix(cleanPlaceholder.Trim()));

                // {{/ds.rows}} close markers — not a property path
                if (propertyPath.StartsWith("/", StringComparison.Ordinal))
                {
                    result.IsValid = true;
                    result.ResolvedPath = propertyPath.TrimStart('/');
                    return result;
                }

                // DocxTemplater separators ({{:s:}}, {{:PageBreak}}, etc.)
                if (IsDocxTemplaterSeparator(propertyPath))
                {
                    result.IsValid = true;
                    result.ResolvedPath = propertyPath;
                    return result;
                }

                // {{IMAGE:Person_Photo}} — injected after merge; property is on ApplicationItem.
                if (UserReportPlaceholderBindingHelper.IsImageInjectorToken(cleanPlaceholder.Trim()))
                {
                    var photoPath = UserReportPlaceholderBindingHelper.StripFormatterSuffix(cleanPlaceholder.Trim());
                    var onItem = rootType == typeof(Application) ? typeof(ApplicationItem) : rootType;
                    var ok = PropertyExists(onItem, photoPath);
                    result.IsValid = ok;
                    result.ResolvedPath = photoPath;
                    result.ExampleValue = ok ? "(photo bytes)" : string.Empty;
                    if (!ok)
                        result.ErrorMessage = $"Property '{photoPath}' not found on {onItem.Name}";
                    return result;
                }

                // {{#ds.rows}} — list of ApplicationItem-shaped dictionaries (filled from Application.ApplicationItems at runtime)
                if (result.IsCollection && string.Equals(propertyPath, "rows", StringComparison.OrdinalIgnoreCase))
                {
                    bool ok = ValidateRowsCollection(rootType);
                    result.IsValid = ok;
                    result.ResolvedPath = "rows";
                    result.ExampleValue = ok ? "(one section per application item)" : string.Empty;
                    if (!ok)
                        result.ErrorMessage = $"'rows' loop is not supported for root type {rootType.Name}";
                    return result;
                }

                // {{#ds.ApplicationItems}} on Application root (photo roster, etc.)
                if (result.IsCollection && string.Equals(propertyPath, "ApplicationItems", StringComparison.OrdinalIgnoreCase))
                {
                    var ok = rootType == typeof(Application);
                    result.IsValid = ok;
                    result.ResolvedPath = "ApplicationItems";
                    result.ExampleValue = ok ? "(one row per application item)" : string.Empty;
                    if (!ok)
                        result.ErrorMessage = $"'ApplicationItems' loop requires root type {nameof(Application)}";
                    return result;
                }

                // {{ds.rows.Application_SponsorName}} — row fields live on ApplicationItem (Contract.docx user template)
                if (propertyPath.StartsWith("rows.", StringComparison.OrdinalIgnoreCase) && propertyPath.Length > 5)
                {
                    var onItemPath = UserReportPlaceholderBindingHelper.StripFormatterSuffix(propertyPath.Substring(5));
                    if (string.Equals(onItemPath, "RowNo", StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = true;
                        result.ResolvedPath = "RowNo";
                        result.ExampleValue = "1";
                        return result;
                    }

                    var ok = PropertyExists(typeof(ApplicationItem), onItemPath);
                    result.IsValid = ok;
                    result.ResolvedPath = onItemPath;
                    if (ok)
                        result.ExampleValue = GetExampleValue(typeof(ApplicationItem), onItemPath);
                    else
                        result.ErrorMessage = $"Property '{onItemPath}' not found on {nameof(ApplicationItem)}";
                    return result;
                }

                // {{ds.ApplicationItems.Person_FullName}} — prefer {{.Person_FullName}} in Word; still validate on ApplicationItem.
                if (propertyPath.StartsWith("ApplicationItems.", StringComparison.OrdinalIgnoreCase)
                    && propertyPath.Length > "ApplicationItems.".Length)
                {
                    var onItemPath = UserReportPlaceholderBindingHelper.StripFormatterSuffix(
                        propertyPath.Substring("ApplicationItems.".Length));
                    var ok = PropertyExists(typeof(ApplicationItem), onItemPath);
                    result.IsValid = ok;
                    result.ResolvedPath = onItemPath;
                    if (ok)
                        result.ExampleValue = GetExampleValue(typeof(ApplicationItem), onItemPath);
                    else
                        result.ErrorMessage = $"Property '{onItemPath}' not found on {nameof(ApplicationItem)}";
                    return result;
                }

                // {{.Person_FullName}} inside {{#ds.ApplicationItems}} — row fields live on ApplicationItem (Application root).
                if (result.IsRowProperty && rootType == typeof(Application))
                {
                    var ok = PropertyExists(typeof(ApplicationItem), propertyPath);
                    result.IsValid = ok;
                    result.ResolvedPath = propertyPath;
                    if (ok)
                        result.ExampleValue = GetExampleValue(typeof(ApplicationItem), propertyPath);
                    else
                        result.ErrorMessage = $"Property '{propertyPath}' not found on {nameof(ApplicationItem)}";
                    return result;
                }

                var isValid = PropertyExists(rootType, propertyPath);
                var resolvedOn = rootType;

                if (!isValid && rootType == typeof(ApplicationItem)
                    && !propertyPath.StartsWith("rows.", StringComparison.OrdinalIgnoreCase)
                    && PropertyExists(typeof(Application), propertyPath))
                {
                    isValid = true;
                    resolvedOn = typeof(Application);
                }

                result.IsValid = isValid;
                result.ResolvedPath = propertyPath;

                if (isValid)
                {
                    result.ExampleValue = GetExampleValue(resolvedOn, propertyPath);
                }
                else
                {
                    result.ErrorMessage = $"Property '{propertyPath}' not found on {rootType.Name}";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
            }

            return result;
        }

        /// <summary>Maps template token <c>ds.FullApplicationNumber</c> to BO path <c>FullApplicationNumber</c>.</summary>
        private static bool IsDocxTemplaterSeparator(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return false;

            if (propertyPath.StartsWith(":", StringComparison.Ordinal))
                return true;

            return string.Equals(propertyPath, "s", StringComparison.OrdinalIgnoreCase)
                || string.Equals(propertyPath, "PageBreak", StringComparison.OrdinalIgnoreCase);
        }

        private static string StripDocxModelPrefix(string pathFromTemplate)
        {
            if (string.IsNullOrWhiteSpace(pathFromTemplate))
                return pathFromTemplate ?? string.Empty;

            var p = pathFromTemplate.Trim();
            if (p.StartsWith("ds.", StringComparison.OrdinalIgnoreCase) && p.Length > 3)
                return p.Substring(3);

            return p;
        }

        private static bool ValidateRowsCollection(Type rootType)
        {
            if (rootType == typeof(ApplicationItem))
                return true;
            if (rootType == typeof(Application))
                return true; // runtime fills rows from ApplicationItems
            return false;
        }

        private Type GetRootType(UserReportBoType boType)
        {
            return boType switch
            {
                UserReportBoType.Application => typeof(Application),
                UserReportBoType.ApplicationItem => typeof(ApplicationItem),
                UserReportBoType.Person => typeof(Person),
                _ => typeof(Application)
            };
        }

        private bool PropertyExists(Type type, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return false;

            var parts = propertyPath.Split('.');
            var currentType = type;

            foreach (var part in parts)
            {
                // Get XAF type info
                var typeInfo = _typesInfo.FindTypeInfo(currentType);
                if (typeInfo == null)
                    return false;

                // Try to find the member
                var member = typeInfo.FindMember(part);
                if (member == null)
                {
                    // Also check for [NotMapped] properties using reflection
                    var prop = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop == null)
                        return false;

                    currentType = prop.PropertyType;
                }
                else
                {
                    // Handle collection types
                    if (member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(IList<>))
                    {
                        currentType = member.MemberType.GetGenericArguments()[0];
                    }
                    else
                    {
                        currentType = member.MemberType;
                    }
                }
            }

            return true;
        }

        private string GetExampleValue(Type rootType, string propertyPath)
        {
            // Return sample values for common properties
            var lowerPath = propertyPath.ToLowerInvariant();

            return lowerPath switch
            {
                var p when p.Contains("number") => "1234",
                var p when p.Contains("date") => "14.05.2026",
                var p when p.Contains("fullname") => "Gurbanguly Berdimuhamedow",
                var p when p.Contains("name") => "Direktor",
                var p when p.Contains("count") => "3",
                var p when p.Contains("address") => "Aşgabat şäheri",
                _ => $"[{propertyPath}]"
            };
        }
    }
}
