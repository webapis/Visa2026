#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// WorkDuty Last-N / auto-link count (WP-style profiles only). Added with WDUT tile.
/// </summary>
public class ApplicationProfilePersonLastCountWorkDutyTests
{
    [Fact]
    public void For_WorkDuty_returns_default_when_position_required_on_wp_style_profile()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPosition = true,
            ProduceWorkPermit = true,
            Code = "get_invitation",
        };

        Assert.Equal(
            ApplicationProfilePersonLastCount.Default,
            ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }

    [Fact]
    public void For_WorkDuty_returns_zero_when_position_not_required()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPosition = false,
            ProduceWorkPermit = true,
        };

        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }

    [Fact]
    public void For_WorkDuty_returns_zero_when_profile_is_not_work_permit_style()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPosition = true,
            ProduceWorkPermit = false,
            CancelWorkPermits = false,
            ProduceWorkLocation = false,
            Code = "get_invitation",
        };

        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }

    [Fact]
    public void For_WorkDuty_matches_via_wp_code_when_produce_flags_off()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPosition = true,
            Code = "cancel_invitation_wp",
        };

        Assert.Equal(
            ApplicationProfilePersonLastCount.Default,
            ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }

    [Fact]
    public void SupportsLastCount_excludes_WorkDuty()
    {
        Assert.False(ApplicationProfilePersonLastCount.SupportsLastCount(
            ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }
}
