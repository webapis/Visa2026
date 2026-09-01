using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>A paragraph as the officer sees it, keyed by the same address the writer and diff gate use.</summary>
public sealed record TemplateOutlineParagraph(string Address, WordPart Part, string Text, bool IsInTable);

/// <summary>A populated cell, keyed by sheet name and A1 reference.</summary>
public sealed record TemplateOutlineCell(string SheetName, string CellReference, int RowNumber, int ColumnNumber, string Text);

public sealed record TemplateOutlineSheet(string Name, IReadOnlyList<TemplateOutlineCell> Cells);

public enum TemplatePageOrientation
{
    Portrait = 0,
    Landscape = 1,
}

/// <summary>
/// A read-only text projection of an uploaded document, addressed identically to
/// <see cref="DocumentRegion"/>. The convert UI needs it to draw the document with highlights on top;
/// nothing here participates in conversion.
/// </summary>
public sealed record TemplateDocumentOutline(
    TemplateSourceFormat Format,
    IReadOnlyList<TemplateOutlineParagraph> Paragraphs,
    IReadOnlyList<TemplateOutlineSheet> Sheets,
    bool IsReadable,
    TemplatePageOrientation PageOrientation = TemplatePageOrientation.Portrait)
{
    public static TemplateDocumentOutline Unreadable(TemplateSourceFormat format) =>
        new(format, Array.Empty<TemplateOutlineParagraph>(), Array.Empty<TemplateOutlineSheet>(), false);

    public bool IsLandscape => PageOrientation == TemplatePageOrientation.Landscape;
}

public interface ITemplateDocumentOutlineReader
{
    TemplateDocumentOutline Read(byte[] content, TemplateSourceFormat format);
}

public sealed class TemplateDocumentOutlineReader : ITemplateDocumentOutlineReader
{
    /// <summary>Beyond this a preview stops being reviewable and starts being a denial of service on the browser.</summary>
    private const int MaxCellsPerSheet = 4000;

    public TemplateDocumentOutline Read(byte[] content, TemplateSourceFormat format)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            return format == TemplateSourceFormat.Docx ? ReadWord(content) : ReadExcel(content);
        }
        catch (Exception)
        {
            // A corrupt upload is an officer-facing outcome (candidate Fail), not an exception to surface.
            return TemplateDocumentOutline.Unreadable(format);
        }
    }

    private static TemplateDocumentOutline ReadWord(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, isEditable: false);

        var paragraphs = WordTemplateAddressing.EnumerateParagraphs(document)
            .Select(p => new TemplateOutlineParagraph(
                p.Address,
                p.Part,
                WordTemplateAddressing.GetParagraphText(p.Paragraph),
                p.IsInTable))
            .ToList();

        return new TemplateDocumentOutline(
            TemplateSourceFormat.Docx,
            paragraphs,
            Array.Empty<TemplateOutlineSheet>(),
            true,
            ReadWordOrientation(document));
    }

    private static TemplateDocumentOutline ReadExcel(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);

        var sheets = new List<TemplateOutlineSheet>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var cells = new List<TemplateOutlineCell>();
            var used = worksheet.RangeUsed();
            if (used != null)
            {
                foreach (var cell in used.CellsUsed())
                {
                    if (cells.Count >= MaxCellsPerSheet)
                        break;

                    cells.Add(new TemplateOutlineCell(
                        worksheet.Name,
                        cell.Address.ToStringRelative(),
                        cell.Address.RowNumber,
                        cell.Address.ColumnNumber,
                        cell.GetFormattedString()));
                }
            }

            sheets.Add(new TemplateOutlineSheet(worksheet.Name, cells));
        }

        return new TemplateDocumentOutline(
            TemplateSourceFormat.Xlsx,
            Array.Empty<TemplateOutlineParagraph>(),
            sheets,
            true,
            ReadExcelOrientation(workbook));
    }

    internal static TemplatePageOrientation ReadWordOrientation(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        var size = body?.Elements<SectionProperties>().LastOrDefault()?.GetFirstChild<PageSize>()
            ?? body?.Descendants<SectionProperties>().LastOrDefault()?.GetFirstChild<PageSize>();
        if (size == null)
            return TemplatePageOrientation.Portrait;

        if (size.Orient?.Value == PageOrientationValues.Landscape)
            return TemplatePageOrientation.Landscape;

        var width = size.Width?.Value ?? 0;
        var height = size.Height?.Value ?? 0;
        return width > height ? TemplatePageOrientation.Landscape : TemplatePageOrientation.Portrait;
    }

    internal static TemplatePageOrientation ReadExcelOrientation(XLWorkbook workbook)
    {
        foreach (var worksheet in workbook.Worksheets)
        {
            if (worksheet.PageSetup.PageOrientation == XLPageOrientation.Landscape)
                return TemplatePageOrientation.Landscape;
        }

        return TemplatePageOrientation.Portrait;
    }
}
