#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Stable yellow-span identity across Analyze / Remap unmarked (FieldId is a new Guid each pass).</summary>
public static class ScanDocumentRegionKey
{
    public static string? ForRegion(DocumentRegion? region) => region switch
    {
        DocumentRegion.WordSpan word =>
            "w|" + word.ParagraphAddress + "|" + word.Start.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|" + word.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DocumentRegion.WordDrawing drawing =>
            "d|" + drawing.ParagraphAddress + "|" + drawing.DrawingIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DocumentRegion.ExcelCell cell =>
            "e|" + cell.SheetName + "|" + cell.CellReference,
        _ => null,
    };

    public static string ForField(ScanDetectedField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var key = ForRegion(field.SourceRegion);
        if (!string.IsNullOrEmpty(key))
            return key;

        return "t|" + field.PageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|" + TemplateTextNormalizer.NormalizeIdentifier(field.LabelText ?? string.Empty);
    }

    /// <summary>Paragraph / drawing / cell group used to align Review rows to live yellows.</summary>
    public static string AlignGroupKey(DocumentRegion? region) =>
        region switch
        {
            DocumentRegion.WordSpan word => "p|" + word.ParagraphAddress,
            DocumentRegion.WordDrawing drawing =>
                "d|" + drawing.ParagraphAddress + "|" + drawing.DrawingIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DocumentRegion.ExcelCell cell => "e|" + cell.SheetName + "|" + cell.CellReference,
            _ => "?",
        };

    public static int OrderInGroup(DocumentRegion? region) =>
        region switch
        {
            DocumentRegion.WordSpan word => word.Start,
            DocumentRegion.WordDrawing drawing => drawing.TextInsertOffset,
            _ => 0,
        };
}