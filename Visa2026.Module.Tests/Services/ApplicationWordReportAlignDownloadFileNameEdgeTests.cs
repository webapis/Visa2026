using System.IO.Compression;
using System.Text;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Catalog labels can say .docx while bytes are .xlsx (and the reverse). Download + LibreOffice
/// must follow the package, not the label.
/// </summary>
public sealed class ApplicationWordReportAlignDownloadFileNameEdgeTests
{
    [Fact]
    public void AlignDownloadFileName_blank_or_empty_content_returns_input()
    {
        Assert.Equal(string.Empty, ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName(null!, Array.Empty<byte>()));
        Assert.Equal("report.docx", ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName("report.docx", Array.Empty<byte>()));
        Assert.Equal("report.docx", ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName("report.docx", null!));
        Assert.Equal("  ", ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName("  ", MinimalXlsx()));
    }

    [Fact]
    public void AlignDownloadFileName_renames_xlsx_label_when_bytes_are_docx()
    {
        var docx = MinimalDocx();
        var name = ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName("roster.xlsx", docx);
        Assert.Equal("roster.docx", name);
    }

    [Fact]
    public void AlignDownloadFileName_uses_report_stem_when_extension_only()
    {
        var xlsx = MinimalXlsx();
        Assert.Equal("report.xlsx", ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName(".docx", xlsx));
    }

    [Fact]
    public void AlignDownloadFileName_keeps_matching_extension()
    {
        var xlsx = MinimalXlsx();
        Assert.Equal("Sanaw.XLSX", ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName("Sanaw.XLSX", xlsx));
    }

    private static byte[] MinimalXlsx()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("xl/workbook.xml");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write("<workbook/>");
        }

        return stream.ToArray();
    }

    private static byte[] MinimalDocx()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write("<w:document/>");
        }

        return stream.ToArray();
    }
}
