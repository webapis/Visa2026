using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstancePersonValidItemsTests
{
    private static readonly DateTime Today = DateTime.Today;

    [Fact]
    public void CanLinkPassport_TrueWhenExpired()
    {
        var passport = new Passport { ExpirationDate = Today.AddDays(-1) };

        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkPassport(passport));
    }

    [Fact]
    public void CanLinkPassport_TrueWhenExpiresToday()
    {
        var passport = new Passport { ExpirationDate = Today };

        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkPassport(passport));
    }

    [Fact]
    public void ResolvePassport_PicksLatestByIssueDateEvenIfExpired()
    {
        var person = new Person();
        var expiredCurrent = new Passport
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Person = person,
            IssueDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        var olderValid = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            IssueDate = Today.AddYears(-2),
            ExpirationDate = Today.AddYears(1),
        };
        person.Passports.Add(expiredCurrent);
        person.Passports.Add(olderValid);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolvePassport(person);

        Assert.Same(expiredCurrent, resolved);
    }

    [Fact]
    public void ResolvePassports_TakesLastTwoByIssueDateIncludingExpired()
    {
        var person = new Person();
        var newestExpired = new Passport
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Person = person,
            IssueDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        var previous = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            IssueDate = Today.AddYears(-2),
            ExpirationDate = Today.AddYears(-1),
        };
        var oldest = new Passport
        {
            ID = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Person = person,
            IssueDate = Today.AddYears(-5),
            ExpirationDate = Today.AddYears(-3),
        };
        person.Passports.Add(newestExpired);
        person.Passports.Add(previous);
        person.Passports.Add(oldest);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolvePassports(person, 2);

        Assert.Equal(2, resolved.Count);
        Assert.Same(newestExpired, resolved[0]);
        Assert.Same(previous, resolved[1]);
    }

    [Fact]
    public void CanLinkVisa_FalseWhenExpiredCancelledChangedOrNotStarted()
    {
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(CreateVisa(
            start: Today.AddDays(-10),
            expiration: Today.AddDays(-1))));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(CreateVisa(
            start: Today.AddDays(-10),
            expiration: Today.AddYears(1),
            cancelled: true)));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(CreateVisa(
            start: Today.AddDays(-10),
            expiration: Today.AddYears(1),
            changed: true)));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(CreateVisa(
            start: Today.AddDays(5),
            expiration: Today.AddYears(1))));
    }

    [Fact]
    public void ResolveVisa_SkipsExpiredAndPicksValid()
    {
        var person = new Person();
        var passport = new Passport
        {
            Person = person,
            IssueDate = Today.AddYears(-1),
            ExpirationDate = Today.AddYears(1),
            Visas = new ObservableCollection<Visa>(),
        };
        person.Passports.Add(passport);

        var expired = CreateVisa(Today.AddDays(-20), Today.AddDays(-1));
        expired.ID = Guid.Parse("11111111-1111-1111-1111-111111111111");
        expired.Passport = passport;
        var valid = CreateVisa(Today.AddDays(-30), Today.AddYears(1));
        valid.ID = Guid.Parse("22222222-2222-2222-2222-222222222222");
        valid.Passport = passport;
        passport.Visas.Add(expired);
        passport.Visas.Add(valid);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveVisa(person);

        Assert.Same(valid, resolved);
    }

    [Fact]
    public void CanLinkMedicalRecord_FalseWhenExpired()
    {
        var record = new MedicalRecord { IssueDate = Today.AddYears(-1) };
        SetProtectedDate(record, nameof(MedicalRecord.ExpirationDate), Today.AddDays(-1));

        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkMedicalRecord(record));
    }

    [Fact]
    public void ResolveMedical_PicksLatestNonExpired()
    {
        var person = new Person();
        var expired = new MedicalRecord
        {
            ID = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Person = person,
            IssueDate = Today.AddMonths(-1),
        };
        SetProtectedDate(expired, nameof(MedicalRecord.ExpirationDate), Today.AddDays(-1));
        var valid = new MedicalRecord
        {
            ID = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Person = person,
            IssueDate = Today.AddYears(-1),
        };
        SetProtectedDate(valid, nameof(MedicalRecord.ExpirationDate), Today.AddYears(1));
        person.MedicalRecords.Add(expired);
        person.MedicalRecords.Add(valid);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveMedical(person);

        Assert.Same(valid, resolved);
    }

    [Fact]
    public void CanLinkInvitationItem_FalseWhenExpiredCancelledChangedOrUsed()
    {
        var invitation = new Invitation
        {
            IssuedDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            new InvitationItem { Invitation = invitation }));

        var live = new Invitation
        {
            IssuedDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddMonths(1),
        };
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            WithCompletedFamily(new InvitationItem { Invitation = live }, ApplicationProfileActionFamily.Cancellation)));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            WithCompletedFamily(new InvitationItem { Invitation = live }, ApplicationProfileActionFamily.Change)));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            new InvitationItem { Invitation = live, IssuedVisa = new Visa() }));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            new InvitationItem { Invitation = live }));
    }

    [Fact]
    public void CanLinkWorkPermitItem_FalseWhenExpiredOrCancelled()
    {
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(new WorkPermitItem
        {
            StartDate = Today.AddMonths(-6),
            ExpirationDate = Today.AddDays(-1),
        }));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(
            WithCompletedFamily(new WorkPermitItem
            {
                StartDate = Today.AddMonths(-6),
                ExpirationDate = Today.AddYears(1),
            }, ApplicationProfileActionFamily.Cancellation)));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(new WorkPermitItem
        {
            StartDate = Today.AddMonths(-6),
            ExpirationDate = Today.AddYears(1),
        }));
    }

    [Fact]
    public void ResolveVisas_TakesLastTwoValidSkipsExpiredAndCancelled()
    {
        var person = new Person();
        var passport = new Passport { Person = person };
        person.Passports.Add(passport);
        var newestExpired = CreateVisa(Today.AddMonths(-1), Today.AddDays(-1));
        newestExpired.ID = Guid.Parse("11111111-1111-1111-1111-111111111111");
        passport.Visas.Add(newestExpired);
        var validNewer = CreateVisa(Today.AddMonths(-4), Today.AddMonths(8));
        validNewer.ID = Guid.Parse("22222222-2222-2222-2222-222222222222");
        passport.Visas.Add(validNewer);
        var validOlder = CreateVisa(Today.AddMonths(-10), Today.AddMonths(2));
        validOlder.ID = Guid.Parse("33333333-3333-3333-3333-333333333333");
        passport.Visas.Add(validOlder);
        var cancelled = CreateVisa(Today.AddMonths(-2), Today.AddYears(1), cancelled: true);
        cancelled.ID = Guid.Parse("44444444-4444-4444-4444-444444444444");
        passport.Visas.Add(cancelled);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveVisas(person, 2);

        Assert.Equal(2, resolved.Count);
        Assert.Same(validNewer, resolved[0]);
        Assert.Same(validOlder, resolved[1]);
    }

    [Fact]
    public void ResolveInvitationItems_TakesLastTwoValidSkipsExpiredAndUsed()
    {
        var person = new Person();
        var expired = new InvitationItem
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Person = person,
            Invitation = new Invitation { IssuedDate = Today.AddMonths(-1), ExpirationDate = Today.AddDays(-1) },
        };
        var used = new InvitationItem
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            Invitation = new Invitation { IssuedDate = Today.AddMonths(-2), ExpirationDate = Today.AddMonths(6) },
            IssuedVisa = new Visa(),
        };
        var validNewer = new InvitationItem
        {
            ID = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Person = person,
            Invitation = new Invitation { IssuedDate = Today.AddMonths(-3), ExpirationDate = Today.AddMonths(4) },
        };
        var validOlder = new InvitationItem
        {
            ID = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Person = person,
            Invitation = new Invitation { IssuedDate = Today.AddMonths(-8), ExpirationDate = Today.AddMonths(1) },
        };
        person.InvitationItems.Add(expired);
        person.InvitationItems.Add(used);
        person.InvitationItems.Add(validNewer);
        person.InvitationItems.Add(validOlder);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveInvitationItems(person, 2);

        Assert.Equal(2, resolved.Count);
        Assert.Same(validNewer, resolved[0]);
        Assert.Same(validOlder, resolved[1]);
    }

    [Fact]
    public void ResolveWorkPermitItems_TakesLastTwoValidSkipsExpired()
    {
        var person = new Person();
        var expired = new WorkPermitItem
        {
            ID = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            Person = person,
            StartDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        var validNewer = new WorkPermitItem
        {
            ID = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Person = person,
            StartDate = Today.AddMonths(-4),
            ExpirationDate = Today.AddMonths(8),
        };
        var validOlder = new WorkPermitItem
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"),
            Person = person,
            StartDate = Today.AddMonths(-10),
            ExpirationDate = Today.AddMonths(2),
        };
        person.WorkPermitItems.Add(expired);
        person.WorkPermitItems.Add(validNewer);
        person.WorkPermitItems.Add(validOlder);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItems(person, 2);

        Assert.Equal(2, resolved.Count);
        Assert.Same(validNewer, resolved[0]);
        Assert.Same(validOlder, resolved[1]);
    }

    [Fact]
    public void CollectMissingAutoLinks_LinksValidVisaInvitationAndWorkPermitOnly()
    {
        var visaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var invitationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var wpId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var expiredVisaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var expiredWpId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var usedInvitationId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                RequirePersonVisa = true,
                PersonVisaLastCount = 2,
                RequirePersonInvitationItem = true,
                PersonInvitationItemLastCount = 2,
                RequirePersonWorkPermitItem = true,
                PersonWorkPermitItemLastCount = 2,
            },
        };
        var liveInvitation = new Invitation
        {
            IssuedDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddMonths(6),
        };
        var validVisa = CreateVisa(Today.AddMonths(-2), Today.AddMonths(6));
        validVisa.ID = visaId;
        var expiredVisa = CreateVisa(Today.AddMonths(-1), Today.AddDays(-1));
        expiredVisa.ID = expiredVisaId;
        var candidates = new List<(ApplicationProfileInstancePersonLinkKind, object?)>
        {
            (ApplicationProfileInstancePersonLinkKind.Visa, validVisa),
            (ApplicationProfileInstancePersonLinkKind.Visa, expiredVisa),
            (ApplicationProfileInstancePersonLinkKind.InvitationItem, new InvitationItem
            {
                ID = invitationId,
                Invitation = liveInvitation,
            }),
            (ApplicationProfileInstancePersonLinkKind.InvitationItem, new InvitationItem
            {
                ID = usedInvitationId,
                Invitation = liveInvitation,
                IssuedVisa = new Visa(),
            }),
            (ApplicationProfileInstancePersonLinkKind.WorkPermitItem, new WorkPermitItem
            {
                ID = wpId,
                StartDate = Today.AddMonths(-6),
                ExpirationDate = Today.AddYears(1),
            }),
            (ApplicationProfileInstancePersonLinkKind.WorkPermitItem, new WorkPermitItem
            {
                ID = expiredWpId,
                StartDate = Today.AddMonths(-6),
                ExpirationDate = Today.AddDays(-1),
            }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Equal(3, missing.Count);
        Assert.Contains(missing, m => m.Kind == ApplicationProfileInstancePersonLinkKind.Visa && m.LinkedObjectId == visaId);
        Assert.Contains(missing, m => m.Kind == ApplicationProfileInstancePersonLinkKind.InvitationItem && m.LinkedObjectId == invitationId);
        Assert.Contains(missing, m => m.Kind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem && m.LinkedObjectId == wpId);
    }

    [Fact]
    public void ResolveWorkPermitItem_SkipsExpired()
    {
        var person = new Person();
        var expired = new WorkPermitItem
        {
            ID = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            Person = person,
            StartDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        var valid = new WorkPermitItem
        {
            ID = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Person = person,
            StartDate = Today.AddMonths(-8),
            ExpirationDate = Today.AddMonths(4),
        };
        person.WorkPermitItems.Add(expired);
        person.WorkPermitItems.Add(valid);

        var resolved = ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person);

        Assert.Same(valid, resolved);
    }

    [Fact]
    public void CanLinkBorderZoneItem_FalseWhenCancelledExpiredOrMissingParent()
    {
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkBorderZoneItem(new BorderZoneItem()));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkBorderZoneItem(
            WithCompletedFamily(new BorderZoneItem
            {
                BorderZone = new BorderZone(),
            }, ApplicationProfileActionFamily.Cancellation)));

        var expiredZone = new BorderZone();
        SetProtectedDate(expiredZone, nameof(BorderZone.ExpirationDate), Today.AddDays(-1));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkBorderZoneItem(new BorderZoneItem
        {
            BorderZone = expiredZone,
        }));

        var liveZone = new BorderZone();
        SetProtectedDate(liveZone, nameof(BorderZone.ExpirationDate), Today.AddMonths(1));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkBorderZoneItem(new BorderZoneItem
        {
            BorderZone = liveZone,
        }));
    }

    [Fact]
    public void CollectMissingAutoLinks_AllowsExpiredPassport()
    {
        var expiredId = Guid.NewGuid();
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };
        var candidates = new[]
        {
            (ApplicationProfileInstancePersonLinkKind.Passport,
                (object?)new Passport { ID = expiredId, ExpirationDate = Today.AddDays(-1) }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Single(missing);
        Assert.Equal(expiredId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void CollectMissingAutoLinks_AllowsExpiredPassportDuringDataImport()
    {
        var expiredId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };
        var candidates = new[]
        {
            (ApplicationProfileInstancePersonLinkKind.Passport,
                (object?)new Passport { ID = expiredId, ExpirationDate = Today.AddDays(-1) }),
        };

        using var scope = MigrationImportContext.BeginDataImportScope();
        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Single(missing);
        Assert.Equal(ApplicationProfileInstancePersonLinkKind.Passport, missing[0].Kind);
        Assert.Equal(expiredId, missing[0].LinkedObjectId);
    }

    [Fact]
    public void ResolvePassport_UsesCurrentIncludingExpiredDuringDataImport()
    {
        var person = new Person();
        var expiredCurrent = new Passport
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Person = person,
            IssueDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddDays(-1),
        };
        var olderValid = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            IssueDate = Today.AddYears(-2),
            ExpirationDate = Today.AddYears(1),
        };
        person.Passports.Add(expiredCurrent);
        person.Passports.Add(olderValid);

        using var scope = MigrationImportContext.BeginDataImportScope();
        var resolved = ApplicationProfileInstancePersonValidItems.ResolvePassport(person);

        Assert.Same(expiredCurrent, resolved);
    }

    private static Visa CreateVisa(DateTime start, DateTime expiration, bool cancelled = false, bool changed = false)
    {
        var visa = new Visa
        {
            IssueDate = start,
            StartDate = start,
            ExpirationDate = expiration,
        };
        if (cancelled)
            WithCompletedFamily(visa, ApplicationProfileActionFamily.Cancellation);
        else if (changed)
            WithCompletedFamily(visa, ApplicationProfileActionFamily.Change);
        return visa;
    }

    private static T WithCompletedFamily<T>(T document, ApplicationProfileActionFamily family)
        where T : class
    {
        var instance = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = family },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        };
        switch (document)
        {
            case InvitationItem invitationItem:
                invitationItem.ApplicationProfileInstances.Add(instance);
                break;
            case WorkPermitItem workPermitItem:
                workPermitItem.ApplicationProfileInstances.Add(instance);
                break;
            case BorderZoneItem borderZoneItem:
                borderZoneItem.ApplicationProfileInstances.Add(instance);
                break;
            case Visa visa:
                visa.ApplicationProfileInstances.Add(instance);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(document));
        }

        return document;
    }

    private static void SetProtectedDate(object target, string propertyName, DateTime? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}