using System;
using System.IO;
using DevExpress.Spreadsheet;
using DevExpress.XtraRichEdit;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Converts generated Resminamalar Word/Excel bytes to PDF via DevExpress Office File API for in-app preview.</summary>
public sealed class ApplicationWordReportOfficePreviewPdfConverter
{
    public byte[]? TryConvertToPdf(byte[] officeContent, string fileName)
    {
        if (officeContent == null || officeContent.Length == 0 || string.IsNullOrWhiteSpace(fileName))
            return null;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".docx" => ConvertWordToPdf(officeContent),
            ".xlsx" or ".xlsm" => ConvertExcelToPdf(officeContent),
            _ => null
        };
    }

    private static byte[]? ConvertWordToPdf(byte[] content)
    {
        using var input = new MemoryStream(content, writable: false);
        using var server = new RichEditDocumentServer();
        server.LoadDocument(input, DevExpress.XtraRichEdit.DocumentFormat.OpenXml);
        server.Options.Printing.EnablePageBackgroundOnPrint = true;

        using var output = new MemoryStream();
        server.ExportToPdf(output);
        return ToByteArray(output);
    }

    private static byte[]? ConvertExcelToPdf(byte[] content)
    {
        using var input = new MemoryStream(content, writable: false);
        using var workbook = new Workbook();
        workbook.LoadDocument(input);
        workbook.CalculateFull();

        using var output = new MemoryStream();
        workbook.ExportToPdf(output);
        return ToByteArray(output);
    }

    private static byte[]? ToByteArray(MemoryStream stream)
    {
        if (stream.Length == 0)
            return null;

        return stream.ToArray();
    }
}

public sealed class ApplicationWordReportPackagePreviewBundle
{
    public required ApplicationWordReportGeneratedFile Original { get; init; }

    public byte[]? PdfContent { get; init; }

    public string PdfFileName =>
        string.IsNullOrWhiteSpace(Original.FileName)
            ? "report-preview.pdf"
            : Path.ChangeExtension(Original.FileName, ".pdf");
}
