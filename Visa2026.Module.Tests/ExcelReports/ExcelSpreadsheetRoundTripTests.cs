using DevExpress.Spreadsheet;
using Xunit;
using Visa2026.Module.Services.ExcelReports;

namespace Visa2026.Module.Tests.ExcelReports;

/// <summary>
/// Verifies DevExpress Spreadsheet Document API round-trip keeps placeholders readable by ClosedXML.
/// </summary>
public sealed class ExcelSpreadsheetRoundTripTests
{
    private const string SeedResourceName =
        "Visa2026.Module.Resources.Templates.Excel.433_gurlusyk_uzt.xlsx";

    [Fact]
    public async Task DevExpress_save_copy_preserves_closedxml_placeholders()
    {
        byte[] original = LoadEmbeddedSeed();
        byte[] roundTripped = RoundTripWithDevExpressSpreadsheet(original);

        var extractor = new ExcelTemplatePlaceholderExtractor();
        using var originalStream = new MemoryStream(original, writable: false);
        using var roundTripStream = new MemoryStream(roundTripped, writable: false);

        var originalPlaceholders = await extractor.ExtractPlaceholdersAsync(originalStream);
        var roundTripPlaceholders = await extractor.ExtractPlaceholdersAsync(roundTripStream);

        Assert.Equal(
            originalPlaceholders.OrderBy(static p => p, StringComparer.Ordinal),
            roundTripPlaceholders.OrderBy(static p => p, StringComparer.Ordinal));
    }

    private static byte[] LoadEmbeddedSeed()
    {
        var assembly = typeof(ExcelTemplatePlaceholderExtractor).Assembly;
        using var stream = assembly.GetManifestResourceStream(SeedResourceName);
        Assert.NotNull(stream);

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] RoundTripWithDevExpressSpreadsheet(byte[] content)
    {
        using var input = new MemoryStream(content, writable: false);
        using var workbook = new Workbook();
        workbook.LoadDocument(input);

        using var output = new MemoryStream();
        workbook.SaveDocument(output, DevExpress.Spreadsheet.DocumentFormat.OpenXml);
        return output.ToArray();
    }
}
