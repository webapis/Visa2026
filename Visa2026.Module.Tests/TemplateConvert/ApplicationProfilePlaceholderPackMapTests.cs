#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>
/// Profile toggles gate which placeholder packs enter convert/scan sets.
/// PersonEducation must stay available for cancel-visa sanaw even when the Education tile is hidden.
/// </summary>
public class ApplicationProfilePlaceholderPackMapTests
{
    [Fact]
    public void IsEnabled_throws_when_profile_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationProfilePlaceholderPackMap.IsEnabled(null!, UserReportPlaceholderPack.Core));
    }

    [Fact]
    public void IsEnabled_Core_always_true()
    {
        var profile = new ApplicationProfile();
        Assert.True(ApplicationProfilePlaceholderPackMap.IsEnabled(profile, UserReportPlaceholderPack.Core));
    }

    [Fact]
    public void IsEnabled_PersonEducation_always_true_even_when_require_flags_off()
    {
        var profile = new ApplicationProfile
        {
            RequirePersonEducation = false,
            ActionFamily = ApplicationProfileActionFamily.Cancellation,
        };
        Assert.True(ApplicationProfilePlaceholderPackMap.IsEnabled(
            profile, UserReportPlaceholderPack.PersonEducation));
    }

    [Theory]
    [InlineData(UserReportPlaceholderPack.PersonPassport, true)]
    [InlineData(UserReportPlaceholderPack.PersonVisa, false)]
    [InlineData(UserReportPlaceholderPack.PersonAddressOfResidence, true)]
    [InlineData(UserReportPlaceholderPack.PersonPosition, false)]
    [InlineData(UserReportPlaceholderPack.PersonSalary, true)]
    [InlineData(UserReportPlaceholderPack.PersonMedical, false)]
    [InlineData(UserReportPlaceholderPack.PersonInvitationItem, true)]
    [InlineData(UserReportPlaceholderPack.PersonWorkPermitItem, false)]
    [InlineData(UserReportPlaceholderPack.PersonBorderZoneItem, true)]
    [InlineData(UserReportPlaceholderPack.PersonRejectionItem, false)]
    public void IsEnabled_follows_require_toggles_for_person_packs(
        UserReportPlaceholderPack pack,
        bool expected)
    {
        var profile = new ApplicationProfile
        {
            RequirePersonPassport = true,
            RequirePersonVisa = false,
            RequirePersonAddressOfResidence = true,
            RequirePersonPosition = false,
            RequirePersonSalary = true,
            RequirePersonMedical = false,
            RequirePersonInvitationItem = true,
            RequirePersonWorkPermitItem = false,
            RequirePersonBorderZoneItem = true,
            RequirePersonRejectionItem = false,
        };

        Assert.Equal(expected, ApplicationProfilePlaceholderPackMap.IsEnabled(profile, pack));
    }

    [Fact]
    public void IsEnabled_PersonTravelHistory_requires_registration_family_and_toggle()
    {
        var registrationOn = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RequirePersonTravelHistory = true,
        };
        var registrationOff = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RequirePersonTravelHistory = false,
        };
        var issuanceOn = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Issuance,
            RequirePersonTravelHistory = true,
        };

        Assert.True(ApplicationProfilePlaceholderPackMap.IsEnabled(
            registrationOn, UserReportPlaceholderPack.PersonTravelHistory));
        Assert.False(ApplicationProfilePlaceholderPackMap.IsEnabled(
            registrationOff, UserReportPlaceholderPack.PersonTravelHistory));
        Assert.False(ApplicationProfilePlaceholderPackMap.IsEnabled(
            issuanceOn, UserReportPlaceholderPack.PersonTravelHistory));
    }

    [Fact]
    public void IsEnabled_Unknown_pack_is_false()
    {
        Assert.False(ApplicationProfilePlaceholderPackMap.IsEnabled(
            new ApplicationProfile(), UserReportPlaceholderPack.Unknown));
    }
}
