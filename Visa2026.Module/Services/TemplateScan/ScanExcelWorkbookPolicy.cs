using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;

#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Create-from-yellow-marks Excel workbooks may contain extra sheets; only the first worksheet
/// is scanned, mapped, previewed, and written. Other sheets stay untouched in the saved copy.
/// </summary>
internal static class ScanExcelWorkbookPolicy
{
    public static string? GetFirstWorksheetName(byte[] workbookBytes)
    {
        ArgumentNullException.ThrowIfNull(workbookBytes);
        if (workbookBytes.Length == 0)
            return null;

        using var stream = new MemoryStream(workbookBytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        return workbook.Worksheets.FirstOrDefault()?.Name;
    }

    public static bool IsOnFirstWorksheet(byte[] workbookBytes, string sheetName)
    {
        var first = GetFirstWorksheetName(workbookBytes);
        return first != null
            && string.Equals(first, sheetName, StringComparison.OrdinalIgnoreCase);
    }

    public static TemplateDocumentOutline LimitOutlineToFirstSheet(TemplateDocumentOutline outline)
    {
        ArgumentNullException.ThrowIfNull(outline);
        if (outline.Format != TemplateSourceFormat.Xlsx || outline.Sheets.Count <= 1)
            return outline;

        return outline with { Sheets = [outline.Sheets[0]] };
    }
}
