#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Create-from-yellow-marks Excel: only the first worksheet is scanned/mapped;
/// extra sheets must stay untouched in the outline and name checks.
/// </summary>
public class ScanExcelWorkbookPolicyTests
{
    [Fact]
    public void GetFirstWorksheetName_EmptyBytes_ReturnsNull()
    {
        Assert.Null(ScanExcelWorkbookPolicy.GetFirstWorksheetName([]));
    }

    [Fact]
    public void GetFirstWorksheetName_ReturnsFirstSheet_IgnoringLaterSheets()
    {
        var bytes = BuildWorkbook("First", "Second");

        Assert.Equal("First", ScanExcelWorkbookPolicy.GetFirstWorksheetName(bytes));
    }

    [Fact]
    public void IsOnFirstWorksheet_MatchesCaseInsensitive()
    {
        var bytes = BuildWorkbook("People", "Extra");

        Assert.True(ScanExcelWorkbookPolicy.IsOnFirstWorksheet(bytes, "people"));
        Assert.False(ScanExcelWorkbookPolicy.IsOnFirstWorksheet(bytes, "Extra"));
        Assert.False(ScanExcelWorkbookPolicy.IsOnFirstWorksheet(bytes, "Missing"));
    }

    [Fact]
    public void LimitOutlineToFirstSheet_KeepsSingleSheetAndNonXlsx()
    {
        var one = new TemplateDocumentOutline(
            TemplateSourceFormat.Xlsx,
            [],
            [new TemplateOutlineSheet("Only", [])],
            IsReadable: true);
        Assert.Same(one, ScanExcelWorkbookPolicy.LimitOutlineToFirstSheet(one));

        var docx = new TemplateDocumentOutline(
            TemplateSourceFormat.Docx,
            [],
            [
                new TemplateOutlineSheet("A", []),
                new TemplateOutlineSheet("B", []),
            ],
            IsReadable: true);
        Assert.Same(docx, ScanExcelWorkbookPolicy.LimitOutlineToFirstSheet(docx));
    }

    [Fact]
    public void LimitOutlineToFirstSheet_DropsExtraXlsxSheets()
    {
        var outline = new TemplateDocumentOutline(
            TemplateSourceFormat.Xlsx,
            [],
            [
                new TemplateOutlineSheet("Keep", []),
                new TemplateOutlineSheet("Drop", []),
                new TemplateOutlineSheet("AlsoDrop", []),
            ],
            IsReadable: true);

        var limited = ScanExcelWorkbookPolicy.LimitOutlineToFirstSheet(outline);

        Assert.NotSame(outline, limited);
        Assert.Single(limited.Sheets);
        Assert.Equal("Keep", limited.Sheets[0].Name);
        Assert.Equal(3, outline.Sheets.Count);
    }

    static byte[] BuildWorkbook(params string[] sheetNames)
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            foreach (var name in sheetNames)
                wb.AddWorksheet(name);
            wb.SaveAs(ms);
        }

        return ms.ToArray();
    }
}
