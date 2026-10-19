using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonList;

public class PersonListActiveDocumentsTests
{
    private static readonly DateTime AsOf = new(2026, 6, 15);

    [Fact]
    public void ListActiveVisas_StacksInForceVisas_LatestStartFirst()
    {
        var person = new Person();
        var passport = new Passport();
        passport.Visas.Add(Visa("A-EARLY", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "Single"));
        passport.Visas.Add(Visa("A-LATE", new DateTime(2026, 3, 1), new DateTime(2026, 9, 1), "Multiple"));
        passport.Visas.Add(Visa("A-EXPIRED", new DateTime(2025, 1, 1), new DateTime(2026, 6, 1), "Single"));
        passport.Visas.Add(Visa("A-FUTURE", new DateTime(2026, 7, 1), new DateTime(2026, 12, 1), "Single"));
        person.Passports.Add(passport);

        var rows = PersonCurrentItems.ListActiveVisas(person, AsOf);

        Assert.Equal(2, rows.Count);
        Assert.Equal("A-LATE", rows[0].Number);
        Assert.Equal("Multiple", rows[0].Detail);
        Assert.Equal("01.03.2026 – 01.09.2026", rows[0].Dates);
        Assert.Contains("/Visa:", rows[0].RecordKey);
        Assert.Equal("A-EARLY", rows[1].Number);
    }

    [Fact]
    public void ListActiveVisas_SkipsCancelled()
    {
        var person = new Person();
        var passport = new Passport();
        var visa = Visa("A-CANCEL", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "Single");
        visa.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });
        passport.Visas.Add(visa);
        person.Passports.Add(passport);

        Assert.Empty(PersonCurrentItems.ListActiveVisas(person, AsOf));
    }

    [Fact]
    public void ListActiveWorkPermits_KeepsTheLineInForceToday()
    {
        var person = new Person();
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-1",
            StartDate = new DateTime(2026, 2, 1),
            ExpirationDate = new DateTime(2026, 8, 1),
        });
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-OLD",
            StartDate = new DateTime(2025, 1, 1),
            ExpirationDate = new DateTime(2026, 1, 1),
        });

        var rows = PersonCurrentItems.ListActiveWorkPermits(person, AsOf);

        Assert.Single(rows);
        Assert.Equal("WP-1", rows[0].Number);
        Assert.Equal("01.02.2026 – 01.08.2026", rows[0].Dates);
        Assert.StartsWith("WorkPermitItem:", rows[0].RecordKey);
    }

    [Fact]
    public void ListActiveInvitations_UsesIssuedAndExpiration_AndSkipsCancelled()
    {
        var person = new Person();
        person.InvitationItems.Add(Invitation("INV-1", new DateTime(2026, 4, 1), new DateTime(2026, 7, 1), cancelled: false));
        person.InvitationItems.Add(Invitation("INV-OLD", new DateTime(2026, 1, 1), new DateTime(2026, 3, 1), cancelled: false));
        person.InvitationItems.Add(Invitation("INV-CANCEL", new DateTime(2026, 5, 1), new DateTime(2026, 8, 1), cancelled: true));

        var rows = PersonCurrentItems.ListActiveInvitations(person, AsOf);

        Assert.Single(rows);
        Assert.Equal("INV-1", rows[0].Number);
        Assert.Equal("01.04.2026 – 01.07.2026", rows[0].Dates);
        Assert.StartsWith("InvitationItem:", rows[0].RecordKey);
        Assert.Equal("INV-1", PersonCurrentItems.FormatActiveInvitationNumbers(person, AsOf));
    }

    private static Visa Visa(string number, DateTime start, DateTime expiration, string typeName) =>
        new()
        {
            VisaNumber = number,
            StartDate = start,
            IssueDate = start,
            ExpirationDate = expiration,
            VisaType = new VisaType { NameTm = typeName },
        };

    private static InvitationItem Invitation(string number, DateTime issued, DateTime expiration, bool cancelled)
    {
        var item = new InvitationItem
        {
            Invitation = new Invitation
            {
                InvitationNumber = number,
                IssuedDate = issued,
                ExpirationDate = expiration,
            },
        };
        if (cancelled)
        {
            item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
            {
                ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
                LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            });
        }

        return item;
    }
}
