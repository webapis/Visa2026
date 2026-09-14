using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstancePersonResolverTests
{
    [Fact]
    public void EnsureApplicationProfileLoaded_TrueWhenProfileAlreadySet()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { PersonVisaLastCount = 2, RequirePersonVisa = true },
        };

        Assert.True(ApplicationProfileInstancePersonResolver.EnsureApplicationProfileLoaded(null, app));
        Assert.Equal(2, ApplicationProfilePersonLastCount.For(app, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void LastCount_DefaultsToOneWhenProfileNavigationMissing()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ShowCurrentVisa = true },
        };

        Assert.Equal(1, ApplicationProfilePersonLastCount.For(app, ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void EnsureApplicationProfileLoaded_FalseWhenObjectSpaceAndProfileMissing()
    {
        Assert.False(ApplicationProfileInstancePersonResolver.EnsureApplicationProfileLoaded(
            null, new ApplicationProfileInstance()));
    }

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
    public void PersonLastCount_ClampZeroAndAboveMax()
    {
        Assert.Equal(1, ApplicationProfilePersonLastCount.Clamp(0));
        Assert.Equal(1, ApplicationProfilePersonLastCount.Clamp(-4));
        Assert.Equal(3, ApplicationProfilePersonLastCount.Clamp(9));
        Assert.Equal(2, ApplicationProfilePersonLastCount.Clamp(2));
    }

    [Fact]
    public void CollectMissingAutoLinks_AddsSecondPassportWhenLastCountIsTwo()
    {
        var stickyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonPassport = true,
                PersonPassportLastCount = 2,
            },
        };
        var existing = new List<ApplicationProfileInstancePersonResolvedLink>
        {
            new() { LinkKind = ApplicationProfileInstancePersonLinkKind.Passport, LinkedObjectId = stickyId },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Passport, new Passport { ID = stickyId }),
            (ApplicationProfileInstancePersonLinkKind.Passport, new Passport { ID = secondId }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(app, existing, candidates);

        Assert.Single(missing);
        Assert.Equal(secondId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void CollectMissingAutoLinks_AddsSecondVisaWhenLastCountIsTwo()
    {
        var stickyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonVisa = true,
                PersonVisaLastCount = 2,
            },
        };
        var existing = new List<ApplicationProfileInstancePersonResolvedLink>
        {
            new() { LinkKind = ApplicationProfileInstancePersonLinkKind.Visa, LinkedObjectId = stickyId },
        };
        var today = DateTime.Today;
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Visa, new Visa
            {
                ID = stickyId,
                StartDate = today.AddMonths(-8),
                ExpirationDate = today.AddMonths(10),
            }),
            (ApplicationProfileInstancePersonLinkKind.Visa, new Visa
            {
                ID = secondId,
                StartDate = today.AddMonths(-2),
                ExpirationDate = today.AddMonths(6),
            }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(app, existing, candidates);

        Assert.Single(missing);
        Assert.Equal(secondId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void CollectMissingAutoLinks_AddsSecondWorkPermitWhenLastCountIsTwo()
    {
        var stickyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var secondId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonWorkPermitItem = true,
                PersonWorkPermitItemLastCount = 2,
            },
        };
        var existing = new List<ApplicationProfileInstancePersonResolvedLink>
        {
            new() { LinkKind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem, LinkedObjectId = stickyId },
        };
        var today = DateTime.Today;
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.WorkPermitItem, new WorkPermitItem
            {
                ID = stickyId,
                StartDate = today.AddMonths(-8),
                ExpirationDate = today.AddMonths(10),
            }),
            (ApplicationProfileInstancePersonLinkKind.WorkPermitItem, new WorkPermitItem
            {
                ID = secondId,
                StartDate = today.AddMonths(-2),
                ExpirationDate = today.AddMonths(6),
            }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(app, existing, candidates);

        Assert.Single(missing);
        Assert.Equal(secondId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void CollectMissingAutoLinks_LinksWhatExistsWhenLastCountIsShort()
    {
        var onlyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonPassport = true,
                PersonPassportLastCount = 2,
            },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Passport, new Passport { ID = onlyId }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Single(missing);
        Assert.Equal(onlyId, missing[0].LinkedObjectId);
        Assert.True(ApplicationProfilePersonLastCount.For(app, ApplicationProfileInstancePersonLinkKind.Passport) > missing.Count);
    }

    [Fact]
    public void DecideEnsureResolvedLink_CreatesSecondWhenLastCountIsTwo()
    {
        var stickyId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var newerId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var existing = new[]
        {
            new ApplicationProfileInstancePersonResolvedLink
            {
                LinkKind = ApplicationProfileInstancePersonLinkKind.Passport,
                LinkedObjectId = stickyId,
            },
        };

        var decision = ApplicationProfileInstancePersonResolver.DecideEnsureResolvedLink(
            existing,
            ApplicationProfileInstancePersonLinkKind.Passport,
            newerId,
            lastCount: 2,
            out var emptyRow);

        Assert.Equal(ApplicationProfileInstancePersonResolver.EnsureResolvedLinkDecision.Create, decision);
        Assert.Null(emptyRow);
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

    [Fact]
    public void DecideEnsureResolvedLink_CreatesWhenNone()
    {
        var newId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var decision = ApplicationProfileInstancePersonResolver.DecideEnsureResolvedLink(
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            ApplicationProfileInstancePersonLinkKind.Salary,
            newId,
            out var emptyRow);

        Assert.Equal(ApplicationProfileInstancePersonResolver.EnsureResolvedLinkDecision.Create, decision);
        Assert.Null(emptyRow);
    }

    [Fact]
    public void DecideEnsureResolvedLink_FillsEmptyLinkedObjectId()
    {
        var newId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var empty = new ApplicationProfileInstancePersonResolvedLink
        {
            LinkKind = ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            LinkedObjectId = null,
        };

        var decision = ApplicationProfileInstancePersonResolver.DecideEnsureResolvedLink(
            new[] { empty },
            ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            newId,
            out var emptyRow);

        Assert.Equal(ApplicationProfileInstancePersonResolver.EnsureResolvedLinkDecision.FillEmpty, decision);
        Assert.Same(empty, emptyRow);
    }

    [Fact]
    public void MergeTrackedLinks_DoesNotAddSameVisaTwice()
    {
        var visaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var personId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var fromQuery = new[]
        {
            new ApplicationProfileInstancePersonResolvedLink
            {
                PersonId = personId,
                LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                LinkedObjectId = visaId,
            },
        };
        var application = new ApplicationProfileInstance
        {
            PersonResolvedLinks =
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    PersonId = personId,
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                    LinkedObjectId = visaId,
                },
            ],
        };

        var merged = ApplicationProfileInstancePersonResolver.MergeTrackedLinks(fromQuery, application, personId);

        Assert.Single(merged);
    }

    [Fact]
    public void DecideEnsureResolvedLink_DoesNotReplaceStickyId()
    {
        var stickyId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var newerId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var existing = new[]
        {
            new ApplicationProfileInstancePersonResolvedLink
            {
                LinkKind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                LinkedObjectId = stickyId,
            },
        };

        var decision = ApplicationProfileInstancePersonResolver.DecideEnsureResolvedLink(
            existing,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            newerId,
            out var emptyRow);

        Assert.Equal(ApplicationProfileInstancePersonResolver.EnsureResolvedLinkDecision.None, decision);
        Assert.Null(emptyRow);
    }

    [Fact]
    public void ShouldDropUsedInvitationLinkOnCancelRelink_TrueOnlyOnCancelInvitationProfile()
    {
        var cancel = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { CancelInvitations = true },
        };
        var other = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                CancelInvitations = false,
                RequirePersonInvitationItem = true,
            },
        };

        Assert.True(ApplicationProfileInstancePersonResolver.ShouldDropUsedInvitationLinkOnCancelRelink(cancel));
        Assert.False(ApplicationProfileInstancePersonResolver.ShouldDropUsedInvitationLinkOnCancelRelink(other));
    }

    [Fact]
    public void CollectMissingAutoLinks_SkipsUsedInvitationOnCancelInvitation()
    {
        var unusedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var usedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var live = new Invitation
        {
            IssuedDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddMonths(6),
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                CancelInvitations = true,
                RequirePersonInvitationItem = true,
                PersonInvitationItemLastCount = 2,
            },
        };
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.InvitationItem, new InvitationItem
            {
                ID = unusedId,
                Invitation = live,
            }),
            (ApplicationProfileInstancePersonLinkKind.InvitationItem, new InvitationItem
            {
                ID = usedId,
                Invitation = live,
                IssuedVisa = new Visa(),
            }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Single(missing);
        Assert.Equal(unusedId, missing[0].LinkedObjectId);
    }
}
