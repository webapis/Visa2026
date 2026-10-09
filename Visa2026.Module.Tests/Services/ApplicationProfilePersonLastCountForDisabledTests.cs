using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Last-N auto-link counts must be 0 when the profile toggle is off so officers
/// do not get phantom passport/visa/WP links on profiles that hide those sections.
/// </summary>
public sealed class ApplicationProfilePersonLastCountForDisabledTests
{
    [Theory]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Passport)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Visa)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.InvitationItem)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.WorkPermitItem)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.BorderZoneItem)]
    public void SupportsLastCount_true_only_for_dated_document_kinds(
        ApplicationProfileInstancePersonLinkKind kind)
    {
        Assert.True(ApplicationProfilePersonLastCount.SupportsLastCount(kind));
    }

    [Theory]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Education)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.AddressOfResidence)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Salary)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.MedicalRecord)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.TravelHistory)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.RejectionItem)]
    public void SupportsLastCount_false_for_single_current_kinds(
        ApplicationProfileInstancePersonLinkKind kind)
    {
        Assert.False(ApplicationProfilePersonLastCount.SupportsLastCount(kind));
    }

    [Fact]
    public void For_returns_zero_when_require_toggle_is_off_even_if_LastCount_is_set()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPassport = false,
            PersonPassportLastCount = 3,
            RequirePersonVisa = false,
            PersonVisaLastCount = 2,
            RequirePersonInvitationItem = false,
            PersonInvitationItemLastCount = 3,
            RequirePersonWorkPermitItem = false,
            PersonWorkPermitItemLastCount = 2,
            RequirePersonBorderZoneItem = false,
            PersonBorderZoneItemLastCount = 2,
            RequirePersonEducation = false,
            RequirePersonAddressOfResidence = false,
            RequirePersonSalary = false,
            RequirePersonMedical = false,
            RequirePersonTravelHistory = false,
            RequirePersonRejectionItem = false,
            RequirePersonPosition = false,
        };

        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Passport));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Visa));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.InvitationItem));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkPermitItem));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.BorderZoneItem));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Education));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.AddressOfResidence));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Salary));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.MedicalRecord));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.TravelHistory));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.RejectionItem));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Position));
        Assert.Equal(0, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.WorkDuty));
    }

    [Fact]
    public void For_null_profile_returns_Default()
    {
        Assert.Equal(
            ApplicationProfilePersonLastCount.Default,
            ApplicationProfilePersonLastCount.For((ApplicationProfile?)null, ApplicationProfileInstancePersonLinkKind.Passport));
    }

    [Fact]
    public void For_clamps_LastCount_when_require_toggle_is_on()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonVisa = true,
            PersonVisaLastCount = 9,
            RequirePersonPassport = true,
            PersonPassportLastCount = 0,
        };

        Assert.Equal(3, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Visa));
        Assert.Equal(1, ApplicationProfilePersonLastCount.For(profile, ApplicationProfileInstancePersonLinkKind.Passport));
    }
}
