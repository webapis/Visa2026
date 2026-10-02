#nullable enable

using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation stay fields added for Çakylyk Almak (ff2819af): region / city / other places
/// on case summary Build + TryApply.
/// </summary>
public class ApplicationWorkspaceInvitationStayHeaderFieldsTests
{
    [Fact]
    public void Build_includes_invitation_stay_fields_when_profile_requires_them()
    {
        var profile = new ApplicationProfile { RequireInvitationAddress = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var keys = fields.Select(f => f.Key).ToList();

        Assert.Contains(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion, keys);
        Assert.Contains(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity, keys);
        Assert.Contains(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAlternativeAddress, keys);
    }

    [Fact]
    public void Build_hides_invitation_stay_when_profile_does_not_require_them()
    {
        var profile = new ApplicationProfile { RequireInvitationAddress = false, RequireVisaType = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.DoesNotContain(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion);
        Assert.DoesNotContain(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity);
        Assert.DoesNotContain(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAlternativeAddress);
    }

    [Fact]
    public void Build_empty_region_and_city_are_Empty_but_other_places_is_Officer()
    {
        // Alternative address is always Officer fill so an empty catalog pick does not block
        // case-summary completeness; region/city still count as gaps.
        var profile = new ApplicationProfile { RequireInvitationAddress = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Empty,
            Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion).FillState);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Empty,
            Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity).FillState);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Officer,
            Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAlternativeAddress).FillState);
    }

    [Fact]
    public void Build_shows_stay_region_city_and_other_places_display()
    {
        var regionId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var altId = Guid.NewGuid();
        var profile = new ApplicationProfile { RequireInvitationAddress = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            InvitationAddress = new InvitationAddress
            {
                Region = new Region { ID = regionId, NameTm = "Ahal" },
                City = new City { ID = cityId, NameTm = "Änew" },
                AlternativeAddress = new AlternativeAddressesForInvitation
                {
                    ID = altId,
                    AddressLine = "Mary welaýaty, Türkmenabat",
                },
            },
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var region = Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion);
        var city = Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity);
        var alt = Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAlternativeAddress);

        Assert.Equal(regionId, region.SelectedId);
        Assert.Equal("Ahal", region.DisplayValue);
        Assert.Equal(cityId, city.SelectedId);
        Assert.Equal("Änew", city.DisplayValue);
        Assert.Equal(altId, alt.SelectedId);
        Assert.Equal("Mary welaýaty, Türkmenabat", alt.DisplayValue);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Officer, region.FillState);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Officer, city.FillState);
    }

    [Fact]
    public void TryApply_invitation_fields_hidden_when_profile_does_not_require_stay()
    {
        var profile = new ApplicationProfile { RequireInvitationAddress = false };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };
        var space = CreatingObjectSpaceStub.Create();

        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.TryApply(
            application,
            space,
            new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion,
                Value = Guid.NewGuid().ToString("D"),
            },
            out var error));
        Assert.Equal("That field is not required on this profile.", error);
        Assert.Null(application.InvitationAddress);
    }

    [Fact]
    public void TryApply_creates_InvitationAddress_and_sets_region()
    {
        var regionId = Guid.NewGuid();
        var region = new Region { ID = regionId, NameTm = "Ahal" };
        var profile = new ApplicationProfile { RequireInvitationAddress = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };
        var space = CreatingObjectSpaceStub.Create(stub => stub.Seed(region));

        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.TryApply(
            application,
            space,
            new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion,
                Value = regionId.ToString("D"),
            },
            out var error));
        Assert.Null(error);
        Assert.NotNull(application.InvitationAddress);
        Assert.Same(region, application.InvitationAddress!.Region);
        Assert.Same(application, application.InvitationAddress.ApplicationProfileInstance);
    }

    [Fact]
    public void TryApply_city_clears_when_region_changes_to_mismatch()
    {
        var regionA = new Region { ID = Guid.NewGuid(), NameTm = "Ahal" };
        var regionB = new Region { ID = Guid.NewGuid(), NameTm = "Mary" };
        var city = new City { ID = Guid.NewGuid(), NameTm = "Änew", Region = regionA };
        var profile = new ApplicationProfile { RequireInvitationAddress = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            InvitationAddress = new InvitationAddress
            {
                Region = regionA,
                City = city,
            },
        };
        var space = CreatingObjectSpaceStub.Create(stub =>
        {
            stub.Seed(regionA, regionB);
            stub.Seed(city);
        });

        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.TryApply(
            application,
            space,
            new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationRegion,
                Value = regionB.ID.ToString("D"),
            },
            out _));

        Assert.Same(regionB, application.InvitationAddress!.Region);
        Assert.Null(application.InvitationAddress.City);
    }

    [Fact]
    public void TryOpenNew_returns_false_when_application_is_null()
    {
        Assert.False(AlternativeAddressCatalogOpenHelper.TryOpenNew(null!));
    }
}
