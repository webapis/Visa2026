using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierInvitationStatusTests
{
    [Fact]
    public void Classify_Cancelled_wins_over_expiry()
    {
        var item = BuildItem(expiration: DateTime.Today.AddDays(-10));
        item.ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>
        {
            new()
            {
                ApplicationProfile = new ApplicationProfile
                {
                    ActionFamily = ApplicationProfileActionFamily.Cancellation,
                },
                LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            },
        };

        var (label, css) = PersonDossierResolver.ClassifyInvitationItem(item);

        Assert.Equal("Cancelled", label);
        Assert.Equal("st-expiring", css);
    }

    [Fact]
    public void Classify_Used_when_visa_issued()
    {
        var item = BuildItem(expiration: DateTime.Today.AddDays(30));
        item.IssuedVisa = new Visa { VisaNumber = "A1" };

        var (label, css) = PersonDossierResolver.ClassifyInvitationItem(item);

        Assert.Equal("Used", label);
        Assert.Equal("st-approved", css);
    }

    [Fact]
    public void Classify_Used_via_used_id_set_when_inverse_not_loaded()
    {
        var item = BuildItem(expiration: DateTime.Today.AddDays(30));
        item.ID = Guid.NewGuid();

        var (label, css) = PersonDossierResolver.ClassifyInvitationItem(
            item,
            usedInvitationItemIds: new HashSet<Guid> { item.ID });

        Assert.Equal("Used", label);
        Assert.Equal("st-approved", css);
    }

    [Fact]
    public void Classify_Expired_when_past_and_not_used()
    {
        var item = BuildItem(expiration: DateTime.Today.AddDays(-1));

        var (label, css) = PersonDossierResolver.ClassifyInvitationItem(item);

        Assert.Equal("Expired", label);
        Assert.Equal("st-expiring", css);
    }

    [Fact]
    public void Classify_ValidTo_when_future_and_not_used()
    {
        var expiry = new DateTime(2027, 3, 24);
        var item = BuildItem(expiration: expiry);

        var (label, css) = PersonDossierResolver.ClassifyInvitationItem(item);

        Assert.StartsWith("Valid to", label, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2027", label, StringComparison.Ordinal);
        Assert.Equal("st-approved", css);
    }

    private static InvitationItem BuildItem(DateTime expiration) =>
        new()
        {
            Invitation = new Invitation
            {
                InvitationNumber = "INV-1",
                ExpirationDate = expiration,
                IssuedDate = expiration.AddMonths(-6),
            },
        };
}
