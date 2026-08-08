using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileApplicabilityHelperTests
{
    [Fact]
    public void IsProfileSelectable_RejectsInactiveProfiles()
    {
        var profile = new ApplicationProfile { IsActive = false, ProgressRoute = ApplicationProgressRouteKind.ViaMinistries };

        Assert.False(ApplicationProfileApplicabilityHelper.IsProfileSelectable(profile, null, null));
    }

    [Fact]
    public void IsProfileSelectable_FiltersByProgressRoute()
    {
        var profile = new ApplicationProfile
        {
            IsActive = true,
            ProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
        };

        Assert.True(ApplicationProfileApplicabilityHelper.IsProfileSelectable(
            profile,
            null,
            ApplicationProgressRouteKind.ViaMinistries));

        Assert.False(ApplicationProfileApplicabilityHelper.IsProfileSelectable(
            profile,
            null,
            ApplicationProgressRouteKind.DirectToMigrationService));
    }

    [Fact]
    public void IsProfileSelectable_AllowsEmptyApplicabilityCriteria()
    {
        var profile = new ApplicationProfile
        {
            IsActive = true,
            ApplicabilityCriteria = null,
        };

        Assert.True(ApplicationProfileApplicabilityHelper.IsProfileSelectable(profile, null, null));
    }

    [Fact]
    public void FormatActionFamily_ReturnsReadableLabels()
    {
        Assert.Equal("Issuance", ApplicationProfilePickerDisplayHelper.FormatActionFamily(ApplicationProfileActionFamily.Issuance));
        Assert.Equal("Business trip", ApplicationProfilePickerDisplayHelper.FormatActionFamily(ApplicationProfileActionFamily.BusinessTrip));
    }
}
