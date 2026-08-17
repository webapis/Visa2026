using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014AddressOfResidenceSponsorCanonicalRegistrationTests
{
    [Fact]
    public void CompareAddressRecency_NullExpirationRanksNewest()
    {
        var open = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var expired = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var cmp = Visa2014AddressOfResidenceSponsorCanonicalRegistration.CompareAddressRecency(
            expA: null,
            oidA: open,
            expB: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            oidB: expired);

        Assert.True(cmp > 0);
    }

    [Fact]
    public void CompareAddressRecency_LaterExpirationWins()
    {
        var olderOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newerOid = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var cmp = Visa2014AddressOfResidenceSponsorCanonicalRegistration.CompareAddressRecency(
            expA: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            oidA: olderOid,
            expB: new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            oidB: newerOid);

        Assert.True(cmp > 0);
    }

    [Fact]
    public void CompareAddressRecency_EqualDatesBreakTieByOid()
    {
        var smaller = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var larger = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var day = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Unspecified);

        var cmp = Visa2014AddressOfResidenceSponsorCanonicalRegistration.CompareAddressRecency(
            expA: day, oidA: larger, expB: day, oidB: smaller);

        Assert.True(cmp > 0);
    }
}
