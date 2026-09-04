#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Builds a Word layout from the merged field plan when AI layout is off or unavailable.
/// Prefers OCR reading order (structure-preserving) over a flat label:token catalog.
/// </summary>
public static class DeterministicScanDocxLayoutPlanner
{
    public static ScanDocxLayoutProposal Build(ScanDocxLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FieldPlan);

        if (request.OcrLines.Count > 0)
            return BuildFromOcr(request);

        return BuildFlatFieldList(request);
    }

    private static ScanDocxLayoutProposal BuildFromOcr(ScanDocxLayoutRequest request)
    {
        var plan = request.FieldPlan;
        var replacements = BuildValueReplacements(request);
        var blocks = new List<ScanDocxBlock>();

        foreach (var line in request.OcrLines)
        {
            var text = line.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                blocks.Add(new ScanDocxBlock { Kind = "blank" });
                continue;
            }

            var replaced = ApplyReplacements(text, replacements);
            blocks.Add(new ScanDocxBlock
            {
                Kind = "paragraph",
                Text = replaced,
                Align = "left",
            });
        }

        AppendRowLoop(blocks, plan);

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = "deterministic-ocr-layout",
        };
    }

    private static ScanDocxLayoutProposal BuildFlatFieldList(ScanDocxLayoutRequest request)
    {
        var plan = request.FieldPlan;
        var blocks = new List<ScanDocxBlock>();

        foreach (var region in plan.StaticRegions
                     .OrderBy(static r => r.PageIndex)
                     .ThenBy(static r => r.Box.Top))
        {
            if (!string.IsNullOrWhiteSpace(region.TextPreview))
            {
                blocks.Add(new ScanDocxBlock
                {
                    Kind = "paragraph",
                    Text = region.TextPreview.Trim(),
                    Align = "left",
                });
            }
        }

        var headerFields = plan.Fields
            .Where(static f => f.Scope != ScanFieldScope.Row && !string.IsNullOrWhiteSpace(f.ProposedToken))
            .OrderBy(static f => f.PageIndex)
            .ThenBy(static f => f.Box.Top)
            .ThenBy(static f => f.LabelText, StringComparer.OrdinalIgnoreCase);

        foreach (var field in headerFields)
        {
            blocks.Add(new ScanDocxBlock
            {
                Kind = "field",
                Text = field.LabelText,
                Token = field.ProposedToken,
                Align = "left",
            });
        }

        AppendRowLoop(blocks, plan);

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = "deterministic-layout",
        };
    }

    private static void AppendRowLoop(List<ScanDocxBlock> blocks, ScanFieldPlan plan)
    {
        var rowFields = plan.Fields
            .Where(static f => f.Scope == ScanFieldScope.Row && !string.IsNullOrWhiteSpace(f.ProposedToken))
            .OrderBy(static f => f.PageIndex)
            .ThenBy(static f => f.Box.Top)
            .ThenBy(static f => f.LabelText, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rowFields.Count == 0)
            return;

        blocks.Add(new ScanDocxBlock { Kind = "loopOpen", Token = "{{#ds.rows}}" });
        foreach (var field in rowFields)
        {
            blocks.Add(new ScanDocxBlock
            {
                Kind = "field",
                Text = field.LabelText,
                Token = field.ProposedToken,
            });
        }

        blocks.Add(new ScanDocxBlock { Kind = "loopClose", Token = "{{/ds.rows}}" });
    }

    private static List<(string Value, string Token)> BuildValueReplacements(ScanDocxLayoutRequest request)
    {
        var list = new List<(string Value, string Token)>();
        foreach (var hint in request.ValueHints)
        {
            if (string.IsNullOrWhiteSpace(hint.Token) || string.IsNullOrWhiteSpace(hint.MaskedValue))
                continue;

            var value = hint.MaskedValue.Trim();
            if (value.Length < 2)
                continue;

            list.Add((value, hint.Token.Trim()));
        }

        // Longer values first so partial overlaps prefer the fuller match.
        return list
            .OrderByDescending(static x => x.Value.Length)
            .DistinctBy(static x => x.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ApplyReplacements(string text, List<(string Value, string Token)> replacements)
    {
        var result = text;
        foreach (var (value, token) in replacements)
        {
            if (result.Contains(value, StringComparison.OrdinalIgnoreCase))
                result = Regex.Replace(result, Regex.Escape(value), token, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return result;
    }

}