using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014MigrationServiceInferenceRulesTests
{
    [Fact]
    public void Infer_MissingRegion_ReturnsNone()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: null,
            regionName: null,
            cityMgCode: "C1",
            cityName: "City",
            usedExpiredAddressFallback: false);

        Assert.Null(result.MigrationServiceNameTm);
        Assert.Equal("none", result.Confidence);
        Assert.Contains("Region mgCode missing", result.Reason);
        Assert.False(result.UsedExpiredAddressFallback);
    }

    [Fact]
    public void Infer_CityOverrideByMgCode_WinsOverRegion()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: "R1",
            regionName: "Region One",
            cityMgCode: "ASHG",
            cityName: "Aşgabat",
            usedExpiredAddressFallback: false);

        Assert.Equal("Aşgabat MS", result.MigrationServiceNameTm);
        Assert.Equal("high", result.Confidence);
        Assert.Contains("City override", result.Reason);
    }

    [Fact]
    public void Infer_CityNameContains_UsesNormalizedMatch()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: "R1",
            regionName: "Region One",
            cityMgCode: null,
            cityName: "  Balkanabat  ",
            usedExpiredAddressFallback: false);

        Assert.Equal("Balkan MS", result.MigrationServiceNameTm);
        Assert.Equal("high", result.Confidence);
        Assert.Contains("City name contains", result.Reason);
    }

    [Fact]
    public void Infer_RegionRule_UsesRegionalOffice()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: "R1",
            regionName: "Mary",
            cityMgCode: "M1",
            cityName: "Mary",
            usedExpiredAddressFallback: false);

        Assert.Equal("Mary MS", result.MigrationServiceNameTm);
        Assert.Equal("medium", result.Confidence);
        Assert.Contains("Regional office from Mary", result.Reason);
    }

    [Fact]
    public void Infer_ExpiredFallback_DowngradesHighToMedium()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: "R1",
            regionName: "Mary",
            cityMgCode: "ASHG",
            cityName: "Aşgabat",
            usedExpiredAddressFallback: true);

        Assert.Equal("Aşgabat MS", result.MigrationServiceNameTm);
        Assert.Equal("medium", result.Confidence);
        Assert.Contains("expired-only address fallback", result.Reason);
        Assert.True(result.UsedExpiredAddressFallback);
    }

    [Fact]
    public void Infer_UnknownRegion_ReturnsNone()
    {
        var rules = CreateRules();

        var result = rules.Infer(
            regionMgCode: "UNKNOWN",
            regionName: "X",
            cityMgCode: null,
            cityName: null,
            usedExpiredAddressFallback: false);

        Assert.Null(result.MigrationServiceNameTm);
        Assert.Equal("none", result.Confidence);
        Assert.Contains("Unknown region mgCode", result.Reason);
    }

    [Fact]
    public void PickCurrent_PrefersStillValid_ThenLatestExpiration()
    {
        var olderValid = new Visa2014AddressForInference(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "R1",
            "R",
            "C1",
            "C",
            ExpirationDate: new DateTime(2030, 1, 1, 0, 0, 0));
        var newerValid = new Visa2014AddressForInference(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "R1",
            "R",
            "C1",
            "C",
            ExpirationDate: new DateTime(2035, 1, 1, 0, 0, 0));
        var expired = new Visa2014AddressForInference(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "R1",
            "R",
            "C1",
            "C",
            ExpirationDate: new DateTime(2020, 1, 1, 0, 0, 0));

        var picked = Visa2014MigrationServiceAddressPicker.PickCurrent(
            [expired, olderValid, newerValid],
            asOf: new DateTime(2026, 8, 15, 0, 0, 0),
            out var usedExpiredFallback);

        Assert.False(usedExpiredFallback);
        Assert.Equal(newerValid.LegacyOid, picked!.LegacyOid);
    }

    [Fact]
    public void PickCurrent_AllExpired_UsesLatestExpiredAndSetsFallback()
    {
        var older = new Visa2014AddressForInference(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "R1",
            "R",
            null,
            null,
            ExpirationDate: new DateTime(2018, 1, 1, 0, 0, 0));
        var newer = new Visa2014AddressForInference(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "R1",
            "R",
            null,
            null,
            ExpirationDate: new DateTime(2019, 6, 1, 0, 0, 0));

        var picked = Visa2014MigrationServiceAddressPicker.PickCurrent(
            [older, newer],
            asOf: new DateTime(2026, 8, 15, 0, 0, 0),
            out var usedExpiredFallback);

        Assert.True(usedExpiredFallback);
        Assert.Equal(newer.LegacyOid, picked!.LegacyOid);
    }

    [Fact]
    public void PickCurrent_Empty_ReturnsNull()
    {
        var picked = Visa2014MigrationServiceAddressPicker.PickCurrent(
            Array.Empty<Visa2014AddressForInference>(),
            asOf: null,
            out var usedExpiredFallback);

        Assert.Null(picked);
        Assert.False(usedExpiredFallback);
    }

    private static Visa2014MigrationServiceInferenceRules CreateRules() =>
        new()
        {
            RegionRules =
            [
                new Visa2014MigrationServiceInferenceRules.RegionRule
                {
                    RegionMgCode = "R1",
                    MigrationServiceNameTm = "Mary MS",
                    Confidence = "medium",
                },
            ],
            CityOverrides =
            [
                new Visa2014MigrationServiceInferenceRules.CityOverride
                {
                    CityMgCodes = ["ASHG"],
                    MigrationServiceNameTm = "Aşgabat MS",
                    Confidence = "high",
                },
                new Visa2014MigrationServiceInferenceRules.CityOverride
                {
                    CityNameContains = "balkanabat",
                    MigrationServiceNameTm = "Balkan MS",
                    Confidence = "high",
                },
            ],
        };
}
