using System.Collections.Generic;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <summary>Validates extracted placeholders against BO properties.</summary>
    public interface IUserReportValidationService
    {
        /// <summary>Validate a list of placeholder keys against a Business Object type.</summary>
        /// <param name="placeholders">List of placeholder keys (e.g., "ApplicationNumber", "CompanyHead.FullName")</param>
        /// <param name="boType">The root Business Object type</param>
        /// <returns>List of validation results with status and resolved paths</returns>
        Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(IList<string> placeholders, UserReportBoType boType);
    }

    /// <summary>Result of validating a single placeholder.</summary>
    public class PlaceholderValidationResult
    {
        /// <summary>The original placeholder key from the template.</summary>
        public string PlaceholderKey { get; set; } = string.Empty;

        /// <summary>Whether this placeholder is valid on the selected BO.</summary>
        public bool IsValid { get; set; }

        /// <summary>The resolved property path (e.g., "ApplicationNumber" or "CompanyHead.FullName").</summary>
        public string ResolvedPath { get; set; } = string.Empty;

        /// <summary>Sample/example value for documentation.</summary>
        public string ExampleValue { get; set; } = string.Empty;

        /// <summary>Error message if validation failed.</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Whether this is a collection/loop placeholder (starts with #).</summary>
        public bool IsCollection { get; set; }

        /// <summary>Whether this is a row property (starts with .).</summary>
        public bool IsRowProperty { get; set; }
    }
}
