#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Local field detection from OCR lines and optional value hints. Used when AI is off and as seeds for Azure.
/// </summary>
public static class DeterministicScanFieldPlanner
{
    private static readonly Regex DateLike = new(
        @"\b(\d{1,2}[./-]\d{1,2}[./-]\d{2,4}|\d{4}[./-]\d{1,2}[./-]\d{1,2})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static ScanFieldPlanProposal Build(ScanFieldPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = new List<ScanDetectedFieldDraft>();
        var gaps = new List<ScanGapDraft>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var row = 0;

        foreach (var line in request.OcrLines)
        {
            var text = line.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var match = FindBestCatalogMatch(text, request.PlaceholderSet.Allowed);
            if (match != null && usedCodes.Add(match.ShortCode))
            {
                fields.Add(CreateField(match, text, line.PageIndex, row++, ScanFieldConfidence.Medium));
                continue;
            }

            if (LooksLikeDataValue(text))
            {
                gaps.Add(new ScanGapDraft(
                    Guid.NewGuid().ToString("N"),
                    text,
                    SuggestPropertyName(text)));
            }
        }

        foreach (var hint in request.ValueHints)
        {
            if (!TemplateTokenSyntax.TryGetShortCode(hint.Token, out var code))
                continue;

            if (!request.PlaceholderSet.Contains(code) || !usedCodes.Add(code))
                continue;

            var entry = request.PlaceholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));

            fields.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                Box = BandBox(row++),
                PageIndex = request.Pages.FirstOrDefault()?.PageIndex ?? 0,
                LabelText = hint.LabelText ?? entry.LabelEn,
                ProposedToken = entry.BuildWordToken(
                    entry.Scope == UserReportPlaceholderScope.Row
                        ? UserReportPlaceholderScope.Row
                        : UserReportPlaceholderScope.Header),
                Confidence = ScanFieldConfidence.High,
                Scope = entry.Scope == UserReportPlaceholderScope.Row ? ScanFieldScope.Row : ScanFieldScope.Header,
            });
        }

        return new ScanFieldPlanProposal
        {
            Fields = fields,
            Gaps = gaps,
            Rationale = fields.Count > 0
                ? "Deterministic OCR label matching and value hints."
                : "No OCR lines matched the profile placeholder catalog.",
            Source = "deterministic",
        };
    }

    private static ScanDetectedFieldDraft CreateField(
        UserReportPlaceholderCatalogEntry entry,
        string labelText,
        int pageIndex,
        int row,
        ScanFieldConfidence confidence) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = BandBox(row),
            PageIndex = pageIndex,
            LabelText = labelText,
            ProposedToken = entry.BuildWordToken(
                entry.Scope == UserReportPlaceholderScope.Row
                    ? UserReportPlaceholderScope.Row
                    : UserReportPlaceholderScope.Header),
            Confidence = confidence,
            Scope = entry.Scope == UserReportPlaceholderScope.Row ? ScanFieldScope.Row : ScanFieldScope.Header,
        };

    private static ScanBoundingBox BandBox(int row)
    {
        var top = 0.05 + row * 0.06;
        return new ScanBoundingBox(0.05, top, 0.95, Math.Min(0.98, top + 0.05)).Clamp();
    }

    internal static UserReportPlaceholderCatalogEntry? FindBestCatalogMatch(
        string lineText,
        IReadOnlyList<UserReportPlaceholderCatalogEntry> allowed)
    {
        var normalizedLine = NormalizeLabel(lineText);
        if (normalizedLine.Length < 3)
            return null;

        UserReportPlaceholderCatalogEntry? best = null;
        var bestScore = 0;

        foreach (var entry in allowed)
        {
            var label = NormalizeLabel(entry.LabelEn);
            if (label.Length < 3)
                continue;

            var score = ScoreMatch(normalizedLine, label);
            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return bestScore >= 60 ? best : null;
    }

    internal static int ScoreMatch(string line, string label)
    {
        if (line.Contains(label, StringComparison.Ordinal) || label.Contains(line, StringComparison.Ordinal))
            return 80;

        var lineTokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var labelTokens = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lineTokens.Length == 0 || labelTokens.Length == 0)
            return 0;

        var overlap = lineTokens.Count(t => labelTokens.Contains(t, StringComparer.Ordinal));
        return (overlap * 100) / Math.Max(labelTokens.Length, lineTokens.Length);
    }

    internal static string NormalizeLabel(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    internal static bool LooksLikeDataValue(string text)
    {
        if (DateLike.IsMatch(text))
            return true;

        var digits = text.Count(char.IsDigit);
        return digits >= 4 && digits >= text.Length / 2;
    }

    private static string? SuggestPropertyName(string text)
    {
        if (DateLike.IsMatch(text))
            return "DetectedDate";

        if (text.Any(char.IsDigit))
            return "DetectedNumber";

        return "DetectedText";
    }
}
