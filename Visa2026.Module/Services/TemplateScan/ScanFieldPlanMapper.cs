#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

internal static class ScanFieldPlanMapper
{
    internal static ScanFieldPlanProposal ToProposal(ScanFieldPlan plan, string source)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new ScanFieldPlanProposal
        {
            Fields = plan.Fields.Select(static f => new ScanDetectedFieldDraft
            {
                FieldId = f.FieldId,
                Box = f.Box,
                PageIndex = f.PageIndex,
                LabelText = f.LabelText,
                ProposedToken = f.ProposedToken,
                Confidence = f.Confidence,
                Scope = f.Scope,
            }).ToList(),
            StaticRegions = plan.StaticRegions.Select(static r => new ScanStaticRegionDraft
            {
                RegionId = r.RegionId,
                PageIndex = r.PageIndex,
                Box = r.Box,
                TextPreview = r.TextPreview,
            }).ToList(),
            Gaps = plan.Gaps.Select(static g => new ScanGapDraft(g.FieldId, g.LabelText, g.SuggestedPropertyName)).ToList(),
            PendingQuestions = plan.PendingQuestions,
            Rationale = plan.Rationale,
            Source = source,
        };
    }
}
