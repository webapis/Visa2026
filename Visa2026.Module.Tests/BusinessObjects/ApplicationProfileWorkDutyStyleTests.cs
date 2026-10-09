#nullable enable

using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// <see cref="ApplicationProfileConfigurationResolver.IsWorkPermitStyleForWorkDuty"/> gates
/// WorkDuty tile visibility and auto-link — code matrix beyond ShowCurrentWorkDuty.
/// </summary>
public class ApplicationProfileWorkDutyStyleTests
{
    [Fact]
    public void IsWorkPermitStyleForWorkDuty_false_for_null()
    {
        Assert.False(ApplicationProfileConfigurationResolver.IsWorkPermitStyleForWorkDuty(null));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void IsWorkPermitStyleForWorkDuty_true_when_any_wp_produce_flag(
        bool produceWorkPermit,
        bool cancelWorkPermits,
        bool produceWorkLocation)
    {
        var profile = new ApplicationProfile
        {
            Code = "unrelated",
            ProduceWorkPermit = produceWorkPermit,
            CancelWorkPermits = cancelWorkPermits,
            ProduceWorkLocation = produceWorkLocation,
        };

        Assert.True(ApplicationProfileConfigurationResolver.IsWorkPermitStyleForWorkDuty(profile));
    }

    [Theory]
    [InlineData("get_invitation_wp")]
    [InlineData("INVITATION_WP_EXTRA")]
    [InlineData("according_to_wp")]
    [InlineData("visa_wp")]
    [InlineData("extend_visa_wp")]
    [InlineData("cancel_visa_wp")]
    [InlineData("cancel_visa_wp_ext")]
    [InlineData("cancel_workpermit")]
    [InlineData("change_workpermit")]
    [InlineData("SomethingWorkPermitElse")]
    public void IsWorkPermitStyleForWorkDuty_true_for_wp_codes(string code)
    {
        var profile = new ApplicationProfile { Code = code };
        Assert.True(ApplicationProfileConfigurationResolver.IsWorkPermitStyleForWorkDuty(profile));
    }

    [Theory]
    [InlineData("get_invitation")]
    [InlineData("produce_visa")]
    [InlineData("")]
    [InlineData(null)]
    public void IsWorkPermitStyleForWorkDuty_false_for_non_wp_codes(string? code)
    {
        var profile = new ApplicationProfile { Code = code };
        Assert.False(ApplicationProfileConfigurationResolver.IsWorkPermitStyleForWorkDuty(profile));
    }
}
