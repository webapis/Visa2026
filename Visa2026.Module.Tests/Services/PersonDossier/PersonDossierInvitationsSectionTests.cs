using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierInvitationsSectionTests
{
    [Fact]
    public void Lists_every_invitation_newest_expiry_first_with_issuing_application()
    {
        var olderApp = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2025-0410",
        };
        var newerApp = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "CE2026-0188",
        };
        var person = new Person();
        person.InvitationItems.Add(Item("1411/25", new DateTime(2025, 6, 1), olderApp, visa: null));
        person.InvitationItems.Add(Item("1884/26", new DateTime(2027, 3, 30), newerApp, visa: null));

        var section = PersonDossierResolver.BuildInvitations(person, usedInvitationItemIds: null);

        Assert.Equal("invitations", section.SectionId);
        Assert.Equal(
            new[] { "1884/26", "1411/25" },
            section.Records.Select(record => record.Cells[0]).ToArray());
        Assert.Equal(newerApp.ID, section.Records[0].ApplicationLinkId);
        Assert.Equal("CE2026-0188", section.Records[0].Cells[6]);
        Assert.Equal("\u2014", section.Records[0].Cells[5]);
        Assert.False(section.Records[0].AppendIssuedVisaBesidePassport);
        Assert.StartsWith("Valid to", section.Records[0].StatusLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Used_row_names_the_issued_visa_and_asks_for_it_beside_the_passport()
    {
        var visa = new Visa
        {
            VisaNumber = "A1731526",
            ExpirationDate = DateTime.Today.AddDays(45),
        };
        var person = new Person();
        person.InvitationItems.Add(Item(
            "1602/26",
            new DateTime(2026, 6, 14),
            application: null,
            visa,
            visaPeriod: "6 months"));

        var section = PersonDossierResolver.BuildInvitations(person, usedInvitationItemIds: null);
        var row = section.Records.Single();

        Assert.Equal("Used", row.StatusLabel);
        Assert.Equal("6 months", row.Cells[2]);
        Assert.Equal("A1731526", row.Cells[5]);
        Assert.Equal("Expires in 45 days", row.CompanionStatusLabel);
        Assert.Equal("st-pending", row.CompanionStatusCssClass);
        Assert.True(row.AppendIssuedVisaBesidePassport);
        Assert.Null(row.ApplicationLinkId);
        Assert.Equal("\u2014", row.Cells[6]);
    }

    private static InvitationItem Item(
        string number,
        DateTime expiration,
        ApplicationProfileInstance? application,
        Visa? visa,
        string? visaPeriod = null) =>
        new()
        {
            ID = Guid.NewGuid(),
            IssuedVisa = visa,
            Invitation = new Invitation
            {
                ID = Guid.NewGuid(),
                InvitationNumber = number,
                IssuedDate = expiration.AddDays(-91),
                ExpirationDate = expiration,
                ApplicationProfileInstance = application,
                VisaPeriod = string.IsNullOrWhiteSpace(visaPeriod)
                    ? null
                    : new VisaPeriod { Name = visaPeriod, NameTm = visaPeriod },
            },
        };
}
