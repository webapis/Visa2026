#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Builds a yellow field-plan proposal from Office package marks (no vision).</summary>
public static class ScanOfficeFieldPlanBuilder
{
    public static ScanFieldPlanProposal Build(
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(yellows);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        var drafts = new List<ScanDetectedFieldDraft>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var yellow in yellows)
        {
            var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
                yellow.Text,
                ScanBoundingBox.FullPage,
                yellow.PageIndex,
                placeholderSet,
                used,
                yellow.Region);

            if (resolved.Count > 0)
            {
                drafts.AddRange(resolved);
                continue;
            }

            // Keep the yellow mark as a draft for merger gap / further mapping.
            drafts.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = yellow.PageIndex,
                LabelText = yellow.Text,
                ProposedToken = null,
                Confidence = ScanFieldConfidence.Medium,
                Scope = ScanFieldScope.Header,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = yellow.Region,
            });
        }

        return new ScanFieldPlanProposal
        {
            Fields = drafts,
            Gaps = Array.Empty<ScanGapDraft>(),
            YellowHighlightCount = yellows.Count,
            Rationale = "office-yellow",
            Source = "office-yellow",
        };
    }
}