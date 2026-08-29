#nullable enable

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanDraftDocxBuilder
{
    ScanDraftDocxResult Build(ScanDraftDocxRequest request);
}

public sealed class ScanDraftDocxBuilder : IScanDraftDocxBuilder
{
    private const string FontName = "Times New Roman";
    private const string FontSizeHalfPts = "24";

    private static readonly Regex TokenPattern = new(
        @"\{\{[^{}]+\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ScanDraftDocxResult Build(ScanDraftDocxRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Layout);
        ArgumentNullException.ThrowIfNull(request.FieldPlan);

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            foreach (var block in request.Layout.Blocks)
                AppendBlock(body, block);

            mainPart.Document.Save();
        }

        var content = stream.ToArray();
        return new ScanDraftDocxResult
        {
            Content = content,
            EmittedTokens = CollectEmittedTokens(request.Layout),
        };
    }

    private static void AppendBlock(Body body, ScanDocxBlock block)
    {
        switch (block.Kind)
        {
            case "blank":
                AppendParagraph(body, string.Empty, block.Align, block.Style);
                break;
            case "twoColumn":
            case "columns":
            case "row":
                AppendTwoColumn(body, block);
                break;
            case "paragraph":
            case "static":
                AppendParagraph(body, block.Text ?? string.Empty, block.Align, block.Style);
                break;
            case "field":
                var token = block.Token?.Trim();
                if (string.IsNullOrWhiteSpace(token))
                    break;

                var label = block.Text?.Trim();
                var line = string.IsNullOrWhiteSpace(label) ? token : $"{label}: {token}";
                AppendParagraph(body, line, block.Align, block.Style);
                break;
            case "loopOpen":
            case "loopClose":
                if (!string.IsNullOrWhiteSpace(block.Token))
                    AppendParagraph(body, block.Token.Trim(), block.Align, block.Style);
                break;
            default:
                if (!string.IsNullOrWhiteSpace(block.Text))
                    AppendParagraph(body, block.Text.Trim(), block.Align, block.Style);
                else if (!string.IsNullOrWhiteSpace(block.Token))
                    AppendParagraph(body, block.Token.Trim(), block.Align, block.Style);
                break;
        }
    }

    private static void AppendTwoColumn(Body body, ScanDocxBlock block)
    {
        var left = block.Text ?? string.Empty;
        var right = block.RightText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
            return;

        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil })),
            new TableGrid(
                new GridColumn { Width = "2500" },
                new GridColumn { Width = "2500" }));

        var row = new TableRow();
        row.Append(
            CreateCell(left, block.Align ?? "left", block.Style),
            CreateCell(right, block.RightAlign ?? "right", block.RightStyle ?? block.Style));
        table.Append(row);
        body.AppendChild(table);
    }

    private static TableCell CreateCell(string text, string? align, string? style)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "2500", Type = TableWidthUnitValues.Pct },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }));

        var lines = SplitLines(text);
        if (lines.Count == 0)
            lines.Add(string.Empty);

        foreach (var line in lines)
            cell.Append(BuildParagraph(line, align, style, afterTwips: "40"));

        return cell;
    }

    private static void AppendParagraph(Body body, string text, string? align, string? style)
    {
        body.AppendChild(BuildParagraph(text, align, style, afterTwips: "120"));
    }

    private static Paragraph BuildParagraph(string text, string? align, string? style, string afterTwips)
    {
        var props = new ParagraphProperties(
            new SpacingBetweenLines { After = afterTwips },
            new Justification { Val = MapAlign(align) });

        var paragraph = new Paragraph(props);
        foreach (var runText in SplitPreserveTokens(text))
            paragraph.AppendChild(new Run(CreateRunProperties(style), new Text(runText) { Space = SpaceProcessingModeValues.Preserve }));

        return paragraph;
    }

    private static RunProperties CreateRunProperties(string? style)
    {
        var props = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = FontSizeHalfPts },
            new FontSizeComplexScript { Val = FontSizeHalfPts });

        var normalized = NormalizeStyle(style);
        if (normalized is "italic" or "bolditalic")
            props.AppendChild(new Italic());
        if (normalized is "bold" or "bolditalic")
            props.AppendChild(new Bold());

        return props;
    }

    private static JustificationValues MapAlign(string? align)
    {
        if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Right;
        if (string.Equals(align, "center", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Center;
        if (string.Equals(align, "justify", StringComparison.OrdinalIgnoreCase)
            || string.Equals(align, "both", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Both;

        return JustificationValues.Left;
    }

    private static string NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
            return "normal";

        var value = style.Trim().ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        return value switch
        {
            "italic" or "italics" or "i" => "italic",
            "bold" or "b" or "strong" => "bold",
            "bolditalic" or "italicbold" or "bolditalics" => "bolditalic",
            _ => "normal",
        };
    }

    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .ToList();
    }

    private static IEnumerable<string> SplitPreserveTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield return string.Empty;
            yield break;
        }

        var index = 0;
        foreach (Match match in TokenPattern.Matches(text))
        {
            if (match.Index > index)
                yield return text[index..match.Index];

            yield return match.Value;
            index = match.Index + match.Length;
        }

        if (index < text.Length)
            yield return text[index..];
        else if (index == 0)
            yield return text;
    }

    private static IReadOnlyList<string> CollectEmittedTokens(ScanDocxLayoutProposal layout)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in layout.Blocks)
        {
            if (!string.IsNullOrWhiteSpace(block.Token)
                && TemplateTokenSyntax.TryGetShortCode(block.Token, out _))
                tokens.Add(block.Token.Trim());

            CollectTokensFromText(block.Text, tokens);
            CollectTokensFromText(block.RightText, tokens);
        }

        return tokens.OrderBy(static t => t, StringComparer.Ordinal).ToList();
    }

    private static void CollectTokensFromText(string? text, HashSet<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (Match match in TokenPattern.Matches(text))
        {
            var token = match.Value;
            if (TemplateTokenSyntax.TryGetShortCode(token, out _))
                tokens.Add(token);
        }
    }
}