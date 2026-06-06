using System;
using System.Collections.Generic;
using System.IO;
using DevExpress.Spreadsheet;
using DevExpress.XtraRichEdit;
using Visa2026.Module.Services;

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

    /// <summary>Converts each office file to PDF and merges pages in order (multi-item per-person preview).</summary>
    public byte[]? TryConvertManyToMergedPdf(IReadOnlyList<(byte[] Content, string FileName)> officeFiles)
    {
        if (officeFiles == null || officeFiles.Count == 0)
            return null;

        if (officeFiles.Count == 1)
            return TryConvertToPdf(officeFiles[0].Content, officeFiles[0].FileName);

        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var (content, fileName) in officeFiles)
            {
                var pdf = TryConvertToPdf(content, fileName);
                if (pdf == null || pdf.Length == 0)
                    return null;

                pdfStreams.Add(new MemoryStream(pdf, writable: false));
            }

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            return ToByteArray(merged);
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }
}

public sealed class ApplicationWordReportPackagePreviewBundle
{
    public required IReadOnlyList<ApplicationWordReportGeneratedFile> Originals { get; init; }

    public ApplicationWordReportGeneratedFile Original => Originals[0];

    public byte[]? PdfContent { get; init; }

    public string PdfFileName
    {
        get
        {
            if (Originals.Count > 1)
                return "report-preview.pdf";

            return string.IsNullOrWhiteSpace(Original.FileName)
                ? "report-preview.pdf"
                : Path.ChangeExtension(Original.FileName, ".pdf");
        }
    }
}
