#nullable enable

using System.Globalization;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>One Review mark: document order (top→bottom) matching the Detected fields row number.</summary>
public sealed record ScanReviewOrderedField(
    int Order,
    string FieldId,
    string LabelText,
    string? ProposedToken,
    DocumentRegion? SourceRegion,
    int PageIndex,
    bool IsGap,
    string OrderLabel = "",
    string OverlayId = "",
    int PartIndex = 0,
    DocumentRegion? OverlayRegion = null,
    IReadOnlyList<int>? HiddenPartIndexes = null)
{
    public string DisplayOrder =>
        string.IsNullOrWhiteSpace(OrderLabel) ? Order.ToString(CultureInfo.InvariantCulture) : OrderLabel;

    public string DisplayId => string.IsNullOrWhiteSpace(OverlayId) ? FieldId : OverlayId;

    public DocumentRegion? DisplayRegion => OverlayRegion ?? SourceRegion;
}

/// <summary>
/// Numbers yellow-mapped fields from the top of the Word/Excel file downward
/// so the left preview squares and the Detected fields rows stay aligned.
/// </summary>
public static class ScanReviewFieldOrder
{
    public static IReadOnlyList<ScanReviewOrderedField> Order(
        IReadOnlyList<ScanDetectedField> fields,
        IReadOnlyList<ScanGap>? gaps = null)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var items = new List<ScanReviewOrderedField>(fields.Count + (gaps?.Count ?? 0));
        foreach (var field in fields)
        {
            items.Add(new ScanReviewOrderedField(
                0,
                field.FieldId,
                field.LabelText,
                field.ProposedToken,
                field.SourceRegion,
                field.PageIndex,
                IsGap: false,
                HiddenPartIndexes: field.HiddenPartIndexes));
        }

        if (gaps != null)
        {
            foreach (var gap in gaps)
            {
                items.Add(new ScanReviewOrderedField(
                    0,
                    gap.FieldId,
                    gap.LabelText,
                    gap.SuggestedPropertyName,
                    null,
                    0,
                    IsGap: true));
            }
        }

        items.Sort(Compare);
        for (var i = 0; i < items.Count; i++)
            items[i] = items[i] with { Order = i + 1 };

        return ExpandCompounds(items);
    }

    public static string ParentFieldId(string? rowKey)
    {
        var key = (rowKey ?? string.Empty).Trim();
        if (key.Length == 0)
            return key;

        var colon = key.LastIndexOf(':');
        if (colon <= 0 || colon + 1 >= key.Length)
            return key;

        return int.TryParse(key[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            ? key[..colon]
            : key;
    }

    internal static IReadOnlyList<ScanReviewOrderedField> ExpandCompounds(
        IReadOnlyList<ScanReviewOrderedField> ordered)
    {
        var expanded = new List<ScanReviewOrderedField>();
        foreach (var mark in ordered)
        {
            var parts = ScanCompoundYellowParts.Split(mark.LabelText, mark.ProposedToken);
            var hidden = mark.HiddenPartIndexes ?? Array.Empty<int>();
            if (parts.Count <= 1)
            {
                expanded.Add(mark with
                {
                    OrderLabel = mark.Order.ToString(CultureInfo.InvariantCulture),
                    OverlayId = mark.FieldId,
                    PartIndex = 0,
                });
                continue;
            }

            foreach (var part in parts)
            {
                if (hidden.Contains(part.Index))
                    continue;

                expanded.Add(mark with
                {
                    OrderLabel = mark.Order.ToString(CultureInfo.InvariantCulture) + "." + part.Index,
                    OverlayId = mark.FieldId + ":" + part.Index,
                    PartIndex = part.Index,
                    LabelText = part.SegmentText,
                    ProposedToken = part.Token,
                    OverlayRegion = ScanCompoundYellowParts.SliceRegion(mark.SourceRegion, mark.LabelText, part),
                });
            }
        }

        return expanded;
    }

    private static int Compare(ScanReviewOrderedField left, ScanReviewOrderedField right)
    {
        var page = left.PageIndex.CompareTo(right.PageIndex);
        if (page != 0)
            return page;

        var pos = CompareRegion(left.SourceRegion, right.SourceRegion);
        if (pos != 0)
            return pos;

        var gap = left.IsGap.CompareTo(right.IsGap);
        if (gap != 0)
            return gap;

        return string.Compare(left.LabelText, right.LabelText, StringComparison.OrdinalIgnoreCase);
    }

    internal static int CompareRegion(DocumentRegion? left, DocumentRegion? right)
    {
        if (left is null && right is null)
            return 0;
        if (left is null)
            return 1;
        if (right is null)
            return -1;

        if (left is DocumentRegion.WordSpan leftWord && right is DocumentRegion.WordSpan rightWord)
        {
            var addr = CompareAddress(leftWord.ParagraphAddress, rightWord.ParagraphAddress);
            return addr != 0 ? addr : leftWord.Start.CompareTo(rightWord.Start);
        }

        if (left is DocumentRegion.WordDrawing leftDrawing && right is DocumentRegion.WordDrawing rightDrawing)
        {
            var addr = CompareAddress(leftDrawing.ParagraphAddress, rightDrawing.ParagraphAddress);
            if (addr != 0)
                return addr;
            var offset = leftDrawing.TextInsertOffset.CompareTo(rightDrawing.TextInsertOffset);
            return offset != 0 ? offset : leftDrawing.DrawingIndex.CompareTo(rightDrawing.DrawingIndex);
        }

        if (left is DocumentRegion.WordDrawing leftPic && right is DocumentRegion.WordSpan rightSpan)
        {
            var addr = CompareAddress(leftPic.ParagraphAddress, rightSpan.ParagraphAddress);
            return addr != 0 ? addr : leftPic.TextInsertOffset.CompareTo(rightSpan.Start);
        }

        if (left is DocumentRegion.WordSpan leftSpan && right is DocumentRegion.WordDrawing rightPic)
        {
            var addr = CompareAddress(leftSpan.ParagraphAddress, rightPic.ParagraphAddress);
            return addr != 0 ? addr : leftSpan.Start.CompareTo(rightPic.TextInsertOffset);
        }

        if (left is DocumentRegion.ExcelCell leftCell && right is DocumentRegion.ExcelCell rightCell)
        {
            var sheet = string.Compare(leftCell.SheetName, rightCell.SheetName, StringComparison.OrdinalIgnoreCase);
            if (sheet != 0)
                return sheet;

            ParseA1(leftCell.CellReference, out var leftRow, out var leftCol);
            ParseA1(rightCell.CellReference, out var rightRow, out var rightCol);
            var row = leftRow.CompareTo(rightRow);
            return row != 0 ? row : leftCol.CompareTo(rightCol);
        }

        return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
    }

    internal static int CompareAddress(string left, string right)
    {
        ParseParagraphAddress(left, out var leftPart, out var leftIndex);
        ParseParagraphAddress(right, out var rightPart, out var rightIndex);
        var part = leftPart.CompareTo(rightPart);
        return part != 0 ? part : leftIndex.CompareTo(rightIndex);
    }

    private static void ParseParagraphAddress(string address, out int partRank, out int index)
    {
        partRank = 1;
        index = 0;
        if (string.IsNullOrWhiteSpace(address))
            return;

        if (address.StartsWith("body/", StringComparison.OrdinalIgnoreCase))
            partRank = 0;
        else if (address.StartsWith("header", StringComparison.OrdinalIgnoreCase))
            partRank = 2;
        else if (address.StartsWith("footer", StringComparison.OrdinalIgnoreCase))
            partRank = 3;

        var slash = address.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < address.Length)
            _ = int.TryParse(address[(slash + 1)..], out index);
    }

    private static void ParseA1(string? reference, out int row, out int column)
    {
        row = 0;
        column = 0;
        if (string.IsNullOrWhiteSpace(reference))
            return;

        var letters = 0;
        foreach (var ch in reference)
        {
            if (ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                letters = letters * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            else if (char.IsDigit(ch))
                row = row * 10 + (ch - '0');
        }

        column = letters;
    }
}
