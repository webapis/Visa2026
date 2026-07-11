namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// City resolve for legacy import.
/// Prefer legacy Region+City when that pair matches a catalog row (Wikipedia/OSM-aligned City.Region).
/// If the legacy Region is wrong but exactly one region-linked city has that name, use that city
/// (catalog geography wins). Never pick an ambiguous multi-region name match.
/// </summary>
internal static class Visa2014CityLookupMatcher
{
    public static Guid? Resolve(IReadOnlyList<City> cities, string? nameTm, string? regionNameTm = null)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return null;

        var nameMatches = new List<City>();
        foreach (var city in cities)
        {
            if (NameMatches(city, nameTm))
                nameMatches.Add(city);
        }

        if (nameMatches.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(regionNameTm))
        {
            foreach (var city in nameMatches)
            {
                if (city.Region != null && Visa2014CatalogMatchHelper.KeysEqual(city.Region.NameTm, regionNameTm))
                    return city.Id;
                if (Visa2014CatalogMatchHelper.KeysEqual(city.RegionName, regionNameTm))
                    return city.Id;
            }

            // Legacy Region did not match — fall back only when catalog has a unique Region-FK city.
            // Ignore RegionName-only / orphan duplicates (common after catalog sync + null-Region clones).
            var regionLinked = nameMatches.Where(c => c.Region != null).ToList();
            if (regionLinked.Count == 1)
                return regionLinked[0].Id;

            // No city carries Region nav (loader gap) — name-only fallback when none have metadata.
            if (regionLinked.Count == 0 &&
                nameMatches.All(c => c.Region == null && string.IsNullOrWhiteSpace(c.RegionName)))
                return PreferRegionLinkedOrFirst(nameMatches);

            // Single RegionName-only row (no FK loaded) — still usable.
            var namedOnly = nameMatches
                .Where(c => c.Region == null && !string.IsNullOrWhiteSpace(c.RegionName))
                .ToList();
            if (regionLinked.Count == 0 && namedOnly.Count == 1)
                return namedOnly[0].Id;

            return null;
        }

        return PreferRegionLinkedOrFirst(nameMatches);
    }

    private static Guid? PreferRegionLinkedOrFirst(IReadOnlyList<City> nameMatches)
    {
        if (nameMatches.Count == 0)
            return null;

        foreach (var city in nameMatches)
        {
            if (city.Region != null || !string.IsNullOrWhiteSpace(city.RegionName))
                return city.Id;
        }

        return nameMatches[0].Id;
    }

    private static bool NameMatches(City city, string nameTm) =>
        Visa2014CatalogMatchHelper.KeysEqual(city.NameTm, nameTm)
        || string.Equals(city.NameTm?.Trim(), nameTm.Trim(), StringComparison.Ordinal);
}
