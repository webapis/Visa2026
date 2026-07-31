using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014CityLookupMatcherTests
{
    private static readonly Guid CanonicalId = Guid.Parse("E79D7D6E-2CD4-47AC-29DD-08DED771520A");
    private static readonly Guid OrphanId = Guid.Parse("408D6222-7326-4C5D-1B27-08DED76DB918");

    [Fact]
    public void Resolve_WithRegion_PrefersRegionLinkedRow()
    {
        var cities = SampleCities();

        var id = Visa2014CityLookupMatcher.Resolve(cities, "Turkmenbasy etraby", "Balkan welayaty");

        Assert.Equal(CanonicalId, id);
    }

    [Fact]
    public void Resolve_WithWrongRegion_FallsBackToUniqueRegionLinkedCity()
    {
        var cities = SampleCities();

        var id = Visa2014CityLookupMatcher.Resolve(cities, "Turkmenbasy etraby", "Wrong welayaty");

        Assert.Equal(CanonicalId, id);
    }

    [Fact]
    public void Resolve_WithWrongRegion_DoesNotGuessWhenMultipleRegionLinked()
    {
        var cities = SampleCities();
        cities.Add(new City
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            NameTm = "Turkmenbasy etraby",
            Region = new Region { NameTm = "Mary welayaty" },
            RegionName = "Mary welayaty",
        });

        var id = Visa2014CityLookupMatcher.Resolve(cities, "Turkmenbasy etraby", "Wrong welayaty");

        Assert.Null(id);
    }

    [Fact]
    public void Resolve_WithoutRegion_PrefersCanonicalOverOrphan()
    {
        var cities = SampleCities();

        var id = Visa2014CityLookupMatcher.Resolve(cities, "Turkmenbasy etraby");

        Assert.Equal(CanonicalId, id);
    }

    [Fact]
    public void Resolve_WithRegion_FallsBackToNameOnly_WhenNoCityHasRegionMetadata()
    {
        var cities = new List<City>
        {
            new() { Id = OrphanId, NameTm = "Mary etraby" },
            new() { Id = CanonicalId, NameTm = "Mary etraby" },
        };

        var id = Visa2014CityLookupMatcher.Resolve(cities, "Mary etraby", "Mary welayaty");

        Assert.Equal(OrphanId, id);
    }

    private static List<City> SampleCities() =>
    [
        new City
        {
            Id = OrphanId,
            NameTm = "Turkmenbasy etraby",
        },
        new City
        {
            Id = CanonicalId,
            NameTm = "Turkmenbasy etraby",
            Region = new Region { NameTm = "Balkan welayaty" },
            RegionName = "Balkan welayaty",
        },
    ];
}