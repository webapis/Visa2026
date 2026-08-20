using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceCaseHeaderFieldsHelperTests
{
    [Fact]
    public void Build_IncludesOnlyProfileUseFields()
    {
        var profile = new ApplicationProfile
        {
            RequireVisaType = true,
            RequireVisaPeriod = true,
            RequireProject = true,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Equal(3, fields.Count);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaPeriod);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Project);
        Assert.DoesNotContain(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Urgency);
    }

    [Fact]
    public void Build_IncludesBusinessTripAddressWhenRequired()
    {
        var profile = new ApplicationProfile { RequireBusinessTripAddress = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddress);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Lookup, fields.Single().Kind);
    }

    [Fact]
    public void Build_IncludesPurposeWhenRequired()
    {
        var profile = new ApplicationProfile { RequirePurpose = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            Purpose = "Business trip reason",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Purpose);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Text, field.Kind);
        Assert.Equal("Business trip reason", field.DisplayValue);
    }

    [Fact]
    public void Build_IncludesBorderZoneAsCommaSeparatedMultiSelectWhenRequired()
    {
        var profile = new ApplicationProfile { RequireBorderZone = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            BorderZoneLocation = "Zone A, Zone B",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect, field.Kind);
        Assert.Equal("Zone A, Zone B", field.Value);
    }

    [Fact]
    public void Build_EmptyWhenProfileHasNoUseFields()
    {
        var profile = new ApplicationProfile();
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Empty(fields);
    }

    [Fact]
    public void Build_ShowsEmptyDisplayAsDash()
    {
        var profile = new ApplicationProfile { RequireVisaType = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var field = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null).Single();

        Assert.Equal("—", field.DisplayValue);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Lookup, field.Kind);
    }
}
