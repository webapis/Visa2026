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

        Assert.Equal(5, fields.Count);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate);
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
        Assert.Equal(
            ApplicationWorkspaceCaseHeaderFieldKind.Lookup,
            Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddress).Kind);
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
    public void Build_IncludesWorkPermitLocationAsCommaSeparatedMultiSelectWhenRequired()
    {
        var profile = new ApplicationProfile { RequireWorkPermitLocation = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            MovementPermitLocation = "Ashgabat, Mary",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.WorkPermitLocation);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect, field.Kind);
        Assert.Equal("Ashgabat, Mary", field.Value);
    }

    [Fact]
    public void Build_AlwaysIncludesApplicationNumberAndDate()
    {
        var profile = new ApplicationProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            FullApplicationNumber = "8/-007",
            ApplicationDate = new DateTime(2024, 8, 25),
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Equal(2, fields.Count);
        var number = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.ShortText, number.Kind);
        Assert.Equal("8/-007", number.DisplayValue);
        var date = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Date, date.Kind);
        Assert.Equal("25.08.2024", date.DisplayValue);
    }

    [Fact]
    public void Build_ShowsEmptyDisplayAsDash()
    {
        var profile = new ApplicationProfile { RequireVisaType = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var field = Assert.Single(
            ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null),
            item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType);

        Assert.Equal("—", field.DisplayValue);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Lookup, field.Kind);
    }

    [Fact]
    public void TrySetInstanceNumber_ParsesFullNumberAndMarksManualEntry()
    {
        var application = new ApplicationProfileInstance();

        var ok = ApplicationWorkspaceCaseHeaderFieldsHelper.TrySetInstanceNumber(application, "8/-007", out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("8/-007", application.FullApplicationNumber);
        Assert.Equal("8", application.AppNumberPrefix);
        Assert.Equal("007", application.ApplicationNumber);
        Assert.True(application.IsManualEntry);
    }

    [Fact]
    public void TrySetInstanceDate_SetsDateYearAndMonth()
    {
        var application = new ApplicationProfileInstance();

        var ok = ApplicationWorkspaceCaseHeaderFieldsHelper.TrySetInstanceDate(application, "2024-08-25", out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new DateTime(2024, 8, 25), application.ApplicationDate);
        Assert.Equal(2024, application.Year);
        Assert.Equal(8, application.Month);
    }
}
