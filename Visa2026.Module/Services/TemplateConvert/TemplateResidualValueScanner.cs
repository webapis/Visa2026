using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <inheritdoc cref="ITemplateResidualValueScanner"/>
public sealed class TemplateResidualValueScanner : ITemplateResidualValueScanner
{
    public ResidualValueScanResult Scan(byte[] content, TemplateSourceFormat format, IReadOnlyList<ResidualValueProbe> probes)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(probes);

        if (probes.Count == 0)
            return ResidualValueScanResult.Clean();

        var segments = format switch
        {
            TemplateSourceFormat.Docx => ReadWordSegments(content),
            TemplateSourceFormat.Xlsx => ReadExcelSegments(content),
            _ => throw new NotSupportedException($"Unsupported template format '{format}'."),
        };

        var hits = new List<ResidualValueHit>();
        foreach (var probe in probes)
        {
            if (!TemplateTextNormalizer.IsMatchable(probe.Value))
                continue;

            var needle = probe.Kind == ResidualProbeKind.Identifier
                ? TemplateTextNormalizer.NormalizeIdentifier(probe.Value)
                : TemplateTextNormalizer.NormalizeFolded(probe.Value);

            if (needle.Length < TemplateTextNormalizer.MinimumMatchLength)
                continue;

            foreach (var (location, text) in segments)
            {
                var haystack = probe.Kind == ResidualProbeKind.Identifier
                    ? TemplateTextNormalizer.NormalizeIdentifier(text)
                    : TemplateTextNormalizer.NormalizeFolded(text);

                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    hits.Add(new ResidualValueHit(probe.Label, probe.Value, location));
                    break;
                }
            }
        }

        return hits.Count == 0 ? ResidualValueScanResult.Clean() : new ResidualValueScanResult(false, hits);
    }

    private static List<(string Location, string Text)> ReadWordSegments(byte[] content)
    {
        var segments = new List<(string, string)>();
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);

        foreach (var paragraph in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var text = WordTemplateAddressing.GetParagraphText(paragraph.Paragraph);
            if (!string.IsNullOrWhiteSpace(text))
                segments.Add((paragraph.Address, text));
        }

        return segments;
    }

    private static List<(string Location, string Text)> ReadExcelSegments(byte[] content)
    {
        var segments = new List<(string, string)>();
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);

        foreach (var worksheet in workbook.Worksheets)
        {
            foreach (var cell in worksheet.CellsUsed())
            {
                var text = cell.GetFormattedString();
                if (!string.IsNullOrWhiteSpace(text))
                    segments.Add(($"{worksheet.Name}!{cell.Address.ToStringRelative()}", text));
            }
        }

        return segments;
    }
}
