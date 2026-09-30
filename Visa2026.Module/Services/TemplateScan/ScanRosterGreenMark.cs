#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Green highlighter is the people list inside a header letter.
/// A numbered green line (<c>1. Hayati Uyan</c>) becomes <c>{{.RNUM}}. {{.PFN}}</c>.
/// Other green marks stay on the column or guessed code, written as a row token.
/// </summary>
public static class ScanRosterGreenMark
{
    private static readonly Regex NumberedPerson = new(
        @"^\s*\d{1,3}\s*[\.\)]\s+(.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HeaderToken = new(
        @"\{\{ds\.([A-Za-z0-9_]+)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<ScanDetectedFieldDraft> PreferRoster(
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        ScanOfficeYellowSpan yellow,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string> usedHeaderCodes)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        ArgumentNullException.ThrowIfNull(yellow);
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(usedHeaderCodes);

        if (yellow.MarkKind != ScanOfficeMarkKind.Green)
            return drafts;
        if (drafts.Any(static d => d.IsLocked))
            return drafts;

        var columnHeader = drafts.Select(static d => d.ColumnHeader).FirstOrDefault(static h => !string.IsNullOrWhiteSpace(h));
        if (TryPersonLine(yellow, columnHeader, placeholderSet, out var person))
        {
            foreach (var draft in drafts)
            {
                foreach (var code in TemplateTokenSyntax.GetShortCodes(draft.ProposedToken))
                    usedHeaderCodes.Remove(code);
            }

            return [person];
        }

        if (drafts.Count == 0)
            return drafts;

        return drafts.Select(d => RewriteToRow(d, placeholderSet)).ToList();
    }

    private static bool TryPersonLine(
        ScanOfficeYellowSpan yellow,
        string? columnHeader,
        ApplicationProfilePlaceholderSet placeholderSet,
        out ScanDetectedFieldDraft draft)
    {
        draft = null!;
        if (!string.IsNullOrWhiteSpace(columnHeader))
            return false;

        var text = yellow.Text?.Trim() ?? string.Empty;
        var numbered = NumberedPerson.Match(text);
        var name = numbered.Success ? numbered.Groups[1].Value.Trim() : text;
        if (!ScanShapeTokenMatcher.LooksLikePersonFullName(name))
            return false;
        if (!placeholderSet.Contains("PFN"))
            return false;

        var person = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, "PFN", StringComparison.OrdinalIgnoreCase));
        var token = person.BuildWordToken(UserReportPlaceholderScope.Row);
        if (numbered.Success && placeholderSet.Contains("RNUM"))
        {
            var rowNumber = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, "RNUM", StringComparison.OrdinalIgnoreCase));
            token = rowNumber.BuildWordToken(UserReportPlaceholderScope.Row) + ". " + token;
        }

        draft = new ScanDetectedFieldDraft
        {
            FieldId = Guid.NewGuid().ToString("N"),
            PageIndex = yellow.PageIndex,
            LabelText = text,
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            Box = ScanBoundingBox.FullPage,
            SourceRegion = yellow.Region,
        };
        return true;
    }

    private static ScanDetectedFieldDraft RewriteToRow(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        if (draft.IsLocked || string.IsNullOrWhiteSpace(draft.ProposedToken))
        {
            return draft.Scope == ScanFieldScope.Row
                ? draft
                : Copy(draft, draft.ProposedToken, ScanFieldScope.Row);
        }

        var token = HeaderToken.Replace(draft.ProposedToken, match =>
        {
            var code = match.Groups[1].Value;
            var entry = placeholderSet.Allowed.FirstOrDefault(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
            if (entry == null || entry.Scope == UserReportPlaceholderScope.Header)
                return match.Value;

            return entry.BuildWordToken(UserReportPlaceholderScope.Row);
        });

        var scope = token.Contains("{{.", StringComparison.Ordinal)
            ? ScanFieldScope.Row
            : draft.Scope;
        if (string.Equals(token, draft.ProposedToken, StringComparison.Ordinal) && scope == draft.Scope)
            return draft;

        return Copy(draft, token, scope);
    }

    private static ScanDetectedFieldDraft Copy(
        ScanDetectedFieldDraft draft,
        string? token,
        ScanFieldScope scope) =>
        new()
        {
            FieldId = draft.FieldId,
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = token,
            Confidence = draft.Confidence,
            Scope = scope,
            Box = draft.Box,
            SourceRegion = draft.SourceRegion,
            ColumnHeader = draft.ColumnHeader,
            NearbyLabel = draft.NearbyLabel,
            Alternatives = draft.Alternatives,
            IsLocked = draft.IsLocked,
        };
}
