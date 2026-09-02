using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Spreadsheet;
using DevExpress.XtraPrinting;
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

        // Nested catalog filename bugs used to label Excel bytes as .docx. ZIP parts win over
        // the extension so Preview still uses Spreadsheet ExportToPdf.
        if (LooksLikeOpenXmlExcel(officeContent))
            return ConvertExcelToPdf(officeContent);
        if (LooksLikeOpenXmlWord(officeContent))
            return ConvertWordToPdf(officeContent);

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".docx" => ConvertWordToPdf(officeContent),
            ".xlsx" or ".xlsm" => ConvertExcelToPdf(officeContent),
            _ => null
        };
    }

    internal static bool LooksLikeOpenXmlExcel(byte[] content) =>
        OpenXmlPackageHasEntryPrefix(content, "xl/");

    internal static bool LooksLikeOpenXmlWord(byte[] content) =>
        OpenXmlPackageHasEntryPrefix(content, "word/");

    private static bool OpenXmlPackageHasEntryPrefix(byte[] content, string prefix)
    {
        if (content == null || content.Length < 4)
            return false;

        try
        {
            using var input = new MemoryStream(content, writable: false);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
            return zip.Entries.Any(entry =>
                entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
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

        if (workbook.Worksheets.Count == 0)
            return null;

        // Match merge + yellow-mark scan: preview only the first worksheet. Drop extras so
        // DevExpress does not emit blank leading pages from unused sheets.
        while (workbook.Worksheets.Count > 1)
            workbook.Worksheets.RemoveAt(workbook.Worksheets.Count - 1);

        var worksheet = workbook.Worksheets[0];
        workbook.Worksheets.ActiveWorksheet = worksheet;

        // Officer sanaw files often keep a stale Print_Area (empty col A after insert, or a
        // blank extra sheet). Each print area becomes its own PDF page — first page looks empty.
        worksheet.ClearPrintRange();
        var usedRange = worksheet.GetUsedRange();
        if (usedRange != null)
            worksheet.SetPrintRange(usedRange);

        var printOptions = worksheet.PrintOptions;
        printOptions.FitToPage = true;
        printOptions.FitToWidth = 1;

        using var output = new MemoryStream();
        workbook.ExportToPdf(output, new PdfExportOptions(), worksheet.Name);
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

        var pdfs = new byte[officeFiles.Count][];
        Parallel.For(0, officeFiles.Count, index =>
        {
            var (content, fileName) = officeFiles[index];
            pdfs[index] = TryConvertToPdf(content, fileName) ?? Array.Empty<byte>();
        });

        var pdfStreams = new List<MemoryStream>(officeFiles.Count);
        try
        {
            foreach (var pdf in pdfs)
            {
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
