using System;
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
    public void CanLinkPassport_FalseWhenExpired()
    {
        var passport = new Passport { ExpirationDate = Today.AddDays(-1) };

        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkPassport(passport));
    }

    [Fact]
    public void CanLinkPassport_TrueWhenExpiresToday()
    {
        var passport = new Passport { ExpirationDate = Today };

        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkPassport(passport));
    }

    [Fact]
    public void ResolvePassport_PicksLatestNonExpiredNotCurrentExpired()
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

        Assert.Same(olderValid, resolved);
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
            new InvitationItem { Invitation = live, IsCancelled = true }));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            new InvitationItem { Invitation = live, IsChanged = true }));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(
            new InvitationItem { Invitation = live, IsUsed = true }));
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
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(new WorkPermitItem
        {
            StartDate = Today.AddMonths(-6),
            ExpirationDate = Today.AddYears(1),
            IsCancelled = true,
        }));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(new WorkPermitItem
        {
            StartDate = Today.AddMonths(-6),
            ExpirationDate = Today.AddYears(1),
        }));
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
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkBorderZoneItem(new BorderZoneItem
        {
            BorderZone = new BorderZone(),
            IsCancelled = true,
        }));

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
    public void CollectMissingAutoLinks_SkipsExpiredPassportCandidate()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequirePersonPassport = true },
        };
        var candidates = new[]
        {
            (ApplicationProfileInstancePersonLinkKind.Passport,
                (object?)new Passport { ID = Guid.NewGuid(), ExpirationDate = Today.AddDays(-1) }),
        };

        var missing = ApplicationProfileInstancePersonResolver.CollectMissingAutoLinks(
            app,
            Array.Empty<ApplicationProfileInstancePersonResolvedLink>(),
            candidates);

        Assert.Empty(missing);
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
            IsCancelled = cancelled,
            IsChanged = changed,
        };
        return visa;
    }

    private static void SetProtectedDate(object target, string propertyName, DateTime? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}