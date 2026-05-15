using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Visa2026.Module.Services.ExcelReports;

/// <summary>Scans an <c>.xlsx</c> template for <c>{{…}}</c> placeholders.</summary>
public interface IExcelTemplatePlaceholderExtractor
{
    Task<IList<string>> ExtractPlaceholdersAsync(Stream xlsxStream);
}
