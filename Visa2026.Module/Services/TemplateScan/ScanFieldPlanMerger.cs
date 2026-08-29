#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanFieldPlanMerger
{
    ScanFieldPlan Merge(ScanFieldPlanMergeRequest request);
}

public sealed class ScanFieldPlanMerger : IScanFieldPlanMerger
{
    public ScanFieldPlan Merge(ScanFieldPlanMergeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Proposal);
        ArgumentNullException.ThrowIfNull(request.PlaceholderSet);

        var allowed = request.PlaceholderSet;
        var hintTokens = (request.ValueHints ?? Array.Empty<ScanValueHint>())
            .Select(static h => h.Token)
            .Where(static t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fields = new List<ScanDetectedField>();
        var gaps = new List<ScanGap>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var draft in request.Proposal.Fields)
            AbsorbDraft(draft, allowed, hintTokens, usedCodes, fields, gaps);

        foreach (var gap in request.Proposal.Gaps)
            AbsorbUnmappedLabel(
                gap.FieldId,
                gap.LabelText,
                gap.SuggestedPropertyName,
                ScanBoundingBox.FullPage,
                pageIndex: 0,
                allowed,
                hintTokens,
                usedCodes,
                fields,
                gaps);

        var staticRegions = request.Proposal.StaticRegions
            .Select(r => new ScanStaticRegion
            {
                RegionId = r.RegionId,
                PageIndex = r.PageIndex,
                Box = r.Box.Clamp(),
                TextPreview = r.TextPreview,
            })
            .ToList();

        return new ScanFieldPlan
        {
            PlaceholderSet = allowed,
            ScanKind = request.ScanKind,
            Fields = fields,
            StaticRegions = staticRegions,
            Gaps = gaps,
            PendingQuestions = request.Proposal.PendingQuestions,
            Rationale = request.Proposal.Rationale,
            Source = request.Proposal.Source,
            YellowHighlightCount = request.Proposal.YellowHighlightCount,
        };
    }

    private static void AbsorbDraft(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet allowed,
        HashSet<string> hintTokens,
        HashSet<string> usedCodes,
        List<ScanDetectedField> fields,
        List<ScanGap> gaps)
    {
        // Prefer local split so "№ … + date" and count/period compounds become separate tokens.
        var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            draft.LabelText,
            draft.Box,
            draft.PageIndex,
            allowed,
            usedCodes);

        foreach (var r in resolved)
            fields.Add(ToField(r, hintTokens));

        if (resolved.Count > 0)
            return;

        if (!string.IsNullOrWhiteSpace(draft.ProposedToken)
            && TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var shortCode)
            && allowed.Contains(shortCode)
            && usedCodes.Add(shortCode))
        {
            fields.Add(ToField(draft, hintTokens));
            return;
        }

        AbsorbUnmappedLabel(
            draft.FieldId,
            draft.LabelText,
            suggested: null,
            draft.Box,
            draft.PageIndex,
            allowed,
            hintTokens,
            usedCodes,
            fields,
            gaps);
    }

    private static void AbsorbUnmappedLabel(
        string fieldId,
        string labelText,
        string? suggested,
        ScanBoundingBox box,
        int pageIndex,
        ApplicationProfilePlaceholderSet allowed,
        HashSet<string> hintTokens,
        HashSet<string> usedCodes,
        List<ScanDetectedField> fields,
        List<ScanGap> gaps)
    {
        var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            labelText,
            box,
            pageIndex,
            allowed,
            usedCodes);

        if (resolved.Count > 0)
        {
            foreach (var r in resolved)
                fields.Add(ToField(r, hintTokens));
            return;
        }

        if (ScanYellowHighlightTokenResolver.IsYellowTextFullyMapped(labelText, allowed, usedCodes))
            return;

        gaps.Add(new ScanGap(fieldId, labelText, suggested));
    }

    private static ScanDetectedField ToField(ScanDetectedFieldDraft draft, HashSet<string> hintTokens)
    {
        var confidence = draft.Confidence;
        if (!string.IsNullOrWhiteSpace(draft.ProposedToken) && hintTokens.Contains(draft.ProposedToken))
            confidence = ScanFieldConfidence.High;

        return new ScanDetectedField
        {
            FieldId = draft.FieldId,
            Box = draft.Box.Clamp(),
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = draft.ProposedToken!.Trim(),
            Confidence = confidence,
            Scope = draft.Scope,
        };
    }
}