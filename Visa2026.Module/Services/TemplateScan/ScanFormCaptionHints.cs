#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Borçnama-style form captions: left-side labels and the parenthetical list under the yellow line
/// describe comma-separated combination parts.
/// </summary>
public static class ScanFormCaptionHints
{
    private static readonly Regex Parenthetical = new(
        @"\(([^)]{8,})\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? ExtractParentheticalList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        Match? best = null;
        foreach (Match match in Parenthetical.Matches(text))
        {
            if (!match.Groups[1].Value.Contains(',', StringComparison.Ordinal))
                continue;
            if (best == null || match.Groups[1].Value.Length > best.Groups[1].Value.Length)
                best = match;
        }

        return best == null ? null : "(" + best.Groups[1].Value.Trim() + ")";
    }

    public static IReadOnlyList<string> Slots(string? nearbyLabel)
    {
        var list = ExtractParentheticalList(nearbyLabel);
        if (string.IsNullOrWhiteSpace(list))
            return Array.Empty<string>();

        var inner = list.Trim();
        if (inner.StartsWith('(') && inner.EndsWith(')') && inner.Length >= 2)
            inner = inner[1..^1];

        return inner
            .Split(',')
            .Select(static s => s.Trim())
            .Where(static s => s.Length > 0)
            .ToList();
    }

    public static IReadOnlyList<string> PreferCodes(
        string? slot,
        ScanLetterRole role,
        string? nearbyLabel)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(slot);
        var nearby = TemplateTextNormalizer.NormalizeFolded(nearbyLabel);
        if (folded.Length == 0)
            return Array.Empty<string>();

        if (folded.Contains("telefon", StringComparison.Ordinal))
            return role == ScanLetterRole.Wekil ? ["RPPH"] : ["ACPHN", "RPPH"];

        if (folded.Contains("doglan", StringComparison.Ordinal))
            return ["PDBT"];

        if (folded.Contains("familiya", StringComparison.Ordinal)
            || folded.Contains("atasynyn ady", StringComparison.Ordinal)
            || folded.Contains("doly ady", StringComparison.Ordinal)
            || folded.Equals("ady", StringComparison.Ordinal))
            return NameCodes(role);

        if (folded.Contains("mohlet", StringComparison.Ordinal))
            return role switch
            {
                ScanLetterRole.Signatory => ["CHPE"],
                ScanLetterRole.Wekil => ["RPPD"],
                _ => ["PPED"],
            };

        if (folded.Contains("nirede", StringComparison.Ordinal)
            || folded.Contains("berildi", StringComparison.Ordinal)
            || folded.Contains("edara", StringComparison.Ordinal)
            || folded.Contains("hakim", StringComparison.Ordinal))
            return AuthorityCodes(role);

        if (folded.Contains("pasport", StringComparison.Ordinal)
            && (folded.Contains("belgi", StringComparison.Ordinal)
                || folded.Contains("seriya", StringComparison.Ordinal)))
            return PassportNumberCodes(role);

        if ((folded.Contains("hasaba", StringComparison.Ordinal) && folded.Contains("belgi", StringComparison.Ordinal))
            || folded.Contains("alnan belgi", StringComparison.Ordinal))
            return ["ACTAX"];

        if (folded.Contains("senesi", StringComparison.Ordinal)
            || (folded.Equals("senesi", StringComparison.Ordinal)))
        {
            if (nearby.Contains("hasaba", StringComparison.Ordinal)
                || nearby.Contains("karhana", StringComparison.Ordinal)
                || nearby.Contains("yuridiki", StringComparison.Ordinal))
                return ["ACRDT"];
            if (nearby.Contains("doglan", StringComparison.Ordinal)
                || nearby.Contains("cagryl", StringComparison.Ordinal)
                || role == ScanLetterRole.Applicant)
                return ["PDBT"];
            return role == ScanLetterRole.Applicant ? ["PDBT"] : ["ACRDT", "PDBT"];
        }

        if (folded.Contains("salgy", StringComparison.Ordinal)
            || folded.Contains("yuridiki", StringComparison.Ordinal)
            || folded.Contains("adres", StringComparison.Ordinal))
            return ["ACADR"];

        return Array.Empty<string>();
    }

    public static string RemapByRole(string shortCode, ScanLetterRole role) =>
        role switch
        {
            ScanLetterRole.Wekil => shortCode.ToUpperInvariant() switch
            {
                "PPN" => "RPPN",
                "PPAT" => "RPPA",
                "PPED" => "RPPD",
                "ACPHN" => "RPPH",
                "PFN" => "RPFN",
                _ => shortCode,
            },
            ScanLetterRole.Signatory => shortCode.ToUpperInvariant() switch
            {
                "PPN" => "CHPN",
                "PPAT" => "CHPA",
                "PPED" => "CHPE",
                "PFN" => "CHFN",
                _ => shortCode,
            },
            _ => shortCode,
        };

    private static IReadOnlyList<string> NameCodes(ScanLetterRole role) =>
        role switch
        {
            ScanLetterRole.Wekil => ["RPFN"],
            ScanLetterRole.Signatory => ["CHFN"],
            _ => ["PFN"],
        };

    private static IReadOnlyList<string> PassportNumberCodes(ScanLetterRole role) =>
        role switch
        {
            ScanLetterRole.Wekil => ["RPPN"],
            ScanLetterRole.Signatory => ["CHPN"],
            _ => ["PPN"],
        };

    private static IReadOnlyList<string> AuthorityCodes(ScanLetterRole role) =>
        role switch
        {
            ScanLetterRole.Wekil => ["RPPA"],
            ScanLetterRole.Signatory => ["CHPA"],
            _ => ["PPAT"],
        };
}
