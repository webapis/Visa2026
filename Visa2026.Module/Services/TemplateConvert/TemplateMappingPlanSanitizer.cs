#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Drops substitutions whose token is outside the profile set, whose region is unknown, or that
/// overlap another kept substitution. Same region rules for loop markers. This - not the prompt -
/// is what makes Q11 / Q12 / Q13 hold.
/// </summary>
public sealed class TemplateMappingPlanSanitizer : ITemplateMappingPlanSanitizer
{
    public TemplateMappingPlan Sanitize(
        TemplateMappingPlan proposed,
        ApplicationProfilePlaceholderSet allowedSet,
        IReadOnlyList<DocumentExtractRegion> knownRegions,
        out IReadOnlyList<string> dropped)
    {
        ArgumentNullException.ThrowIfNull(proposed);
        ArgumentNullException.ThrowIfNull(allowedSet);
        ArgumentNullException.ThrowIfNull(knownRegions);

        var known = knownRegions
            .Select(static r => r.Region)
            .ToHashSet();

        var drops = new List<string>();
        var kept = new List<TokenSubstitution>();

        foreach (var sub in proposed.Substitutions ?? Array.Empty<TokenSubstitution>())
        {
            if (!known.Contains(sub.Region))
            {
                drops.Add($"Unknown region for token {DescribeToken(sub.Token)}.");
                continue;
            }

            if (!allowedSet.Contains(sub.Token))
            {
                drops.Add($"Token {DescribeToken(sub.Token)} is not in the profile placeholder set.");
                continue;
            }

            if (OverlapsAny(sub.Region, kept.Select(static k => k.Region)))
            {
                drops.Add($"Overlapping region for token {DescribeToken(sub.Token)}.");
                continue;
            }

            kept.Add(sub);
        }

        var keptLoops = new List<LoopMarker>();
        foreach (var loop in proposed.Loops ?? Array.Empty<LoopMarker>())
        {
            if (!known.Contains(loop.Start) || !known.Contains(loop.End))
            {
                drops.Add($"Loop {loop.CollectionToken} references an unknown region.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(loop.CollectionToken))
            {
                drops.Add("Loop is missing a collection token.");
                continue;
            }

            if (OverlapsAny(loop.Start, kept.Select(static k => k.Region))
                || OverlapsAny(loop.End, kept.Select(static k => k.Region))
                || OverlapsAny(loop.Start, keptLoops.Select(static l => l.Start).Concat(keptLoops.Select(static l => l.End)))
                || OverlapsAny(loop.End, keptLoops.Select(static l => l.Start).Concat(keptLoops.Select(static l => l.End))))
            {
                drops.Add($"Loop {loop.CollectionToken} overlaps another edit.");
                continue;
            }

            keptLoops.Add(loop);
        }

        // Gaps are advisory for the officer / developer packet - keep only those on known regions.
        var keptGaps = (proposed.Gaps ?? Array.Empty<MappingGap>())
            .Where(g => known.Contains(g.Region))
            .ToList();

        dropped = drops;
        return new TemplateMappingPlan(kept, keptLoops, keptGaps, proposed.Rationale);
    }

    private static string DescribeToken(string token) =>
        TemplateTokenSyntax.TryGetShortCode(token, out var code) ? code : token;

    private static bool OverlapsAny(DocumentRegion region, IEnumerable<DocumentRegion> others) =>
        others.Any(other => RegionsOverlap(region, other));

    internal static bool RegionsOverlap(DocumentRegion a, DocumentRegion b)
    {
        if (a is DocumentRegion.WordSpan wa && b is DocumentRegion.WordSpan wb)
        {
            if (!string.Equals(wa.ParagraphAddress, wb.ParagraphAddress, StringComparison.Ordinal))
                return false;

            var aEnd = wa.Start + wa.Length;
            var bEnd = wb.Start + wb.Length;
            return wa.Start < bEnd && wb.Start < aEnd;
        }

        if (a is DocumentRegion.ExcelCell ea && b is DocumentRegion.ExcelCell eb)
        {
            return string.Equals(ea.SheetName, eb.SheetName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(ea.CellReference, eb.CellReference, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}