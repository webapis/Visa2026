#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Builds a yellow field-plan proposal from Office package marks (no vision).</summary>
public static class ScanOfficeFieldPlanBuilder
{
    public static ScanFieldPlanProposal Build(
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ApplicationProfilePlaceholderSet placeholderSet,
        byte[]? officeBytes = null,
        ScanSourceKind sourceKind = ScanSourceKind.Word)
    {
        ArgumentNullException.ThrowIfNull(yellows);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        if (sourceKind == ScanSourceKind.Excel
            && officeBytes is { Length: > 0 })
        {
            var excelFields = ScanExcelYellowResolver.Resolve(officeBytes, yellows, placeholderSet);
            return new ScanFieldPlanProposal
            {
                Fields = excelFields,
                Gaps = Array.Empty<ScanGapDraft>(),
                YellowHighlightCount = yellows.Count,
                Rationale = "excel-manual-inference",
                Source = "excel-manual-inference",
            };
        }

        var drafts = new List<ScanDetectedFieldDraft>();
        var usedHeaderCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var yellow in yellows)
        {
            var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
                yellow.Text,
                ScanBoundingBox.FullPage,
                yellow.PageIndex,
                placeholderSet,
                usedHeaderCodes,
                yellow.Region);

            if (resolved.Count > 0)
            {
                drafts.AddRange(resolved);
                continue;
            }

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
