using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.ExcelReports;

/// <inheritdoc cref="IExcelTemplatePlaceholderExtractor"/>
public class ExcelTemplatePlaceholderExtractor : IExcelTemplatePlaceholderExtractor
{
    public Task<IList<string>> ExtractPlaceholdersAsync(Stream xlsxStream)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);

        if (xlsxStream.CanSeek)
            xlsxStream.Position = 0;

        using var workbook = new XLWorkbook(xlsxStream);
        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var cell in worksheet.CellsUsed())
            {
                var text = cell.GetFormattedString();
                if (string.IsNullOrEmpty(text))
                    continue;

                foreach (System.Text.RegularExpressions.Match match in UserReportPlaceholderPatterns.PlaceholderRegex.Matches(text))
                {
                    if (match.Groups.Count > 1)
                        placeholders.Add(match.Groups[1].Value);
                }
            }
        }

        return Task.FromResult<IList<string>>(new List<string>(placeholders));
    }
}
