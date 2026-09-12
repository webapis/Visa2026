#nullable enable

using System.Globalization;
using System.Text;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// <c>PBPL</c> / <c>Person_BirthPlace</c> is the city (Doglan yeri), never the birth country.
/// Officers sometimes store <c>Turkiye/Gaziantep</c>; PCBT already prints the country.
/// </summary>
public static class PersonBirthPlaceText
{
    public static string CityOnly(Person? person)
    {
        var raw = person?.BirthPlace?.Trim();
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var countryKeys = CountryKeys(person?.CountryOfBirth);
        var parts = SplitPlaceParts(raw);
        if (parts.Count > 0)
        {
            var city = parts.LastOrDefault(p => !MatchesCountry(p, countryKeys));
            return city ?? string.Empty;
        }

        return MatchesCountry(raw, countryKeys) ? string.Empty : raw;
    }

    private static IReadOnlyList<string> SplitPlaceParts(string raw)
    {
        foreach (var sep in new[] { '/', ',' })
        {
            var index = raw.IndexOf(sep);
            if (index <= 0 || index >= raw.Length - 1)
                continue;

            return new[] { raw[..index].Trim(), raw[(index + 1)..].Trim() }
                .Where(static p => p.Length > 0)
                .ToList();
        }

        return Array.Empty<string>();
    }

    private static readonly string[] WellKnownCountryLabels =
    [
        "turkey",
        "turkiye",
        "turkiie",
        "tur",
    ];

    private static IReadOnlyList<string> CountryKeys(Country? country)
    {
        var keys = new HashSet<string>(WellKnownCountryLabels, StringComparer.OrdinalIgnoreCase);
        if (country != null)
        {
            foreach (var label in new[] { country.NameTm, country.Name, country.Code })
            {
                if (!string.IsNullOrWhiteSpace(label))
                    keys.Add(Fold(label));
            }
        }

        return keys.ToList();
    }

    private static bool MatchesCountry(string value, IReadOnlyList<string> countryKeys)
    {
        if (countryKeys.Count == 0)
            return false;

        var folded = Fold(value);
        return countryKeys.Any(k => string.Equals(k, folded, StringComparison.OrdinalIgnoreCase));
    }

    private static string Fold(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch is 'ı' or 'I' or 'İ' ? 'i' : ch);
        }

        return sb.ToString();
    }
}