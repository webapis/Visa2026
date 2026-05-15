using System.Collections.Generic;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.ExcelReports;

/// <summary>Validates Excel template placeholders (header vs row split for list mode).</summary>
public interface IExcelReportValidationService
{
    Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
        IList<string> placeholders,
        UserReportBoType boType,
        ExcelMergeMode mergeMode);
}
