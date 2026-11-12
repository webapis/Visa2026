using System.Globalization;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationWorkspace;

/// <summary>
/// Case-summary address-type DisplayValue must use localized site labels (e.g. TK lodging),
/// not <see cref="ResidenceType"/> enum names — officer list must match invitation letter wording.
/// </summary>
public class ApplicationWorkspaceResidenceTypeLabelTests
{
    [Fact]
    public void BusinessTripAddressType_display_uses_localized_lodging_label_not_enum_name()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tk-TM");
            var profile = new ApplicationProfile { RequireBusinessTripAddress = true };
            var application = new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BusinessTripAddressType = ResidenceType.Lodging,
            };

            var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
            var typeField = Assert.Single(
                fields,
                f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddressType);

            Assert.Equal(
                ApplicationProfileLocalization.Msg("ApplicationProfile.Site.Lodging"),
                typeField.DisplayValue);
            Assert.Equal("Umumy ýaşaýyş jaýy", typeField.DisplayValue);
            Assert.NotEqual(nameof(ResidenceType.Lodging), typeField.DisplayValue);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void InvitationAddressType_display_uses_localized_hotel_label()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var profile = new ApplicationProfile
            {
                RequireInvitationAddress = true,
                RequireInvitationPlace = true,
            };
            var application = new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                InvitationAddress = new InvitationAddress { Type = ResidenceType.Hotel },
            };

            var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
            var typeField = Assert.Single(
                fields,
                f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAddressType);

            Assert.Equal(
                ApplicationProfileLocalization.Msg("ApplicationProfile.Site.Hotel"),
                typeField.DisplayValue);
            Assert.Equal("Hotel", typeField.DisplayValue);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(ResidenceType.Hospital, "ApplicationProfile.Site.Hospital")]
    [InlineData(ResidenceType.Other, "ApplicationProfile.Site.Other")]
    [InlineData(ResidenceType.PrivateHouse, "ApplicationProfile.Site.PrivateHouse")]
    public void InvitationAddressType_display_covers_remaining_site_kinds(ResidenceType type, string msgKey)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var profile = new ApplicationProfile
            {
                RequireInvitationAddress = true,
                RequireInvitationPlace = true,
            };
            var application = new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                InvitationAddress = new InvitationAddress { Type = type },
            };

            var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
            var typeField = Assert.Single(
                fields,
                f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAddressType);

            Assert.Equal(ApplicationProfileLocalization.Msg(msgKey), typeField.DisplayValue);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
