using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstancePersonResolverTests
{
    [Fact]
    public void IsAutoLinkEnabled_RespectsRequirePersonPassport()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };

        Assert.True(ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(app, ApplicationProfileInstancePersonLinkKind.Passport));
    }

    [Fact]
    public void IsAutoLinkEnabled_FalseWhenToggleOff()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonPassport = false,
                RequirePersonVisa = false,
            },
        };

        Assert.False(ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(app, ApplicationProfileInstancePersonLinkKind.Passport));
        Assert.False(ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(app, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void IsAutoLinkEnabled_FallsBackToTypeWhenProfileMissing()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ShowCurrentVisa = true },
        };

        Assert.True(ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(app, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void CollectMissingAutoLinks_SkipsExistingStickyEvenIfCandidateDiffers()
    {
        var stickyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };
        var existing = new List<ApplicationProfileInstancePersonResolvedLink>
        {
            new() { LinkKind = ApplicationProfileInstancePersonLinkKind.Passport, LinkedObjectId = stickyId },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Passport, new Passport { ID = newerId }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(app, existing, candidates);

        Assert.Empty(missing);
    }

    [Fact]
    public void CollectMissingAutoLinks_SkipsWhenToggleOffEvenIfCandidateExists()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonVisa = false },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Visa, new Visa { ID = Guid.NewGuid() }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Empty(missing);
    }

    [Fact]
    public void CollectMissingAutoLinks_AddsWhenRequiredAndMissing()
    {
        var passportId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonPassport = true,
                RequirePersonEducation = true,
            },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Passport, new Passport { ID = passportId }),
            (ApplicationProfileInstancePersonLinkKind.Education, null),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            new ObservableCollection<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Single(missing);
        Assert.Equal(ApplicationProfileInstancePersonLinkKind.Passport, missing[0].Kind);
        Assert.Equal(passportId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void CollectMissingAutoLinks_KeepsExistingWhenToggleLaterOff()
    {
        var stickyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonEducation = false },
        };
        var existing = new[]
        {
            new ApplicationProfileInstancePersonResolvedLink
            {
                LinkKind = ApplicationProfileInstancePersonLinkKind.Education,
                LinkedObjectId = stickyId,
            },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Education, new Education { ID = Guid.NewGuid() }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(app, existing, candidates);

        Assert.Empty(missing);
    }
}
