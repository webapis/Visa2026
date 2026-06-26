using System.Globalization;
using System.Text.RegularExpressions;

namespace Visa2026.Module.Services;

/// <summary>
/// Parses VISA2015 <c>dbo.MaritalStatus.StatusL</c> family narrative into
/// <see cref="VisaFamilyMemberLineDto"/> rows for <see cref="Person.VisaApplicationFamilyMembersText"/>.
/// </summary>
public static class LegacyMaritalStatusLParser
{
    private static readonly Regex CanonicalLinePattern = new(
        @";\s*\d{2}\.\d{2}\.\d{4}\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LegacyMarkerPattern = new(
        @"(?i)\b(aýaly|ayaly|ogly|gyzy|çaga)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RelationshipSegmentPattern = new(
        @"(?i)\b(aýaly|ayaly|ogly|gyzy|çaga)\b\s*[-–]?\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DatePattern = new(
        @"(?<!\d)\(?(\d{2}\.\d{2}\.\d{4})\)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TrailingCountryPattern = new(
        @"(?:\(\s*([A-Z]{3})\s*\)|\b([A-Z]{3}))\s*\.?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex InlineCountryPattern = new(
        @"\(\s*([A-Z]{3})\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MaritalPrefixPattern = new(
        @"^(?i)\s*(?:öýlenen|oylenen|öýl\.?|married)\s*,?\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool LooksLikeLegacyStatusL(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (VisaFamilyMemberLinesHelper.IsNoneValue(trimmed))
        {
            return false;
        }

        if (CanonicalLinePattern.IsMatch(trimmed))
        {
            return false;
        }

        return LegacyMarkerPattern.IsMatch(trimmed) && DatePattern.IsMatch(trimmed);
    }

    public static IReadOnlyList<VisaFamilyMemberLineDto> Parse(string? statusL)
    {
        if (string.IsNullOrWhiteSpace(statusL))
        {
            return Array.Empty<VisaFamilyMemberLineDto>();
        }

        var text = NormalizeLegacyText(statusL);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<VisaFamilyMemberLineDto>();
        }

        var defaultCountry = ExtractTrailingCountry(ref text);
        var matches = RelationshipSegmentPattern.Matches(text);
        if (matches.Count == 0)
        {
            return Array.Empty<VisaFamilyMemberLineDto>();
        }

        var rows = new List<VisaFamilyMemberLineDto>(matches.Count);
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var relationship = NormalizeRelationship(match.Groups[1].Value);
            var start = match.Index + match.Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var segment = text[start..end].Trim().Trim(',').Trim();
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            var row = ParseSegment(segment, relationship, defaultCountry);
            if (row != null)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    public static string? ToStorageText(string? statusL)
    {
        if (!LooksLikeLegacyStatusL(statusL))
        {
            return null;
        }

        var rows = Parse(statusL);
        return rows.Count == 0 ? null : VisaFamilyMemberLinesHelper.Format(rows);
    }

    private static string NormalizeLegacyText(string statusL)
    {
        var text = statusL.Trim();
        text = text.Replace("ý.d.", string.Empty, StringComparison.OrdinalIgnoreCase);
        text = text.Replace("ý.", string.Empty, StringComparison.OrdinalIgnoreCase);
        text = MaritalPrefixPattern.Replace(text, string.Empty);
        return text.Trim().Trim(',').Trim();
    }

    private static string? ExtractTrailingCountry(ref string text)
    {
        var match = TrailingCountryPattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var code = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();
        text = text[..match.Index].Trim().Trim(',').Trim();
        return string.IsNullOrEmpty(code) ? null : code;
    }

    private static VisaFamilyMemberLineDto? ParseSegment(string segment, string relationship, string? defaultCountry)
    {
        var dateMatch = DatePattern.Match(segment);
        if (!dateMatch.Success)
        {
            return null;
        }

        if (!DateTime.TryParseExact(
                dateMatch.Groups[1].Value,
                VisaFamilyMemberLinesHelper.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var birthDate))
        {
            return null;
        }

        var nameEnd = dateMatch.Index;
        var fullName = VisaFamilyMemberLinesHelper.SanitizeFamilyMemberFullName(
            segment[..nameEnd].Trim().Trim(',').Trim('-', '–'));
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var country = ExtractInlineCountry(segment) ?? defaultCountry ?? string.Empty;
        var incomplete = string.IsNullOrWhiteSpace(country);

        return new VisaFamilyMemberLineDto
        {
            FullName = fullName,
            BirthDate = birthDate,
            RelationshipNameTm = relationship,
            CountryCode = country,
            IsLegacyIncomplete = incomplete,
        };
    }

    private static string? ExtractInlineCountry(string segment)
    {
        var matches = InlineCountryPattern.Matches(segment);
        if (matches.Count == 0)
        {
            return null;
        }

        return matches[^1].Groups[1].Value.Trim();
    }

    private static string NormalizeRelationship(string raw)
    {
        if (string.Equals(raw, "ayaly", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "aýaly", StringComparison.OrdinalIgnoreCase))
        {
            return "aýaly";
        }

        if (string.Equals(raw, "ogly", StringComparison.OrdinalIgnoreCase))
        {
            return "ogly";
        }

        if (string.Equals(raw, "gyzy", StringComparison.OrdinalIgnoreCase))
        {
            return "gyzy";
        }

        if (string.Equals(raw, "çaga", StringComparison.OrdinalIgnoreCase))
        {
            return "çaga";
        }

        return raw.Trim().ToLowerInvariant();
    }
}
