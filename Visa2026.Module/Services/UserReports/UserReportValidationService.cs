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
                var propertyPath = cleanPlaceholder;
                var isValid = PropertyExists(rootType, propertyPath);

                result.IsValid = isValid;
                result.ResolvedPath = propertyPath;

                if (isValid)
                {
                    result.ExampleValue = GetExampleValue(rootType, propertyPath);
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

        private Type GetRootType(UserReportBoType boType)
        {
            return boType switch
            {
                UserReportBoType.Application => typeof(Application),
                UserReportBoType.ApplicationItem => typeof(ApplicationItem),
                UserReportBoType.Registration => typeof(Registration),
                UserReportBoType.BusinessTrip => typeof(BusinessTrip),
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
