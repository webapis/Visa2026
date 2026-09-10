using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DevExpress.Drawing.Printing;
using DevExpress.Spreadsheet;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Custom Resminamalar / preview-slot Excel→PDF page size. No officer control:
/// use the workbook PageSetup when set, otherwise the used range width.
/// </summary>
public static class ExcelPreviewPageLayout
{
    /// <summary>A4 portrait printable width in Excel default character units.</summary>
    public const double PortraitMaxCharacterWidth = 80;

    public const int WideColumnCount = 8;

    public static bool ShouldUseLandscape(
        bool pageSetupIsLandscape,
        int usedColumnCount,
        double usedCharacterWidth)
    {
        if (pageSetupIsLandscape)
            return true;

        if (usedCharacterWidth > PortraitMaxCharacterWidth)
            return true;

        return usedColumnCount >= WideColumnCount && usedCharacterWidth >= 55;
    }

    public static void ApplyToWorksheet(Worksheet worksheet)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        var used = worksheet.GetUsedRange();
        var columns = 0;
        var width = 0d;
        if (used != null)
        {
            columns = used.RightColumnIndex - used.LeftColumnIndex + 1;
            for (var column = used.LeftColumnIndex; column <= used.RightColumnIndex; column++)
                width += worksheet.Columns[column].Width;
        }

        var landscape = ShouldUseLandscape(
            worksheet.ActiveView.Orientation == PageOrientation.Landscape,
            columns,
            width);

        worksheet.ActiveView.Orientation = landscape
            ? PageOrientation.Landscape
            : PageOrientation.Portrait;
        worksheet.ActiveView.PaperKind = DXPaperKind.A4;

        var printOptions = worksheet.PrintOptions;
        printOptions.FitToPage = true;
        printOptions.FitToWidth = 1;
        printOptions.FitToHeight = 0;
    }

    public static byte[] StampFromContent(byte[] content)
    {
        if (content == null || content.Length == 0)
            return content ?? Array.Empty<byte>();

        try
        {
            using var input = new MemoryStream(content, writable: false);
            using var workbook = new XLWorkbook(input);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
                return content;

            var used = sheet.RangeUsed();
            var columns = used?.ColumnCount() ?? 0;
            var width = 0d;
            if (used != null)
            {
                for (var column = used.FirstColumn().ColumnNumber(); column <= used.LastColumn().ColumnNumber(); column++)
                    width += sheet.Column(column).Width;
            }

            var landscape = ShouldUseLandscape(
                sheet.PageSetup.PageOrientation == XLPageOrientation.Landscape,
                columns,
                width);
            if (!landscape || sheet.PageSetup.PageOrientation == XLPageOrientation.Landscape)
                return content;

            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }
        catch (Exception)
        {
            return content;
        }
    }
}