using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Replaces image placeholder text in a merged <c>.docx</c> with inline pictures (post–DocxTemplater).
/// Supports <c>{{IMAGE:Person_Photo}}</c>, legacy <c>{{…Person_Photo:img(…)…}}</c>, and leftover <c>System.Byte[]</c> text.
/// </summary>
public static class WordUserReportImageInjector
{
    /// <summary>Matches explicit image markers and legacy DocxTemplater <c>:img()</c> tokens.
    /// Whitespace, soft hyphens, and line wraps inside the token are allowed so a photo-cell
    /// round-trip through Word still injects.</summary>
    public static readonly Regex PlaceholderRegex = new(
        @"\{\{\s*IMAGE\s*:\s*(?<key>[^}]+?)\s*\}\}|\{\{(?:[^}]*\.)?(?<key2>[\w]+):img(?:\([^)]*\))?\}\}|System\.Byte\[\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private const long EmuPerMm = 36000L;
    private const long DefaultWidthEmu = 35L * EmuPerMm;
    private const long DefaultHeightEmu = 45L * EmuPerMm;

    public static void Inject(Stream mergedDocx, Stream output, IReadOnlyDictionary<string, IReadOnlyList<byte[]>> photosByKey)
    {
        ArgumentNullException.ThrowIfNull(mergedDocx);
        ArgumentNullException.ThrowIfNull(output);
        photosByKey ??= new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase);

        using (var document = WordprocessingDocument.Open(mergedDocx, true))
        {
            var mainPart = document.MainDocumentPart
                ?? throw new InvalidOperationException("Word document has no main document part.");

            var cursors = photosByKey.Keys.ToDictionary(static k => k, _ => 0, StringComparer.OrdinalIgnoreCase);
            var nextDrawingId = GetMaxDrawingPropertyId(mainPart) + 1U;

            foreach (var root in EnumerateOpenXmlTextRoots(mainPart))
            {
                foreach (var cell in root.Descendants<TableCell>().ToList())
                {
                    var owned = cell.Descendants<Text>()
                        .Where(t => t.Ancestors<TableCell>().FirstOrDefault() == cell)
                        .ToList();
                    nextDrawingId = ProcessTextScope(mainPart, owned, photosByKey, cursors, nextDrawingId);
                }

                foreach (var paragraph in root.Descendants<Paragraph>().ToList())
                {
                    if (paragraph.Ancestors<TableCell>().Any())
                        continue;
                    nextDrawingId = ProcessTextScope(
                        mainPart,
                        paragraph.Descendants<Text>().ToList(),
                        photosByKey,
                        cursors,
                        nextDrawingId);
                }
            }

            mainPart.Document.Save();
            document.Save();
        }

        mergedDocx.Position = 0;
        mergedDocx.CopyTo(output);
    }

    private static IEnumerable<OpenXmlElement> EnumerateOpenXmlTextRoots(MainDocumentPart mainPart)
    {
        if (mainPart.Document.Body != null)
            yield return mainPart.Document.Body;
        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header != null)
                yield return headerPart.Header;
        }

        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer != null)
                yield return footerPart.Footer;
        }
    }

    /// <summary>
    /// Word often splits <c>{{IMAGE:Person_Photo}}</c> across runs, a <c>w:br</c>, or two
    /// paragraphs in a narrow table cell (photo box). Match on concatenated <c>w:t</c> text.
    /// </summary>
    private static uint ProcessTextScope(
        MainDocumentPart mainPart,
        List<Text> textNodes,
        IReadOnlyDictionary<string, IReadOnlyList<byte[]>> photosByKey,
        Dictionary<string, int> cursors,
        uint nextDrawingId)
    {
        if (textNodes.Count == 0)
            return nextDrawingId;

        var combined = string.Concat(textNodes.Select(static t => t.Text ?? string.Empty));
        if (string.IsNullOrEmpty(combined))
            return nextDrawingId;

        var matches = PlaceholderRegex.Matches(combined).Cast<Match>().OrderByDescending(static m => m.Index).ToList();
        foreach (var match in matches)
        {
            var photoKey = NormalizeImageKey(
                match.Groups["key"].Success
                    ? match.Groups["key"].Value
                    : match.Groups["key2"].Value);

            if (string.IsNullOrEmpty(photoKey)
                || !TryResolvePhotos(photosByKey, cursors, photoKey, out var photos, out var lookupKey))
            {
                RemoveTextRange(textNodes, match.Index, match.Length);
                continue;
            }

            var index = cursors[lookupKey];
            cursors[lookupKey] = index + 1;
            var imageBytes = index < photos.Count ? photos[index] : Array.Empty<byte>();

            var anchor = GetFirstTextInRange(textNodes, match.Index, match.Length);
            RemoveTextRange(textNodes, match.Index, match.Length);

            if (imageBytes.Length == 0 || anchor == null)
                continue;

            var (widthEmu, heightEmu) = ResolveSizeEmu(match.Value, imageBytes, anchor.Value.Run);
            nextDrawingId = InsertInlineImage(mainPart, anchor.Value.Run, anchor.Value.Text, imageBytes, widthEmu, heightEmu, nextDrawingId);
        }

        return nextDrawingId;
    }

    internal static string NormalizeImageKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var buffer = new char[raw.Length];
        var n = 0;
        foreach (var ch in raw)
        {
            if (char.IsWhiteSpace(ch) || ch is '\u00AD' or '\u200B' or '\u200C' or '\u200D' or '-')
                continue;
            buffer[n++] = ch;
        }

        return n == 0 ? string.Empty : new string(buffer, 0, n);
    }

    private static bool TryResolvePhotos(
        IReadOnlyDictionary<string, IReadOnlyList<byte[]>> photosByKey,
        Dictionary<string, int> cursors,
        string photoKey,
        out IReadOnlyList<byte[]> photos,
        out string lookupKey)
    {
        photos = Array.Empty<byte[]>();
        lookupKey = photoKey = NormalizeImageKey(photoKey);
        if (lookupKey.Length == 0)
            return false;

        if (photosByKey.TryGetValue(photoKey, out photos!))
            return EnsureCursor(cursors, photoKey);

        var canonical = UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath(photoKey);
        if (!string.Equals(canonical, photoKey, StringComparison.OrdinalIgnoreCase)
            && photosByKey.TryGetValue(canonical, out photos!))
        {
            lookupKey = canonical;
            return EnsureCursor(cursors, lookupKey);
        }

        if (UserReportPlaceholderAliasRegistry.TryGetShortCode(photoKey, out var shortCode)
            && photosByKey.TryGetValue(shortCode, out photos!))
        {
            lookupKey = shortCode;
            return EnsureCursor(cursors, lookupKey);
        }

        photos = Array.Empty<byte[]>();
        return false;
    }

    private static bool EnsureCursor(Dictionary<string, int> cursors, string key)
    {
        if (!cursors.ContainsKey(key))
            cursors[key] = 0;
        return true;
    }

    private static (Run Run, Text Text)? GetFirstTextInRange(IReadOnlyList<Text> textNodes, int start, int length)
    {
        var end = start + length;
        var pos = 0;
        foreach (var text in textNodes)
        {
            var segment = text.Text ?? string.Empty;
            var segEnd = pos + segment.Length;
            if (segEnd > start && pos < end && text.Parent is Run run)
                return (run, text);
            pos = segEnd;
        }

        return null;
    }

    private static void RemoveTextRange(IReadOnlyList<Text> textNodes, int start, int length)
    {
        var end = start + length;
        var pos = 0;
        foreach (var text in textNodes)
        {
            var segment = text.Text ?? string.Empty;
            var segStart = pos;
            var segEnd = pos + segment.Length;
            pos = segEnd;

            if (segEnd <= start || segStart >= end)
                continue;

            var removeStart = Math.Max(0, start - segStart);
            var removeEnd = Math.Min(segment.Length, end - segStart);
            text.Text = segment.Remove(removeStart, removeEnd - removeStart);
        }
    }

    private static uint InsertInlineImage(
        MainDocumentPart mainPart,
        Run run,
        Text text,
        byte[] imageBytes,
        long widthEmu,
        long heightEmu,
        uint drawingId)
    {
        var partType = imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8
            ? ImagePartType.Jpeg
            : ImagePartType.Png;

        var imagePart = mainPart.AddImagePart(partType);
        using (var stream = new MemoryStream(imageBytes, writable: false))
            imagePart.FeedData(stream);

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        var drawing = BuildInlineDrawing(relationshipId, drawingId, widthEmu, heightEmu);

        text.Text = string.Empty;
        run.AppendChild(drawing);
        return drawingId + 1;
    }

    private static Drawing BuildInlineDrawing(string relationshipId, uint drawingId, long widthEmu, long heightEmu)
    {
        var graphicData = new A.GraphicData(
            new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"Photo {drawingId}.png" },
                    new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                    new A.Blip { Embed = relationshipId },
                    new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
        {
            Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
        };

        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L,
                },
                new DW.DocProperties { Id = drawingId, Name = $"Picture {drawingId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(graphicData))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
            });
    }

    private static (long WidthEmu, long HeightEmu) ResolveSizeEmu(string tokenText, byte[] imageBytes, Run run)
    {
        if (!TryGetImagePixelSize(imageBytes, out var pixelWidth, out var pixelHeight))
        {
            pixelWidth = 113;
            pixelHeight = 151;
        }

        if (tokenText.Contains("keepratio", StringComparison.OrdinalIgnoreCase)
            && TryGetTableCellWidthEmu(run, out var cellWidthEmu))
        {
            var scale = (double)cellWidthEmu / PixelsToEmu(pixelWidth);
            var heightEmu = (long)(PixelsToEmu(pixelHeight) * scale);
            return (cellWidthEmu, heightEmu);
        }

        var widthMm = TryParseMm(tokenText, 'w') ?? 35;
        var heightMm = TryParseMm(tokenText, 'h') ?? 45;
        return (widthMm * EmuPerMm, heightMm * EmuPerMm);
    }

    private static int? TryParseMm(string token, char dimension)
    {
        var match = Regex.Match(
            token,
            $@"{dimension}:(\d+)\s*mm",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static bool TryGetTableCellWidthEmu(Run run, out long cellWidthEmu)
    {
        cellWidthEmu = 0;
        var cell = run.Ancestors<TableCell>().FirstOrDefault();
        var width = cell?.TableCellProperties?.GetFirstChild<TableCellWidth>()?.Width?.Value;
        if (width == null || !int.TryParse(width, out var dxa) || dxa <= 0)
            return false;

        // DXA (twentieths of a point) → EMU: dxa / 20 * 12700
        cellWidthEmu = (long)dxa * 12700L / 20L;
        return cellWidthEmu > 0;
    }

    private static long PixelsToEmu(int pixels) => (long)pixels * 9525L;

    private static bool TryGetImagePixelSize(byte[] bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length >= 24
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47)
        {
            width = (bytes[16] & 0xFF) << 24 | (bytes[17] & 0xFF) << 16 | (bytes[18] & 0xFF) << 8 | (bytes[19] & 0xFF);
            height = (bytes[20] & 0xFF) << 24 | (bytes[21] & 0xFF) << 16 | (bytes[22] & 0xFF) << 8 | (bytes[23] & 0xFF);
            if (width > 0 && height > 0)
                return true;
        }

        return false;
    }

    private static uint GetMaxDrawingPropertyId(MainDocumentPart mainPart)
    {
        uint max = 0;
        foreach (var prop in mainPart.Document.Descendants<DW.DocProperties>())
        {
            if (prop.Id?.Value is uint id && id > max)
                max = id;
        }

        return max;
    }
}
