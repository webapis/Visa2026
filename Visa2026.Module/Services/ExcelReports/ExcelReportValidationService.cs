using System.Collections.Generic;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.ExcelReports;

/// <inheritdoc cref="IExcelReportValidationService"/>
public class ExcelReportValidationService : IExcelReportValidationService
{
    private readonly IUserReportValidationService _wordValidator;

    public ExcelReportValidationService(IUserReportValidationService wordValidator)
    {
        _wordValidator = wordValidator;
    }

    public async Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
        IList<string> placeholders,
        UserReportBoType boType,
        ExcelMergeMode mergeMode)
    {
        var results = new List<PlaceholderValidationResult>();

        foreach (var placeholder in placeholders)
        {
            if (placeholder.StartsWith("/", StringComparison.Ordinal))
            {
                results.Add(new PlaceholderValidationResult
                {
                    PlaceholderKey = placeholder,
                    IsValid = true,
                    ResolvedPath = placeholder.TrimStart('/'),
                });
                continue;
            }

            if (mergeMode == ExcelMergeMode.ItemList)
            {
                if (placeholder.StartsWith(".", StringComparison.Ordinal))
                {
                    var itemResults = await _wordValidator
                        .ValidatePlaceholdersAsync(new List<string> { placeholder }, UserReportBoType.ApplicationItem)
                        .ConfigureAwait(false);
                    results.Add(itemResults[0]);
                    continue;
                }

                var clean = UserReportMergeDataHelper.StripDocxModelPrefix(placeholder.TrimStart('#', '/'));
                if (string.Equals(clean, "rows", StringComparison.OrdinalIgnoreCase)
                    || clean.StartsWith("rows.", StringComparison.OrdinalIgnoreCase))
                {
                    var itemResults = await _wordValidator
                        .ValidatePlaceholdersAsync(new List<string> { placeholder }, UserReportBoType.ApplicationItem)
                        .ConfigureAwait(false);
                    results.Add(itemResults[0]);
                    continue;
                }

                var appResults = await _wordValidator
                    .ValidatePlaceholdersAsync(new List<string> { $"ds.{clean}" }, UserReportBoType.Application)
                    .ConfigureAwait(false);
                results.Add(appResults[0]);
                continue;
            }

            var single = await _wordValidator
                .ValidatePlaceholdersAsync(new List<string> { placeholder }, boType)
                .ConfigureAwait(false);
            results.Add(single[0]);
        }

        return results;
    }
}
