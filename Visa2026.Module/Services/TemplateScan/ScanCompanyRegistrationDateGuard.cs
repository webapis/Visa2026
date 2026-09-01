#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Isolated dates next to company registration wording (hasaba alyş belgesi / şahamça / tescil)
/// are <c>ACRDT</c>, not application date <c>ADAT</c>.
/// </summary>
public static class ScanCompanyRegistrationDateGuard
{
    public const string CompanyRegistrationDateCode = "ACRDT";
    public const string ApplicationDateCode = "ADAT";

    private static readonly Regex DateLike = new(
        @"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(?:\s*ý\.?)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IReadOnlyList<ScanDetectedFieldDraft> RewriteDrafts(
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string>? usedHeaderCodes = null)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        ArgumentNullException.ThrowIfNull(placeholderSet);
        if (drafts.Count == 0)
            return drafts;

        var list = new List<ScanDetectedFieldDraft>(drafts.Count);
        foreach (var draft in drafts)
            list.Add(RewriteDraft(draft, placeholderSet, usedHeaderCodes));
        return list;
    }

    public static ScanDetectedFieldDraft RewriteDraft(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string>? usedHeaderCodes = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        if (!placeholderSet.Contains(CompanyRegistrationDateCode))
            return draft;

        if (!LooksLikeDate(draft.LabelText))
            return draft;

        if (!LooksLikeCompanyRegistration(draft.NearbyLabel, draft.ColumnHeader, draft.LabelText))
            return draft;

        if (TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var code)
            && code.Equals(CompanyRegistrationDateCode, StringComparison.OrdinalIgnoreCase))
            return draft;

        if (code != null && code.Equals(ApplicationDateCode, StringComparison.OrdinalIgnoreCase))
            usedHeaderCodes?.Remove(ApplicationDateCode);

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, CompanyRegistrationDateCode, StringComparison.OrdinalIgnoreCase));
        var token = entry.BuildWordToken(UserReportPlaceholderScope.Header);
        usedHeaderCodes?.Add(CompanyRegistrationDateCode);

        return new ScanDetectedFieldDraft
        {
            FieldId = draft.FieldId,
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
            Box = draft.Box,
            SourceRegion = draft.SourceRegion,
            ColumnHeader = draft.ColumnHeader,
            NearbyLabel = draft.NearbyLabel,
            Alternatives =
            [
                new ScanTokenAlternative(
                    token,
                    CompanyRegistrationDateCode,
                    94,
                    "Company registration date — not application date"),
                .. draft.Alternatives,
            ],
        };
    }

    internal static bool LooksLikeCompanyRegistration(params string?[] fragments)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(
            string.Join(" ", fragments.Where(static f => !string.IsNullOrWhiteSpace(f))));
        if (folded.Length == 0)
            return false;

        return folded.Contains("hasaba alys", StringComparison.Ordinal)
            || folded.Contains("sahamca", StringComparison.Ordinal)
            || folded.Contains("tescil", StringComparison.Ordinal)
            || folded.Contains("company registration", StringComparison.Ordinal)
            || (folded.Contains("karhana", StringComparison.Ordinal)
                && folded.Contains("hasaba", StringComparison.Ordinal));
    }

    private static bool LooksLikeDate(string? text) =>
        !string.IsNullOrWhiteSpace(text) && DateLike.IsMatch(text);
}