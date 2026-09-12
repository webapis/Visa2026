#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

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
                pageIndex: gap.PageIndex,
                allowed,
                hintTokens,
                usedCodes,
                fields,
                gaps,
                nearbyLabel: null,
                sourceRegion: gap.SourceRegion);

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
        if (draft.IsLocked)
        {
            RegisterUsedHeaderCode(draft, allowed, usedCodes);
            fields.Add(ToField(draft, hintTokens, allowed));
            return;
        }

        // Excel inference / single-token drafts — do not re-run regex (dates become ADAT).
        if (ShouldKeepDraftToken(draft, allowed, usedCodes))
        {
            RegisterUsedHeaderCode(draft, allowed, usedCodes);
            fields.Add(ToField(draft, hintTokens, allowed));
            return;
        }

        // Compound yellow text — split number+date, count+period, etc.
        var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            draft.LabelText,
            draft.Box,
            draft.PageIndex,
            allowed,
            usedCodes,
            draft.SourceRegion,
            draft.NearbyLabel);

        foreach (var r in resolved)
            fields.Add(ToField(r, hintTokens, allowed));

        if (resolved.Count > 0)
            return;

        if (!string.IsNullOrWhiteSpace(draft.ProposedToken)
            && TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var shortCode)
            && allowed.Contains(shortCode)
            && usedCodes.Add(shortCode))
        {
            fields.Add(ToField(draft, hintTokens, allowed));
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
            gaps,
            draft.NearbyLabel,
            draft.SourceRegion);
    }

    private static bool ShouldKeepDraftToken(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet allowed,
        HashSet<string> usedCodes)
    {
        if (string.IsNullOrWhiteSpace(draft.ProposedToken))
            return false;

        var token = draft.ProposedToken.Trim();
        var secondBrace = token.IndexOf("{{", 2, StringComparison.Ordinal);
        if (secondBrace >= 0)
            return true;

        if (draft.Alternatives.Any(static a =>
                a.Reason.Contains("Column", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.ShortCode, "COMPOUND", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!TemplateTokenSyntax.TryGetShortCode(token, out var shortCode) || !allowed.Contains(shortCode))
            return false;

        // Office Analyze already bound a write address — do not re-split (dates become ADAT, regions drop).
        if (draft.SourceRegion != null)
            return true;

        var entry = allowed.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));
        return entry?.Scope == UserReportPlaceholderScope.Row
            || draft.Scope == ScanFieldScope.Row;
    }

    private static void RegisterUsedHeaderCode(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet allowed,
        HashSet<string> usedCodes)
    {
        if (!TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var shortCode)
            || !allowed.Contains(shortCode))
            return;

        var entry = allowed.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));
        if (entry?.Scope == UserReportPlaceholderScope.Row || draft.Scope == ScanFieldScope.Row)
            return;

        usedCodes.Add(shortCode);
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
        List<ScanGap> gaps,
        string? nearbyLabel = null,
        DocumentRegion? sourceRegion = null)
    {
        var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            labelText,
            box,
            pageIndex,
            allowed,
            usedCodes,
            nearbyLabel: nearbyLabel);

        if (resolved.Count > 0)
        {
            foreach (var r in resolved)
                fields.Add(ToField(r, hintTokens, allowed));
            return;
        }

        if (ScanYellowHighlightTokenResolver.IsYellowTextFullyMapped(labelText, allowed, usedCodes, nearbyLabel))
            return;

        var key = TemplateTextNormalizer.NormalizeIdentifier(labelText);
        if (key.Length >= TemplateTextNormalizer.MinimumMatchLength)
        {
            var prior = fields.FirstOrDefault(f =>
                !string.IsNullOrWhiteSpace(f.ProposedToken)
                && string.Equals(
                    TemplateTextNormalizer.NormalizeIdentifier(f.LabelText),
                    key,
                    StringComparison.Ordinal));
            if (prior != null && !IsPersonCountCloneBlockedByVisaCancel(nearbyLabel, prior.ProposedToken))
            {
                fields.Add(new ScanDetectedField
                {
                    FieldId = fieldId,
                    Box = box.Clamp(),
                    PageIndex = pageIndex,
                    LabelText = labelText,
                    ProposedToken = prior.ProposedToken,
                    Confidence = prior.Confidence,
                    Scope = prior.Scope,
                    SourceRegion = prior.SourceRegion,
                    Alternatives = prior.Alternatives,
                });
                return;
            }
        }

        if (sourceRegion != null)
        {
            fields.Add(new ScanDetectedField
            {
                FieldId = fieldId,
                Box = box.Clamp(),
                PageIndex = pageIndex,
                LabelText = labelText,
                ProposedToken = null,
                Confidence = ScanFieldConfidence.Low,
                Scope = ScanFieldScope.Header,
                SourceRegion = sourceRegion,
            });
            return;
        }

        gaps.Add(new ScanGap(fieldId, labelText, suggested, sourceRegion, pageIndex));
    }

    private static bool IsPersonCountCloneBlockedByVisaCancel(string? nearbyLabel, string? priorToken)
    {
        if (!ScanOfficialLetterHints.PrefersDocumentCancelCount(nearbyLabel))
            return false;
        if (!TemplateTokenSyntax.TryGetShortCode(priorToken ?? string.Empty, out var priorCode))
            return false;

        return string.Equals(priorCode, "TPCNT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(priorCode, "TPCTX", StringComparison.OrdinalIgnoreCase);
    }

    private static ScanDetectedField ToField(
        ScanDetectedFieldDraft draft,
        HashSet<string> hintTokens,
        ApplicationProfilePlaceholderSet allowed)
    {
        var confidence = draft.Confidence;
        string? token = null;
        if (!string.IsNullOrWhiteSpace(draft.ProposedToken))
        {
            var original = draft.ProposedToken.Trim();
            if (hintTokens.Contains(original))
                confidence = ScanFieldConfidence.High;
            token = ScanLibraryTokenRewriter.Rewrite(original, allowed);
        }

        return new ScanDetectedField
        {
            FieldId = draft.FieldId,
            Box = draft.Box.Clamp(),
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = token,
            Confidence = confidence,
            Scope = draft.Scope,
            SourceRegion = draft.SourceRegion,
            Alternatives = draft.Alternatives,
            IsLocked = draft.IsLocked,
        };
    }
}