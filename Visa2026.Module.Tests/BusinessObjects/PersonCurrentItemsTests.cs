using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class PersonCurrentItemsTests
{
    [Fact]
    public void GetCurrentPassport_PicksLatestIssueDateThenNewestId()
    {
        var older = new Passport
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            IssueDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        var newerSameDay = new Passport
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            IssueDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        var newest = new Passport
        {
            ID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            IssueDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        var person = new Person
        {
            Passports = new ObservableCollection<Passport> { older, newerSameDay, newest }
        };

        Assert.Same(newest, PersonCurrentItems.GetCurrentPassport(person));
    }

    [Fact]
    public void GetCurrentPassport_IgnoresNullIssueDate()
    {
        var withDate = new Passport
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            IssueDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        var withoutDate = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            IssueDate = null
        };
        var person = new Person
        {
            Passports = new ObservableCollection<Passport> { withoutDate, withDate }
        };

        Assert.Same(withDate, PersonCurrentItems.GetCurrentPassport(person));
    }

    [Fact]
    public void GetCurrentVisa_SkipsCancelledAndFutureStart()
    {
        var asOf = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified);
        var passport = new Passport { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        var cancelled = CreateVisa(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            issue: asOf.AddYears(-2),
            start: asOf.AddYears(-2),
            cancelled: true);
        var future = CreateVisa(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            issue: asOf.AddDays(-10),
            start: asOf.AddDays(10));
        var current = CreateVisa(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            issue: asOf.AddYears(-1),
            start: asOf.AddYears(-1));
        passport.Visas = new ObservableCollection<Visa> { cancelled, future, current };
        var person = new Person
        {
            Passports = new ObservableCollection<Passport> { passport }
        };

        Assert.Same(current, PersonCurrentItems.GetCurrentVisa(person, asOf));
        Assert.Same(current, PersonCurrentItems.GetCurrentVisa(passport, asOf));
    }

    [Fact]
    public void GetCurrentVisa_PrefersLaterStartThenLaterIssue()
    {
        var asOf = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified);
        var passport = new Passport { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        var older = CreateVisa(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            issue: asOf.AddYears(-2),
            start: asOf.AddYears(-2));
        var newer = CreateVisa(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            issue: asOf.AddMonths(-6),
            start: asOf.AddMonths(-3));
        passport.Visas = new ObservableCollection<Visa> { older, newer };
        var person = new Person
        {
            Passports = new ObservableCollection<Passport> { passport }
        };

        Assert.Same(newer, PersonCurrentItems.GetCurrentVisa(person, asOf));
    }

    [Fact]
    public void GetCurrentAddressOfResidence_PrefersStillValidThenFallsBack()
    {
        var asOf = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified);
        var expired = new AddressOfResidence
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ExpirationDate = asOf.AddDays(-1)
        };
        var validSoon = new AddressOfResidence
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ExpirationDate = asOf.AddDays(10)
        };
        var validOpen = new AddressOfResidence
        {
            ID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ExpirationDate = null
        };
        var person = new Person
        {
            AddressesOfResidence = new ObservableCollection<AddressOfResidence>
            {
                expired, validSoon, validOpen
            }
        };

        Assert.Same(validOpen, PersonCurrentItems.GetCurrentAddressOfResidence(person, asOf));

        person.AddressesOfResidence = new ObservableCollection<AddressOfResidence> { expired };
        Assert.Same(expired, PersonCurrentItems.GetCurrentAddressOfResidence(person, asOf));
    }

    [Fact]
    public void GetCurrentEducation_UsesParsedGraduationYear()
    {
        var older = new Education
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GraduationYear = "2010"
        };
        var invalidYear = new Education
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GraduationYear = "n/a"
        };
        var newer = new Education
        {
            ID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            GraduationYear = " 2022 "
        };
        var person = new Person
        {
            Educations = new ObservableCollection<Education> { older, invalidYear, newer }
        };

        Assert.Same(newer, PersonCurrentItems.GetCurrentEducation(person));
    }

    [Fact]
    public void GetCurrentPositionHistory_PrefersOpenPeriodByStartDate()
    {
        var asOf = DateTime.Today;
        var closed = new EmployeePositionHistory
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            StartDate = asOf.AddYears(-3),
            EndDate = asOf.AddYears(-1)
        };
        var openOlder = new EmployeePositionHistory
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            StartDate = asOf.AddYears(-2),
            EndDate = null
        };
        var openNewer = new EmployeePositionHistory
        {
            ID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            StartDate = asOf.AddMonths(-6),
            EndDate = asOf.AddDays(30)
        };
        var person = new Person
        {
            PositionHistory = new ObservableCollection<EmployeePositionHistory>
            {
                closed, openOlder, openNewer
            }
        };

        Assert.Same(openNewer, PersonCurrentItems.GetCurrentPositionHistory(person));
    }

    [Fact]
    public void ExtractPerson_And_ResolveFromSource_MapKnownSources()
    {
        var person = new Person { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        var passport = new Passport
        {
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Person = person,
            IssueDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        person.Passports = new ObservableCollection<Passport> { passport };
        var item = new ApplicationItem { Person = person };
        var visa = CreateVisa(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            issue: DateTime.Today.AddYears(-1),
            start: DateTime.Today.AddYears(-1));
        visa.Passport = passport;
        passport.Visas = new ObservableCollection<Visa> { visa };

        Assert.Same(person, PersonCurrentItems.ExtractPerson(item));
        Assert.Same(person, PersonCurrentItems.ExtractPerson(passport));
        Assert.Same(person, PersonCurrentItems.ExtractPerson(visa));
        Assert.Null(PersonCurrentItems.ExtractPerson("not-a-person-source"));

        Assert.Same(passport, PersonCurrentItems.ResolveFromSource(item, "CurrentPassport"));
        Assert.Same(visa, PersonCurrentItems.ResolveFromSource(passport, "CurrentVisa"));
        Assert.Null(PersonCurrentItems.ResolveFromSource(item, "UnknownProperty"));
    }

    private static Visa CreateVisa(Guid id, DateTime issue, DateTime start, bool cancelled = false)
    {
        var visa = new Visa { ID = id, IsCancelled = cancelled };
        visa.IssueDate = issue;
        visa.StartDate = start;
        return visa;
    }
}
