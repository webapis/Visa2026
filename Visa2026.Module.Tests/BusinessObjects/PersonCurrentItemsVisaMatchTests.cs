using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class PersonCurrentItemsVisaMatchTests
{
    private static readonly DateTime Today = DateTime.Today;

    [Fact]
    public void GetCurrentVisa_IncludesFutureStart_PrefersInForce()
    {
        var person = new Person();
        var passport = new Passport { Person = person, Visas = new ObservableCollection<Visa>() };
        person.Passports.Add(passport);
        var current = MakeVisa(passport, "CUR", Today.AddMonths(-2), Today.AddMonths(6));
        var future = MakeVisa(passport, "FUT", Today.AddMonths(2), Today.AddMonths(10));
        passport.Visas.Add(current);
        passport.Visas.Add(future);

        Assert.Same(current, PersonCurrentItems.GetCurrentVisa(person));
    }

    [Fact]
    public void GetVisaClosestToWorkPermit_PrefersLargestOverlap()
    {
        var person = new Person();
        var passport = new Passport { Person = person, Visas = new ObservableCollection<Visa>() };
        person.Passports.Add(passport);
        var overlapping = MakeVisa(passport, "OV", Today.AddMonths(-1), Today.AddMonths(5));
        var future = MakeVisa(passport, "FUT", Today.AddMonths(6), Today.AddMonths(12));
        passport.Visas.Add(overlapping);
        passport.Visas.Add(future);

        var wp = new WorkPermitItem
        {
            Person = person,
            StartDate = Today,
            ExpirationDate = Today.AddMonths(4),
        };

        Assert.Same(overlapping, PersonCurrentItems.GetVisaClosestToWorkPermit(person, wp));
    }

    [Fact]
    public void GetVisaClosestToWorkPermit_WhenNoOverlap_PicksNearestSpan()
    {
        var person = new Person();
        var passport = new Passport { Person = person, Visas = new ObservableCollection<Visa>() };
        person.Passports.Add(passport);
        var earlier = MakeVisa(passport, "EAR", Today.AddMonths(-10), Today.AddMonths(-6));
        earlier.ExpirationDate = Today.AddDays(-1); // expired — not linkable
        var futureNear = MakeVisa(passport, "NEAR", Today.AddMonths(1), Today.AddMonths(4));
        var futureFar = MakeVisa(passport, "FAR", Today.AddMonths(8), Today.AddMonths(12));
        passport.Visas.Add(futureNear);
        passport.Visas.Add(futureFar);

        var wp = new WorkPermitItem
        {
            Person = person,
            StartDate = Today.AddMonths(-2),
            ExpirationDate = Today.AddDays(10),
        };

        Assert.Same(futureNear, PersonCurrentItems.GetVisaClosestToWorkPermit(person, wp));
    }

    private static Visa MakeVisa(Passport passport, string number, DateTime start, DateTime expiration) =>
        new()
        {
            ID = Guid.NewGuid(),
            Passport = passport,
            VisaNumber = number,
            StartDate = start,
            IssueDate = start.AddDays(-7),
            ExpirationDate = expiration,
        };
}