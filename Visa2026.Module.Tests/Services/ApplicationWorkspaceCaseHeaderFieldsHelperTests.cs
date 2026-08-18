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
