using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DevExpress.Spreadsheet;
using DevExpress.XtraSpreadsheet.Utils;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// The red "For evaluation purposes only" line on Excel preview is painted by Office File API
/// when <see cref="Workbook"/> is not covered by the process license. It is not in the .xlsx.
/// </summary>
internal static class ExcelPreviewEvaluationNotice
{
    internal const string SheetName = "Evaluation Warning";

    internal static bool WouldStampExcelPdf() =>
        InformationCreator.CanAddInformation(typeof(Workbook));

    /// <summary>
    /// Office File API inserts an "Evaluation Warning" sheet while saving an unlicensed workbook.
    /// Drop that sheet so LibreOffice does not print it. Officer sheets stay.
    /// </summary>
    internal static byte[] RemoveInjectedSheet(byte[] xlsx)
    {
        if (xlsx == null || xlsx.Length == 0)
            return xlsx ?? Array.Empty<byte>();

        try
        {
            using var input = new MemoryStream(xlsx, writable: false);
            using var workbook = new XLWorkbook(input);
            var injected = workbook.Worksheets
                .Where(sheet => IsInjectedSheet(sheet.Name))
                .ToList();
            if (injected.Count == 0 || injected.Count >= workbook.Worksheets.Count)
                return xlsx;

            foreach (var sheet in injected)
                sheet.Delete();

            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }
        catch (Exception)
        {
            return xlsx;
        }
    }

    private static bool IsInjectedSheet(string name) =>
        name.Equals(SheetName, StringComparison.OrdinalIgnoreCase)
        || name.StartsWith(SheetName + " (", StringComparison.OrdinalIgnoreCase);
}