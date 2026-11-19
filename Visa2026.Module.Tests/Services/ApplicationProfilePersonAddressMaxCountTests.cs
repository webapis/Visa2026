using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePersonAddressMaxCountTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void Clamp_LimitsToOneOrTwo(int input, int expected) =>
        Assert.Equal(expected, ApplicationProfilePersonAddressMaxCount.Clamp(input));

    [Fact]
    public void For_WhenRequireAddress1And2_ReturnsTwo()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = true,
        };
        Assert.Equal(2, ApplicationProfilePersonAddressMaxCount.For(profile));
        Assert.Equal(1, ApplicationProfilePersonAddressMaxCount.RequiredSlotCount(profile));
    }

    [Fact]
    public void For_WhenRequireAddress1Only_ReturnsOne()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = true,
            RequireAddress2 = false,
        };
        Assert.Equal(1, ApplicationProfilePersonAddressMaxCount.For(profile));
        Assert.Equal(1, ApplicationProfilePersonAddressMaxCount.RequiredSlotCount(profile));
    }

    [Fact]
    public void For_LegacyWhenRequiredAndMaxTwo_ReturnsTwo()
    {
#pragma warning disable CS0618
        var profile = new ApplicationProfile
        {
            RequireAddress1 = false,
            RequireAddress2 = false,
            RequirePersonAddressOfResidence = true,
            PersonAddressOfResidenceMaxCount = 2,
        };
#pragma warning restore CS0618
        Assert.Equal(2, ApplicationProfilePersonAddressMaxCount.For(profile));
        Assert.Equal(1, ApplicationProfilePersonAddressMaxCount.RequiredSlotCount(profile));
    }

    [Fact]
    public void For_WhenToggleOff_ReturnsZero()
    {
        var profile = new ApplicationProfile
        {
            RequireAddress1 = false,
            RequireAddress2 = false,
        };
#pragma warning disable CS0618
        profile.RequirePersonAddressOfResidence = false;
        profile.PersonAddressOfResidenceMaxCount = 2;
#pragma warning restore CS0618
        Assert.Equal(0, ApplicationProfilePersonAddressMaxCount.For(profile));
        Assert.Equal(0, ApplicationProfilePersonAddressMaxCount.RequiredSlotCount(profile));
    }

    [Fact]
    public void SyncRequireFlagsFromLegacy_MapsMaxTwo()
    {
#pragma warning disable CS0618
        var profile = new ApplicationProfile
        {
            RequirePersonAddressOfResidence = true,
            PersonAddressOfResidenceMaxCount = 2,
        };
#pragma warning restore CS0618
        ApplicationProfilePersonAddressMaxCount.SyncRequireFlagsFromLegacy(profile);
        Assert.True(profile.RequireAddress1);
        Assert.True(profile.RequireAddress2);
    }
}