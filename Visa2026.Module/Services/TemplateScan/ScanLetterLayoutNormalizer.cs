#nullable enable

using System.Text.RegularExpressions;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Post-processes AI/OCR layout blocks into a ministry-letter Word structure:
/// left header (№ + date) + right addressee, italic urgency, justified body, bold split signature.
/// </summary>
public static class ScanLetterLayoutNormalizer
{
    private static readonly Regex Addressee = new(
        @"migrasi[ýy]a\s+gullugyna|d[öo]wlet\s+migrasi",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex UrgencyLiteral = new(
        @"Adaty\s+tertipde|Gyssagly\s+tertipde|Oran\s+gyssagly",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SignatoryTitle = new(
        @"şahamça|mudiri|müdiri|direktor|director",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DateLike = new(
        @"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(?:\s*ý\.?)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static ScanDocxLayoutProposal Apply(
        ScanDocxLayoutProposal proposal,
        IReadOnlyList<ScanOcrLine>? ocrLines = null)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (proposal.Blocks.Count == 0)
            return proposal;

        var lines = FlattenLines(proposal.Blocks);
        InjectOcrAddresseeIfMissing(lines, ocrLines);

        var rebuilt = TryRebuildFromLines(lines, proposal);
        if (rebuilt != null)
            return rebuilt;

        return FixMisplacedHeaderDate(PolishExisting(proposal), ocrLines);
    }

    private static void InjectOcrAddresseeIfMissing(List<FlatLine> lines, IReadOnlyList<ScanOcrLine>? ocrLines)
    {
        if (ocrLines == null || ocrLines.Count == 0)
            return;
        if (lines.Any(static l => LooksLikeAddressee(l.Text)))
            return;

        var band = ExtractAddresseeBandFromOcr(ocrLines);
        if (band.Count == 0)
            return;

        var insertAt = 0;
        while (insertAt < lines.Count && LooksLikeHeaderLeft(lines[insertAt].Text))
            insertAt++;

        for (var i = 0; i < band.Count; i++)
            lines.Insert(insertAt + i, new FlatLine(band[i]));
    }

    private static List<string> ExtractAddresseeBandFromOcr(IReadOnlyList<ScanOcrLine> ocrLines)
    {
        var texts = ocrLines
            .Select(static l => l.Text?.Trim() ?? string.Empty)
            .Where(static t => t.Length > 0)
            .ToList();

        var idx = texts.FindIndex(LooksLikeAddressee);
        if (idx < 0)
            return new List<string>();

        var start = idx;
        while (start > 0 && LooksLikeAddresseeBand(texts[start - 1]))
            start--;

        var end = idx;
        while (end + 1 < texts.Count && LooksLikeAddresseeBand(texts[end + 1]))
            end++;

        return texts.Skip(start).Take(end - start + 1).ToList();
    }

    private static ScanDocxLayoutProposal? TryRebuildFromLines(List<FlatLine> lines, ScanDocxLayoutProposal proposal)
    {
        if (lines.Count < 4)
            return null;

        var addresseeIdx = lines.FindIndex(static l => LooksLikeAddressee(l.Text));
        if (addresseeIdx < 0)
            return null;

        var urgencyIdx = lines.FindIndex(static l => LooksLikeUrgency(l.Text));
        var titleIdx = lines.FindLastIndex(static l => LooksLikeSignatoryTitle(l.Text));
        if (titleIdx < 0 || titleIdx <= addresseeIdx)
            return null;

        var nameIdx = FindSignatoryNameIndex(lines, titleIdx);

        var headerEnd = addresseeIdx;
        while (headerEnd > 0 && LooksLikeAddresseeBand(lines[headerEnd - 1].Text))
            headerEnd--;

        var headerLines = lines.Take(headerEnd)
            .Where(static l => !string.IsNullOrWhiteSpace(l.Text))
            .Where(static l => !LooksLikeUrgency(l.Text))
            .Where(static l => LooksLikeHeaderLeft(l.Text) || !LooksLikeAddresseeBand(l.Text))
            .Select(static l => l.Text.Trim())
            .Where(LooksLikeHeaderLeft)
            .ToList();

        // Fallback: keep non-addressee preamble as header if AFNUM/ADAT detection was too strict.
        if (headerLines.Count == 0)
        {
            headerLines = lines.Take(headerEnd)
                .Where(static l => !string.IsNullOrWhiteSpace(l.Text))
                .Where(static l => !LooksLikeUrgency(l.Text))
                .Select(static l => l.Text.Trim())
                .ToList();
        }

        var addresseeLines = lines.Skip(headerEnd).Take(addresseeIdx - headerEnd + 1)
            .Where(static l => !string.IsNullOrWhiteSpace(l.Text))
            .Select(static l => l.Text.Trim())
            .ToList();

        var addresseeLast = addresseeIdx;
        while (addresseeLast + 1 < lines.Count
               && !string.IsNullOrWhiteSpace(lines[addresseeLast + 1].Text)
               && addresseeLast + 1 < (urgencyIdx >= 0 ? urgencyIdx : titleIdx)
               && LooksLikeAddresseeBand(lines[addresseeLast + 1].Text))
        {
            addresseeLast++;
            addresseeLines.Add(lines[addresseeLast].Text.Trim());
        }

        addresseeLines = addresseeLines
            .Where(static t => !LooksLikeHeaderLeft(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (headerLines.Count == 0 || addresseeLines.Count == 0)
            return null;

        var bodyStart = Math.Max(addresseeLast, urgencyIdx) + 1;
        var bodyEnd = titleIdx;
        var bodyLines = lines.Skip(bodyStart).Take(Math.Max(0, bodyEnd - bodyStart))
            .Where(static l => !string.IsNullOrWhiteSpace(l.Text))
            .Where(static l => !LooksLikeUrgency(l.Text))
            .Where(static l => !LooksLikeAddresseeBand(l.Text))
            .Select(static l => l.Text.Trim())
            .ToList();

        var blocks = new List<ScanDocxBlock>
        {
            new ScanDocxBlock
            {
                Kind = "twoColumn",
                Text = string.Join("\n", headerLines),
                RightText = string.Join("\n", addresseeLines),
                Align = "left",
                RightAlign = "right",
            },
            new ScanDocxBlock { Kind = "blank" },
        };

        if (urgencyIdx >= 0)
        {
            blocks.Add(new ScanDocxBlock
            {
                Kind = "paragraph",
                Text = lines[urgencyIdx].Text.Trim(),
                Align = "left",
                Style = "italic",
            });
            blocks.Add(new ScanDocxBlock { Kind = "blank" });
        }

        foreach (var body in bodyLines)
        {
            blocks.Add(new ScanDocxBlock
            {
                Kind = "paragraph",
                Text = body,
                Align = "justify",
            });
        }

        blocks.Add(new ScanDocxBlock { Kind = "blank" });

        var titleText = lines[titleIdx].Text.Trim();
        if (nameIdx > titleIdx + 1)
        {
            titleText = string.Join(
                "\n",
                lines.Skip(titleIdx).Take(nameIdx - titleIdx)
                    .Select(static l => l.Text.Trim())
                    .Where(static t => t.Length > 0));
        }

        var nameText = nameIdx >= 0 ? lines[nameIdx].Text.Trim() : string.Empty;
        blocks.Add(new ScanDocxBlock
        {
            Kind = "twoColumn",
            Text = titleText,
            RightText = nameText,
            Align = "left",
            RightAlign = "right",
            Style = "bold",
            RightStyle = "bold",
        });

        foreach (var block in proposal.Blocks)
        {
            if (string.Equals(block.Kind, "loopOpen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(block.Kind, "loopClose", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(block.Kind, "field", StringComparison.OrdinalIgnoreCase) && IsRowLike(block)))
            {
                blocks.Add(block);
            }
        }

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = AppendRationale(proposal.Rationale, "letter-rebuild"),
        };
    }

    private static ScanDocxLayoutProposal FixMisplacedHeaderDate(
        ScanDocxLayoutProposal proposal,
        IReadOnlyList<ScanOcrLine>? ocrLines)
    {
        var blocks = proposal.Blocks.ToList();
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (!string.Equals(block.Kind, "twoColumn", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!LooksLikeHeaderLeft(block.Text) || !LooksLikeHeaderLeft(block.RightText))
                continue;
            if (LooksLikeAddressee(block.RightText) || LooksLikeAddresseeBand(block.RightText))
                continue;

            var addressee = ExtractAddresseeBandFromOcr(ocrLines ?? Array.Empty<ScanOcrLine>());
            if (addressee.Count == 0)
            {
                // Pull addressee from later paragraph blocks.
                addressee = blocks
                    .Skip(i + 1)
                    .Where(static b => LooksLikeAddressee(b.Text) || LooksLikeAddresseeBand(b.Text))
                    .Select(static b => b.Text!.Trim())
                    .Take(3)
                    .ToList();
            }

            if (addressee.Count == 0)
                continue;

            blocks[i] = new ScanDocxBlock
            {
                Kind = "twoColumn",
                Text = string.Join("\n", new[] { block.Text?.Trim(), block.RightText?.Trim() }.Where(static t => !string.IsNullOrWhiteSpace(t))!),
                RightText = string.Join("\n", addressee),
                Align = "left",
                RightAlign = "right",
                Style = block.Style,
                RightStyle = block.RightStyle,
            };

            // Drop duplicate addressee paragraphs now folded into the header row.
            blocks = blocks
                .Where((b, idx) => idx <= i || !(LooksLikeAddressee(b.Text) || LooksLikeAddresseeBand(b.Text)))
                .ToList();
            break;
        }

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = AppendRationale(proposal.Rationale, "letter-fix-header"),
        };
    }

    private static ScanDocxLayoutProposal PolishExisting(ScanDocxLayoutProposal proposal)
    {
        var blocks = new List<ScanDocxBlock>(proposal.Blocks.Count);
        foreach (var block in proposal.Blocks)
        {
            if (string.Equals(block.Kind, "blank", StringComparison.OrdinalIgnoreCase)
                || string.Equals(block.Kind, "loopOpen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(block.Kind, "loopClose", StringComparison.OrdinalIgnoreCase)
                || string.Equals(block.Kind, "field", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add(block);
                continue;
            }

            if (string.Equals(block.Kind, "twoColumn", StringComparison.OrdinalIgnoreCase))
            {
                var isSignature = LooksLikeSignatoryTitle(block.Text) || LooksLikeSignatoryTitle(block.RightText);
                blocks.Add(new ScanDocxBlock
                {
                    Kind = "twoColumn",
                    Text = block.Text,
                    RightText = block.RightText,
                    Align = block.Align ?? "left",
                    RightAlign = block.RightAlign ?? "right",
                    Style = isSignature ? (block.Style ?? "bold") : block.Style,
                    RightStyle = isSignature ? (block.RightStyle ?? "bold") : block.RightStyle,
                });
                continue;
            }

            var text = block.Text ?? string.Empty;
            var align = block.Align;
            var style = block.Style;
            if (LooksLikeUrgency(text))
            {
                align = "left";
                style = style ?? "italic";
            }
            else if (LooksLikeBodyParagraph(text))
            {
                align = "justify";
            }

            blocks.Add(new ScanDocxBlock
            {
                Kind = string.IsNullOrWhiteSpace(block.Kind) ? "paragraph" : block.Kind,
                Text = block.Text,
                Token = block.Token,
                Align = align,
                Style = style,
                RightText = block.RightText,
                RightAlign = block.RightAlign,
                RightStyle = block.RightStyle,
            });
        }

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = AppendRationale(proposal.Rationale, "letter-polish"),
        };
    }

    private static int FindSignatoryNameIndex(List<FlatLine> lines, int titleIdx)
    {
        for (var i = titleIdx + 1; i < lines.Count; i++)
        {
            var text = lines[i].Text.Trim();
            if (text.Length == 0)
                continue;
            if (LooksLikeSignatoryTitle(text))
                continue;
            return i;
        }

        return -1;
    }

    private static List<FlatLine> FlattenLines(IReadOnlyList<ScanDocxBlock> blocks)
    {
        var lines = new List<FlatLine>();
        foreach (var block in blocks)
        {
            if (string.Equals(block.Kind, "blank", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(new FlatLine(string.Empty));
                continue;
            }

            if (string.Equals(block.Kind, "twoColumn", StringComparison.OrdinalIgnoreCase))
            {
                // Keep left cell together for header detection; expand right separately.
                foreach (var part in SplitLines(block.Text))
                    lines.Add(new FlatLine(part));
                foreach (var part in SplitLines(block.RightText))
                    lines.Add(new FlatLine(part));
                continue;
            }

            if (string.Equals(block.Kind, "field", StringComparison.OrdinalIgnoreCase))
            {
                var token = block.Token?.Trim();
                var label = block.Text?.Trim();
                var line = string.IsNullOrWhiteSpace(label) ? token : label + ": " + token;
                lines.Add(new FlatLine(line ?? string.Empty));
                continue;
            }

            foreach (var part in SplitLines(block.Text ?? block.Token))
                lines.Add(new FlatLine(part));
        }

        return lines;
    }

    private static IEnumerable<string> SplitLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
            yield return line;
    }

    private static bool LooksLikeHeaderLeft(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (text.Contains("AFNUM", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ADAT", StringComparison.OrdinalIgnoreCase)
            || text.Contains('№')
            || text.Contains('N') && text.Contains('/'))
            return true;
        return DateLike.IsMatch(text);
    }

    private static bool LooksLikeAddressee(string? text) =>
        !string.IsNullOrWhiteSpace(text) && Addressee.IsMatch(text);

    private static bool LooksLikeAddresseeBand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (LooksLikeAddressee(text))
            return true;
        if (LooksLikeUrgency(text) || LooksLikeSignatoryTitle(text) || LooksLikeBodyParagraph(text) || LooksLikeHeaderLeft(text))
            return false;

        var trimmed = text.Trim();
        return trimmed.Length <= 48
            && (trimmed.Contains("Döwlet", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("Dowlet", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("Türkmenistan", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("Turkmenistan", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("gullugyna", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeUrgency(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (text.Contains("Urgency_NameTm", StringComparison.OrdinalIgnoreCase))
            return true;
        return UrgencyLiteral.IsMatch(text);
    }

    private static bool LooksLikeSignatoryTitle(string? text) =>
        !string.IsNullOrWhiteSpace(text) && SignatoryTitle.IsMatch(text);

    private static bool LooksLikeBodyParagraph(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 80)
            return false;
        if (LooksLikeAddressee(text) || LooksLikeUrgency(text) || LooksLikeSignatoryTitle(text))
            return false;
        return true;
    }

    private static bool IsRowLike(ScanDocxBlock block) =>
        block.Token != null
        && (block.Token.Contains("{{#", StringComparison.Ordinal)
            || block.Token.Contains("{{/", StringComparison.Ordinal)
            || block.Token.Contains(".rows", StringComparison.OrdinalIgnoreCase));

    private static string AppendRationale(string? existing, string tag)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return tag;
        if (existing.Contains(tag, StringComparison.OrdinalIgnoreCase))
            return existing;
        return existing.Trim() + ";" + tag;
    }

    private readonly record struct FlatLine(string Text);
}