using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressLineNormalizer
{
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

    public static string StripRegionAndCityPrefixes(
        string? addressLine,
        string? regionDisplayName,
        string? cityDisplayName)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return string.Empty;

        var result = addressLine.Trim();

        string? regionShort = null;
        string? cityShort = null;
        if (!string.IsNullOrWhiteSpace(regionDisplayName))
            regionShort = Regex.Replace(regionDisplayName, @"\s*wela[ýy]aty\s*$", "", RegexOptions.IgnoreCase).Trim();
        if (!string.IsNullOrWhiteSpace(cityDisplayName))
            cityShort = Regex.Replace(cityDisplayName, @"\s*(etraby|şäheri|şäherçesi)\s*$", "", RegexOptions.IgnoreCase).Trim();

        // Wel/etr regex handles glued genitives (welaýatynyň) before catalog prefix match can leave "nyn".
        if (!string.IsNullOrWhiteSpace(regionShort))
            result = StripWelPrefix(result, regionShort);
        if (!string.IsNullOrWhiteSpace(cityShort))
            result = StripEtrapPrefix(result, cityShort);

        result = StripKnownPrefix(result, regionDisplayName);
        result = StripKnownPrefix(result, cityDisplayName);

        if (!string.IsNullOrWhiteSpace(regionShort))
            result = StripKnownPrefix(result, regionShort);
        if (!string.IsNullOrWhiteSpace(cityShort))
            result = StripKnownPrefix(result, cityShort);

        result = StripOrphanAdministrativeFragments(result);
        result = MultiSpace.Replace(result, " ").Trim(' ', ',', '.', ';', ':');
        if (result.Length > 255)
            result = result[..255].TrimEnd();
        return result;
    }

    /// <summary>
    /// Strips region/city prefixes then lodging-specific wel./ş./w, fragments for catalog <see cref="Lodging.FullAddress"/>.
    /// </summary>
    public static string NormalizeLodgingCatalogAddress(
        string? addressLine,
        string? regionDisplayName,
        string? cityDisplayName)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return string.Empty;

        var result = StripRegionAndCityPrefixes(addressLine, regionDisplayName, cityDisplayName);
        result = StripLodgingAdministrativeFragments(result);
        result = StripPartialSaherFragments(result);
        result = StripOrphanAdministrativeFragments(result);

        if (!string.IsNullOrWhiteSpace(cityDisplayName))
        {
            var cityShort = Regex.Replace(cityDisplayName, @"\s*(etraby|şäheri|şäherçesi)\s*$", "", RegexOptions.IgnoreCase).Trim();
            result = StripKnownPrefix(result, cityDisplayName);
            result = StripKnownPrefix(result, cityShort);
        }

        result = StripLodgingAdministrativeFragments(result);
        result = StripOrphanAdministrativeFragments(result);
        result = MultiSpace.Replace(result, " ").Trim(' ', ',', '.', ';', ':', '"');
        if (result.Length > 255)
            result = result[..255].TrimEnd();
        return result;
    }

    /// <summary>
    /// Strips region/city prefixes then hotel-specific ş./şäher/wel. fragments for catalog <see cref="Hotel.Name"/>.
    /// </summary>
    public static string NormalizeHotelCatalogName(
        string? addressLine,
        string? regionDisplayName,
        string? cityDisplayName)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            return string.Empty;

        var result = StripRegionAndCityPrefixes(addressLine, regionDisplayName, cityDisplayName);
        result = StripCitySemicolonPrefix(result, cityDisplayName);
        result = StripHotelAdministrativeFragments(result);
        result = NormalizeHotelFormatting(result);
        result = StripPartialSaherFragments(result);
        result = StripOrphanAdministrativeFragments(result);
        result = RestoreCityWhenGenericHotelSuffix(result, cityDisplayName);
        result = MultiSpace.Replace(result, " ").Trim(' ', ',', '.', ';', ':', '"');
        if (result.Length > 255)
            result = result[..255].TrimEnd();
        return result;
    }

    /// <summary>
    /// Same admin-prefix cleanup as hotels; used for <see cref="Hospital.Name"/>.
    /// </summary>
    public static string NormalizeHospitalCatalogName(
        string? addressLine,
        string? regionDisplayName,
        string? cityDisplayName) =>
        NormalizeHotelCatalogName(addressLine, regionDisplayName, cityDisplayName);

    private static string StripLodgingAdministrativeFragments(string text)
    {
        var result = text.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var (pattern, replacement) in LodgingLeadingReplacements)
            {
                var next = pattern.Replace(result, replacement);
                if (!string.Equals(next, result, StringComparison.Ordinal))
                {
                    result = next.TrimStart();
                    changed = true;
                }
            }
        } while (changed);

        return result;
    }

    private static string StripCitySemicolonPrefix(string text, string? cityDisplayName)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(cityDisplayName))
            return text;

        var semi = text.IndexOf(';');
        if (semi <= 0 || semi >= text.Length - 1)
            return text;

        var left = text[..semi].Trim();
        var cityShort = Regex.Replace(cityDisplayName, @"\s*(etraby|şäheri|şäherçesi)\s*$", "", RegexOptions.IgnoreCase).Trim();
        if (Visa2014CatalogMatchHelper.KeysEqual(left, cityDisplayName)
            || Visa2014CatalogMatchHelper.KeysEqual(left, cityShort))
            return text[(semi + 1)..].Trim();

        return text;
    }

    private static string StripHotelAdministrativeFragments(string text)
    {
        var result = text.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var (pattern, replacement) in HotelLeadingReplacements)
            {
                var next = pattern.Replace(result, replacement);
                if (!string.Equals(next, result, StringComparison.Ordinal))
                {
                    result = next.TrimStart();
                    changed = true;
                }
            }
        } while (changed);

        return result;
    }

    private static string RestoreCityWhenGenericHotelSuffix(string text, string? cityDisplayName)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(cityDisplayName))
            return text;

        if (!IsGenericHotelSuffixOnly(text))
            return text;

        var cityShort = Regex.Replace(cityDisplayName, @"\s*(etraby|şäheri|şäherçesi)\s*$", "", RegexOptions.IgnoreCase).Trim();
        return string.IsNullOrWhiteSpace(cityShort) ? text : $"{cityShort} {text}".Trim();
    }

    private static bool IsGenericHotelSuffixOnly(string text)
    {
        var folded = NormalizeMatchKey(text);
        return folded is "myhmanhanasy" or "myhmanhana" or "myhymanhanasy";
    }

    private static string StripPartialSaherFragments(string text)
    {
        var result = text.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var (pattern, replacement) in PartialSaherReplacements)
            {
                var next = pattern.Replace(result, replacement);
                if (!string.Equals(next, result, StringComparison.Ordinal))
                {
                    result = next.TrimStart();
                    changed = true;
                }
            }
        } while (changed);

        return result;
    }

    private static string NormalizeHotelFormatting(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var result = text;
        result = Regex.Replace(result, @"([\p{L}\-'\d])""(?=myhmanhan)", "$1 ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"""([^""]+)""", "$1", RegexOptions.CultureInvariant);
        return MultiSpace.Replace(result, " ").Trim();
    }

    private static readonly (Regex Pattern, string Replacement)[] HotelLeadingReplacements =
    [
        (new(@"^ş\.?\s*""([\p{L}\-'\s\.]+)""\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1 "),
        (new(@"^şä+h(?:er|\.?)\s*,?\s*""?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^şaher\s*,?\s*""?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^şäher\s*,?\s*""?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^ş\.\s*([\p{L}\-']+)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1 "),
        (new(@"^ş\.(?:\s|,)+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^ş\.([\p{L}\-']+)(?=\s*myhmanhan)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1 "),
        (new(@"^wel\.(?=[\p{L}""])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş\.(?=[^\s,;.])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş\.(?:\s|,|;|$)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+şäheri\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
    ];

    private static readonly (Regex Pattern, string Replacement)[] LodgingLeadingReplacements =
    [
        ..HotelLeadingReplacements,
        (new(@"^wel[-.]?(?:ň|yn|yň)?\.(?=[\p{L}""\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^w,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^we\.,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^we\.\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^ş\.(?=[\p{L}\d""])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^ş,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^S\.(?=[\p{L}])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş\s*,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş-ň\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+ş-çesi(?:niň|ň)?\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+şäheriniň\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+şäherçesi(?:niň|ň)?\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^ň\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+etr\.(?=[\p{L}\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^etr[-.]?(?:ň|yn|yň)?\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^[\p{L}\-']+\s+s,\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
    ];

    private static readonly (Regex Pattern, string Replacement)[] PartialSaherReplacements =
    [
        (new(@"^ä+h(?:er|\.?)\s*,?\s*""?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^aher\s*,?\s*""?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
        (new(@"^äh\s*\.?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), ""),
    ];

    /// <summary>
    /// Removes leading administrative fragments left after Region/City names are stripped
    /// (e.g. "-yň", "etr.,", "Mary etr.", "aýatynyň" from welaýaty/welaýatynyň).
    /// </summary>
    internal static string StripOrphanAdministrativeFragments(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var result = text.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var pattern in OrphanLeadingPatterns)
            {
                var next = pattern.Replace(result, string.Empty);
                if (!string.Equals(next, result, StringComparison.Ordinal))
                {
                    result = next.TrimStart();
                    changed = true;
                }
            }
        } while (changed);

        return result;
    }

    private static readonly Regex[] OrphanLeadingPatterns =
    [
        new(@"^[\s.,;:\-]+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"^a[yä]atynyň\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etrabynyň\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^etrabynyň\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^abynyň\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^çägind[äe]\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^çäginde\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etr,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etr\.(?:\s|,|;|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etrap(?:\.|,|\s|;|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etrap\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+(?:wel\.?,?|w\.?,?)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+ş\.(?:\s|,|;|$)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^etr\.(?:\s|,|;|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^etr[-.]?(?:ň|yn|yň)\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+s,\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^etr\.(?=[^\s,;.])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^[\p{L}\-']+\s+etr\.(?=[^\s,;.])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^etrap(?:\.|,|\s|;|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^ap(?:\.|,|\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^-?(?:yň|yn|ň|niň|nin|dan|den)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^a[yä]aty(?:nyň|nyn|nda)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^nyn\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^nda\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^(?:şäheri|şäherçesi|şäheriniň|seheri|sehercesi|ş-çesi|ş-ň|ş\.(?:\s|,|;|$))\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^(?:wela[yä]aty|wel\.(?:\s|,|;|$)|wel,(?:\s|$)|w\.(?:\s|,|;|$))\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^wel[-.]?(?:ň|yn|yň)\.?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^w,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^we\.,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^ň\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^ş,\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^s\.(?:\s|,|;|$)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
    ];

    public static string NormalizeMatchKey(string? value) =>
        Visa2014CatalogMatchHelper.NormalizeKey(value);

    /// <summary>
    /// City-scoped fingerprint for merging lodging catalog rows that differ only by
    /// punctuation, leading location text, or minor spelling drift.
    /// </summary>
    public static string BuildLodgingDedupeKey(string? cityNameTm, string? fullAddress)
    {
        var cityKey = NormalizeMatchKey(cityNameTm);
        if (string.IsNullOrEmpty(cityKey))
            return string.Empty;

        var scalar = ExtractLodgingDedupeScalar(fullAddress);
        return string.IsNullOrEmpty(scalar) ? string.Empty : cityKey + "|" + scalar;
    }

    internal static string ExtractLodgingDedupeScalar(string? fullAddress)
    {
        if (string.IsNullOrWhiteSpace(fullAddress))
            return string.Empty;

        var candidate = PreferTrailingLodgingSiteSegment(fullAddress);
        candidate = StripDecorativeQuotes(candidate);
        candidate = StripLodgingDedupeFluff(candidate);
        candidate = ApplyLodgingTypoFolds(candidate);
        return CompactAlphanumericKey(candidate);
    }

    private static string PreferTrailingLodgingSiteSegment(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
            return text;

        for (var i = parts.Length - 1; i >= 0; i--)
        {
            var folded = NormalizeMatchKey(parts[i]);
            if (folded.Contains("uyj", StringComparison.Ordinal)
                || folded.Contains("yasayys", StringComparison.Ordinal)
                || folded.Contains("lojman", StringComparison.Ordinal)
                || folded.Contains("iscilersaherce", StringComparison.Ordinal))
                return parts[i];
        }

        return text;
    }

    private static string StripLodgingDedupeFluff(string text)
    {
        var result = text.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (var pattern in LodgingDedupeFluffPatterns)
            {
                var next = pattern.Replace(result, string.Empty);
                if (!string.Equals(next, result, StringComparison.Ordinal))
                {
                    result = next.TrimStart();
                    changed = true;
                }
            }
        } while (changed);

        return result.Trim();
    }

    private static string StripDecorativeQuotes(string text) =>
        text.Replace('\u201c', ' ').Replace('\u201d', ' ').Replace('"', ' ').Trim();

    private static string ApplyLodgingTypoFolds(string text)
    {
        var folded = NormalizeMatchKey(text);
        folded = folded.Replace("energjy", "enerji", StringComparison.Ordinal);
        folded = folded.Replace("calik", "calyk", StringComparison.Ordinal);
        folded = folded.Replace("stansiyasy", "stansiya", StringComparison.Ordinal);
        folded = folded.Replace("stansiyasi", "stansiya", StringComparison.Ordinal);
        return folded;
    }

    private static string CompactAlphanumericKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var folded = NormalizeMatchKey(text);
        var buffer = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            var c = ch is 'ı' or 'İ' ? 'i' : ch;
            if (char.IsLetterOrDigit(c))
                buffer.Append(c);
        }

        var compact = buffer.ToString();
        if (compact.EndsWith("uyjf", StringComparison.Ordinal))
            compact = compact[..^1];
        compact = compact.Replace("enerjy", "enerji", StringComparison.Ordinal);
        compact = Regex.Replace(compact, @"c[aá]?l[yi]k", "calyk", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return compact;
    }

    private static readonly Regex[] LodgingDedupeFluffPatterns =
    [
        new(@"^.*?\d+[-.]?\s*nji\s*km\.?\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?(?:gündogar|günbatar|göndogar|gundogar|gunbat|dogar|batar).*?tarapynda\s+ýerleşýän\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?uzaklyk(?:da|ta)\s+ýerleşýän\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?şäherçesinde\s+ýerleşýän\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?çäklerinde\s+ýerleşýän\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?çägind[äe]ki\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^.*?ýerleşýän\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^a[yä]atynyň\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^nde\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
    ];

    public static string BuildCityScopedCatalogKey(string? cityNameTm, string? scalar)
    {
        var cityKey = NormalizeMatchKey(cityNameTm);
        var scalarKey = NormalizeMatchKey(scalar);
        if (string.IsNullOrEmpty(cityKey) || string.IsNullOrEmpty(scalarKey))
            return string.Empty;
        return cityKey + "|" + scalarKey;
    }

    private static readonly string[] TurkmenGluedSuffixes =
        ["yň", "yn", "niň", "nin", "nyn", "nda", "dan", "den", "y"];

    private static string StripKnownPrefix(string text, string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return text;

        var foldedText = NormalizeMatchKey(text);
        var foldedPrefix = NormalizeMatchKey(prefix);
        if (string.IsNullOrEmpty(foldedPrefix))
            return text;

        if (foldedText.StartsWith(foldedPrefix, StringComparison.Ordinal))
        {
            var cut = FindOriginalCutForFoldedPrefix(text, foldedPrefix);
            if (cut <= 0)
                cut = prefix.Length;
            cut = ExtendCutForTurkmenSuffix(text, cut);
            cut = FindCutIndex(text, cut);
            return text[cut..].TrimStart(' ', ',', '.', ';', ':');
        }

        foreach (var sep in new[] { ". ", ", ", "; ", ": " })
        {
            var combined = foldedPrefix + NormalizeMatchKey(sep);
            if (foldedText.StartsWith(combined, StringComparison.Ordinal))
            {
                var idx = IndexOfFoldedPrefix(text, prefix);
                if (idx >= 0)
                {
                    var after = idx + prefix.Length;
                    while (after < text.Length && ".,;:".Contains(text[after]))
                        after++;
                    while (after < text.Length && char.IsWhiteSpace(text[after]))
                        after++;
                    return text[after..];
                }
            }
        }

        return text;
    }

    private static string StripWelPrefix(string text, string? regionOrCityToken)
    {
        if (string.IsNullOrWhiteSpace(regionOrCityToken))
            return text;

        var token = Regex.Escape(regionOrCityToken.Trim());
        var pattern =
            $@"^(?:{token}\s*)?(?:wela[ýy]aty(?:nyň|nyn|nda)?|wel\.(?:\s|,|;|$)|wel,(?:\s|$)|w\.(?:\s|,|;|$))\s*";
        return Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase).TrimStart();
    }

    private static string StripEtrapPrefix(string text, string? cityToken)
    {
        if (string.IsNullOrWhiteSpace(cityToken))
            return text;

        var token = Regex.Escape(cityToken.Trim());
        var pattern =
            $@"^(?:{token}\s*)?(?:etraby(?:nyň|nyn|nda)?|etrap(?:\.|,|\s|;|$)|etr\.(?:\s|,|;|$)|etr,(?:\s|$))\s*";
        return Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase).TrimStart();
    }

    /// <summary>
    /// Maps a folded prefix match back to an index in the original string (handles ý/ý vs ASCII y length drift).
    /// </summary>
    private static int FindOriginalCutForFoldedPrefix(string text, string foldedPrefix)
    {
        for (var cut = 1; cut <= text.Length; cut++)
        {
            if (NormalizeMatchKey(text[..cut]) == foldedPrefix)
                return cut;
        }

        for (var cut = 1; cut <= text.Length; cut++)
        {
            var foldedSlice = NormalizeMatchKey(text[..cut]);
            if (foldedSlice.Length >= foldedPrefix.Length
                && foldedSlice.StartsWith(foldedPrefix, StringComparison.Ordinal))
            {
                return cut;
            }
        }

        return 0;
    }

    /// <summary>
    /// When legacy text glues a genitive/locative suffix to the catalog name
    /// (e.g. "Balkan welaýaty" + "nyn" in "Balkan welaýatynyň"), extend the cut past the suffix.
    /// </summary>
    private static int ExtendCutForTurkmenSuffix(string text, int prefixLength)
    {
        if (prefixLength >= text.Length)
            return prefixLength;

        var remainder = text[prefixLength..];
        foreach (var suffix in TurkmenGluedSuffixes)
        {
            if (!remainder.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var afterSuffix = prefixLength + suffix.Length;
            if (afterSuffix >= text.Length
                || char.IsWhiteSpace(text[afterSuffix])
                || ".,;:-".Contains(text[afterSuffix]))
            {
                return afterSuffix;
            }
        }

        return prefixLength;
    }

    private static int FindCutIndex(string text, int prefixLength)
    {
        var idx = Math.Min(prefixLength, text.Length);
        while (idx < text.Length && ".,;:".Contains(text[idx]))
            idx++;
        while (idx < text.Length && char.IsWhiteSpace(text[idx]))
            idx++;
        return idx;
    }

    private static int IndexOfFoldedPrefix(string text, string prefix)
    {
        if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        var folded = NormalizeMatchKey(text);
        var foldedPrefix = NormalizeMatchKey(prefix);
        if (!folded.StartsWith(foldedPrefix, StringComparison.Ordinal))
            return -1;

        return 0;
    }
}
