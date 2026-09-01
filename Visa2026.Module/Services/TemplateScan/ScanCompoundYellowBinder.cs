#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// When one yellow highlight contains a comma, it is a combination candidate.
/// Left-side labels and the parenthetical caption under the line guide each part.
/// </summary>
public static class ScanCompoundYellowBinder
{
    public static IReadOnlyList<ScanDetectedFieldDraft> Upgrade(
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        if (drafts.Count != 1)
            return drafts;

        var draft = drafts[0];
        if (TemplateTokenSyntax.GetShortCodes(draft.ProposedToken).Count > 1)
            return drafts;

        var usage = draft.Scope == ScanFieldScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;
        var bound = TryBind(draft.LabelText, placeholderSet, usage, draft.NearbyLabel, draft.ColumnHeader);
        if (bound == null)
            return drafts;

        return
        [
            new ScanDetectedFieldDraft
            {
                FieldId = draft.FieldId,
                PageIndex = draft.PageIndex,
                LabelText = draft.LabelText,
                ProposedToken = bound.Value.Token,
                Confidence = ScanFieldConfidence.High,
                Scope = draft.Scope,
                Box = draft.Box,
                SourceRegion = draft.SourceRegion,
                Alternatives = MergeAlternatives(draft.Alternatives, bound.Value.Alternatives),
                ColumnHeader = draft.ColumnHeader,
                NearbyLabel = draft.NearbyLabel,
            },
        ];
    }

    internal static (string Token, IReadOnlyList<ScanTokenAlternative> Alternatives)? TryBind(
        string? labelText,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderScope usage,
        string? nearbyLabel = null,
        string? columnHeader = null)
    {
        if (!ScanCompoundYellowParts.IsCommaCombination(labelText))
            return null;

        var segments = ScanCompoundYellowParts.SplitSegments(labelText ?? string.Empty);
        if (segments.Count < 2)
            return null;

        var nearby = string.Join(" ", new[] { nearbyLabel, columnHeader }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        var role = ScanLetterRoleHint.FromNearbyText(nearby);
        var slots = ScanFormCaptionHints.Slots(nearby);

        // Isolated I-AŞ + phone (no form caption) is one RPCL token. Caption slots still split.
        if (slots.Count == 0)
        {
            var fullShape = ScanShapeTokenMatcher.ScoreSnippet(
                labelText ?? string.Empty,
                placeholderSet,
                usage);
            if (fullShape.Any(static c =>
                    c.ShortCode.Equals("RPCL", StringComparison.OrdinalIgnoreCase)
                    && c.ScorePercent >= 80))
                return null;
        }

        var catalog = ScanPlaceholderCatalogIndex.Build(placeholderSet);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codes = new string?[segments.Count];

        if (slots.Count == segments.Count)
        {
            for (var i = 0; i < segments.Count; i++)
                codes[i] = PickCode(segments[i].Text, slots[i], placeholderSet, catalog, usage, used, role, nearby);
        }
        else
        {
            for (var i = 0; i < segments.Count; i++)
            {
                var slot = i < slots.Count ? slots[i] : null;
                codes[i] = PickCode(segments[i].Text, slot, placeholderSet, catalog, usage, used, role, nearby);
            }
        }

        var bound = codes.Where(static c => !string.IsNullOrWhiteSpace(c)).Select(static c => c!).ToList();
        if (bound.Count < 2 || bound.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
            return null;

        var tokens = new List<string>();
        var alternatives = new List<ScanTokenAlternative>();
        foreach (var code in bound)
        {
            if (!placeholderSet.Contains(code))
                return null;
            var entry = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
            var token = entry.BuildWordToken(
                entry.Scope == UserReportPlaceholderScope.Row
                    ? UserReportPlaceholderScope.Row
                    : usage);
            tokens.Add(token);
            alternatives.Add(new ScanTokenAlternative(token, code, 88, "Form caption + comma segment"));
        }

        return (ScanFieldPlanOfficerOverride.JoinLibraryTokens(labelText ?? string.Empty, null, tokens), alternatives);
    }

    private static string? PickCode(
        string segment,
        string? slot,
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanPlaceholderCatalogIndex catalog,
        UserReportPlaceholderScope usage,
        HashSet<string> used,
        ScanLetterRole role,
        string nearby)
    {
        foreach (var preferred in ScanFormCaptionHints.PreferCodes(slot, role, nearby))
        {
            var remapped = ScanFormCaptionHints.RemapByRole(preferred, role);
            if (ScanCompoundYellowParts.SegmentFitsCode(segment, remapped)
                && TryTake(placeholderSet, used, remapped))
                return remapped;
        }

        var headerHits = catalog.ScoreHeader(slot);
        foreach (var (entry, score) in headerHits)
        {
            if (score < 55)
                continue;
            var remapped = ScanFormCaptionHints.RemapByRole(entry.ShortCode, role);
            if (ScanCompoundYellowParts.SegmentFitsCode(segment, remapped)
                && TryTake(placeholderSet, used, remapped))
                return remapped;
        }

        if (ScanCompoundYellowParts.LooksLikePassportNumber(segment)
            && TryTake(placeholderSet, used, ScanFormCaptionHints.RemapByRole("PPN", role)))
            return ScanFormCaptionHints.RemapByRole("PPN", role);

        if (ScanCompoundYellowParts.PhoneShape.IsMatch(segment)
            && TryTake(placeholderSet, used, role == ScanLetterRole.Wekil ? "RPPH" : "ACPHN"))
            return role == ScanLetterRole.Wekil ? "RPPH" : "ACPHN";

        if (ScanCompoundYellowParts.DateLikeShape.IsMatch(segment))
        {
            var nearbyFolded = TemplateTextNormalizer.NormalizeFolded(nearby);
            var dateCodes = nearbyFolded.Contains("hasaba", StringComparison.Ordinal)
                ? new[] { "ACRDT", "PPED", "PDBT", "ADAT" }
                : role == ScanLetterRole.Wekil
                    ? new[] { "RPPD", "PPED", "ACRDT" }
                    : new[] { "PPED", "PDBT", "ACRDT", "ADAT" };
            foreach (var dateCode in dateCodes)
            {
                if (TryTake(placeholderSet, used, dateCode))
                    return dateCode;
            }
        }

        if (role != ScanLetterRole.Wekil
            && role != ScanLetterRole.Signatory
            && TemplateTextNormalizer.NormalizeFolded(nearby).Contains("pasport", StringComparison.Ordinal)
            && TryTake(placeholderSet, used, "PPAT"))
            return "PPAT";

        if (role == ScanLetterRole.Wekil
            && TryTake(placeholderSet, used, "RPPA"))
            return "RPPA";

        var ranked = ScanShapeTokenMatcher.ScoreSnippet(segment, placeholderSet, usage);
        var pick = ranked.FirstOrDefault(c =>
            c.ScorePercent >= 55 && used.Add(ScanFormCaptionHints.RemapByRole(c.ShortCode, role)));
        return pick == null ? null : ScanFormCaptionHints.RemapByRole(pick.ShortCode, role);
    }

    private static bool TryTake(
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string> used,
        string code) =>
        placeholderSet.Contains(code) && used.Add(code);

    private static IReadOnlyList<ScanTokenAlternative> MergeAlternatives(
        IReadOnlyList<ScanTokenAlternative> existing,
        IReadOnlyList<ScanTokenAlternative> extra)
    {
        var list = extra.ToList();
        foreach (var item in existing)
        {
            if (!list.Any(a => string.Equals(a.ShortCode, item.ShortCode, StringComparison.OrdinalIgnoreCase)))
                list.Add(item);
        }

        return list.Take(6).ToList();
    }
}
