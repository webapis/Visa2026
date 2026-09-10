using System;
using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report / merge text for a linked residence. <c>ADRS</c> is Region + City + street.
/// Region is omitted when it is the same as City (Aşgabat şäheri).
/// Welaýat names use the genitive <c>welaýatynyň</c>.
/// </summary>
public static class AddressOfResidenceReportText
{
    public const string AsgabatSaheri = "Aşgabat şäheri";

    private const string Welayaty = "welaýaty";
    private const string Welayatynyn = "welaýatynyň";

    public static string CityAndStreet(AddressOfResidence? address)
    {
        if (address == null)
            return string.Empty;

        var region = address.Region?.NameTm?.Trim();
        var city = address.City?.NameTm?.Trim();
        var street = address.FullAddress?.Trim();

        var parts = new List<string>();
        if (ShouldIncludeRegion(region, city, street))
            parts.Add(ToWelayatGenitive(region!));
        if (ShouldInclude(city, parts, street))
            parts.Add(city!);
        if (!string.IsNullOrEmpty(street))
            parts.Add(street);

        return string.Join(", ", parts);
    }

    internal static string ToWelayatGenitive(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return region ?? string.Empty;

        var trimmed = region.Trim();
        if (trimmed.EndsWith(Welayatynyn, StringComparison.OrdinalIgnoreCase))
            return trimmed;
        if (trimmed.EndsWith(Welayaty, StringComparison.OrdinalIgnoreCase))
            return trimmed[..^Welayaty.Length] + Welayatynyn;

        return trimmed;
    }

    private static bool ShouldIncludeRegion(string? region, string? city, string? street)
    {
        if (string.IsNullOrEmpty(region))
            return false;
        if (NamesMatch(region, city))
            return false;
        if (NamesMatch(region, AsgabatSaheri))
            return false;
        if (ContainsName(city, region) || ContainsName(street, region))
            return false;
        return true;
    }

    private static bool ShouldInclude(string? part, List<string> already, string? street)
    {
        if (string.IsNullOrEmpty(part))
            return false;
        if (ContainsName(street, part))
            return false;
        foreach (var existing in already)
        {
            if (NamesMatch(existing, part) || ContainsName(existing, part) || ContainsName(part, existing))
                return false;
        }

        return true;
    }

    private static bool NamesMatch(string? left, string? right) =>
        !string.IsNullOrEmpty(left)
        && !string.IsNullOrEmpty(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool ContainsName(string? haystack, string? needle) =>
        !string.IsNullOrEmpty(haystack)
        && !string.IsNullOrEmpty(needle)
        && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}