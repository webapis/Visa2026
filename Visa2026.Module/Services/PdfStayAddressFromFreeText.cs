using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.Services;

/// <summary>
/// PDF fields 33–34 are choice codes inferred from Address 1 free text.
/// Field 35 is Address 2 (optional) plus Address 1. Region and City rows are name catalogs only.
/// </summary>
public static class PdfStayAddressFromFreeText
{
    public readonly record struct Place(string Name, string? Code, string? RegionName);

    public readonly record struct Codes(string? RegionCode, string? CityCode);

    private static readonly Lazy<(IReadOnlyList<Place> Regions, IReadOnlyList<Place> Cities)> Seed =
        new(LoadSeed);

    public static string Combine(string? address2, string? address1)
    {
        var second = CaseAddressAutocomplete.NormalizeDisplay(address2);
        var first = CaseAddressAutocomplete.NormalizeDisplay(address1);
        if (second.Length == 0)
            return first;
        if (first.Length == 0)
            return second;
        if (string.Equals(CaseAddressAutocomplete.FoldKey(second), CaseAddressAutocomplete.FoldKey(first), StringComparison.Ordinal))
            return first;
        return second + ", " + first;
    }

    public static Codes Extract(string? address1) =>
        Extract(address1, Seed.Value.Regions, Seed.Value.Cities);

    public static Codes Extract(
        string? address1,
        IReadOnlyList<Place> regions,
        IReadOnlyList<Place> cities)
    {
        var folded = CaseAddressAutocomplete.FoldKey(address1);
        if (folded.Length == 0)
            return default;

        var city = BestMatch(folded, cities);
        var region = BestMatch(folded, regions);
        var regionCode = region?.Code;
        if (string.IsNullOrWhiteSpace(regionCode) && !string.IsNullOrWhiteSpace(city?.RegionName))
            regionCode = CodeForName(regions, city.Value.RegionName);

        return new Codes(
            string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim(),
            string.IsNullOrWhiteSpace(city?.Code) ? null : city.Value.Code.Trim());
    }

    public static (IReadOnlyList<Place> Regions, IReadOnlyList<Place> Cities) ResolveCatalog(IObjectSpace? objectSpace)
    {
        if (objectSpace == null)
            return Seed.Value;

        try
        {
            var regions = objectSpace.GetObjectsQuery<Region>()
                .Select(region => new Place(region.NameTm, region.PdfForm_Code, null))
                .ToList();
            var cities = objectSpace.GetObjectsQuery<City>()
                .Select(city => new Place(city.NameTm, city.PdfForm_Code, city.Region != null ? city.Region.NameTm : city.RegionName))
                .ToList();
            if (regions.Count == 0 && cities.Count == 0)
                return Seed.Value;
            return (
                regions.Count == 0 ? Seed.Value.Regions : regions,
                cities.Count == 0 ? Seed.Value.Cities : cities);
        }
        catch (Exception)
        {
            return Seed.Value;
        }
    }

    private static Place? BestMatch(string foldedAddress, IReadOnlyList<Place> places)
    {
        Place? best = null;
        var bestLength = 0;
        foreach (var place in places)
        {
            var name = CaseAddressAutocomplete.FoldKey(place.Name);
            if (name.Length <= bestLength)
                continue;
            if (!ContainsBounded(foldedAddress, name))
                continue;
            best = place;
            bestLength = name.Length;
        }

        return best;
    }

    private static string? CodeForName(IReadOnlyList<Place> regions, string regionName)
    {
        var folded = CaseAddressAutocomplete.FoldKey(regionName);
        foreach (var region in regions)
        {
            if (string.Equals(CaseAddressAutocomplete.FoldKey(region.Name), folded, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(region.Code))
                return region.Code.Trim();
        }

        return null;
    }

    private static bool ContainsBounded(string haystack, string needle)
    {
        var start = 0;
        while (start <= haystack.Length - needle.Length)
        {
            var at = haystack.IndexOf(needle, start, StringComparison.Ordinal);
            if (at < 0)
                return false;
            var beforeOk = at == 0 || !char.IsLetter(haystack[at - 1]);
            var after = at + needle.Length;
            var afterOk = after >= haystack.Length || !char.IsLetter(haystack[after]);
            if (beforeOk && afterOk)
                return true;
            start = at + 1;
        }

        return false;
    }

    private static (IReadOnlyList<Place> Regions, IReadOnlyList<Place> Cities) LoadSeed()
    {
        return (ReadPlaces("region.json", includeRegion: false), ReadPlaces("city.json", includeRegion: true));
    }

    private static IReadOnlyList<Place> ReadPlaces(string fileName, bool includeRegion)
    {
        var file = LookupCatalogResourceLoader.LoadCatalogFile(fileName);
        if (file?.Rows == null || file.Rows.Count == 0)
            return Array.Empty<Place>();

        var places = new List<Place>(file.Rows.Count);
        foreach (var row in file.Rows)
        {
            var name = Text(row, "NameTm");
            if (name.Length == 0)
                continue;
            var code = Text(row, "PdfForm_Code");
            var region = includeRegion ? Text(row, "Region") : string.Empty;
            places.Add(new Place(
                name,
                code.Length == 0 ? null : code,
                region.Length == 0 ? null : region));
        }

        return places;
    }

    private static string Text(Dictionary<string, JsonElement> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.String)
            return string.Empty;
        return value.GetString()?.Trim() ?? string.Empty;
    }
}
