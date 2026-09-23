using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Travel history is Registration-only after the 2026-09 policy tighten.
/// Catalog/mapper tests cover seed rows; these pin the pure gate used at runtime.
/// </summary>
public sealed class ApplicationProfileTravelHistoryPolicyTests
{
    [Fact]
    public void AllowsPersonTravelHistory_null_profile_is_false() =>
        Assert.False(ApplicationProfileTravelHistoryPolicy.AllowsPersonTravelHistory(null));

    [Theory]
    [InlineData(ApplicationProfileActionFamily.Registration, true)]
    [InlineData(ApplicationProfileActionFamily.Issuance, false)]
    [InlineData(ApplicationProfileActionFamily.Cancellation, false)]
    [InlineData(ApplicationProfileActionFamily.BusinessTrip, false)]
    [InlineData(ApplicationProfileActionFamily.Change, false)]
    public void AllowsPersonTravelHistory_family_overload(ApplicationProfileActionFamily family, bool expected) =>
        Assert.Equal(expected, ApplicationProfileTravelHistoryPolicy.AllowsPersonTravelHistory(family));

    [Theory]
    [InlineData(ApplicationProfileActionFamily.Registration, true)]
    [InlineData(ApplicationProfileActionFamily.Issuance, false)]
    [InlineData(ApplicationProfileActionFamily.Cancellation, false)]
    [InlineData(ApplicationProfileActionFamily.BusinessTrip, false)]
    [InlineData(ApplicationProfileActionFamily.Change, false)]
    public void AllowsPersonTravelHistory_reads_profile_ActionFamily(
        ApplicationProfileActionFamily family,
        bool expected)
    {
        var profile = new ApplicationProfile { ActionFamily = family };
        Assert.Equal(expected, ApplicationProfileTravelHistoryPolicy.AllowsPersonTravelHistory(profile));
    }
}
