namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014CityLookupMatcher
{
    public static Guid? Resolve(IReadOnlyList<City> cities, string? nameTm, string? regionNameTm = null)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
            return null;

        if (!string.IsNullOrWhiteSpace(regionNameTm))
        {
            foreach (var city in cities)
            {
                if (!NameMatches(city, nameTm))
                    continue;

                if (city.Region != null && Visa2014CatalogMatchHelper.KeysEqual(city.Region.NameTm, regionNameTm))
                    return city.Id;
                if (Visa2014CatalogMatchHelper.KeysEqual(city.RegionName, regionNameTm))
                    return city.Id;
            }

            return null;
        }

        Guid? orphan = null;
        foreach (var city in cities)
        {
            if (!NameMatches(city, nameTm))
                continue;

            if (city.Region != null)
                return city.Id;

            orphan ??= city.Id;
        }

        return orphan;
    }

    private static bool NameMatches(City city, string nameTm) =>
        Visa2014CatalogMatchHelper.KeysEqual(city.NameTm, nameTm)
        || string.Equals(city.NameTm?.Trim(), nameTm.Trim(), StringComparison.Ordinal);
}