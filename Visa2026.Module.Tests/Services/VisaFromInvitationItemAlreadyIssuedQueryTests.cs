using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers the ObjectSpace query path for "visa already issued from this invitation item"
/// (navigation <see cref="InvitationItem.IssuedVisa"/> may be unloaded).
/// </summary>
public class VisaFromInvitationItemAlreadyIssuedQueryTests
{
    [Fact]
    public void CanIssue_blocks_when_object_space_finds_visa_for_item()
    {
        var item = BuildEligibleItem();
        item.ID = Guid.NewGuid();
        // IssuedVisa nav intentionally null — officer UI often does not load the inverse.
        Assert.Null(item.IssuedVisa);

        var space = QueryableObjectSpaceStub.Create(stub =>
            stub.SetQuery([new Visa { IssuingInvitationItem = item }]));

        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, space, out var key));
        Assert.Equal("InvitationItem.IssueVisa.VisaAlreadyIssued", key);
    }

    [Fact]
    public void CanIssue_allows_when_object_space_has_no_visa_for_item()
    {
        var item = BuildEligibleItem();
        item.ID = Guid.NewGuid();

        var space = QueryableObjectSpaceStub.Create(stub =>
            stub.SetQuery(Array.Empty<Visa>()));

        Assert.True(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, space, out var key));
        Assert.Null(key);
    }

    private static InvitationItem BuildEligibleItem() =>
        new()
        {
            Person = new Person(),
            Passport = new Passport(),
            Invitation = new Invitation
            {
                ApplicationProfileInstance = new ApplicationProfileInstance
                {
                    ApplicationProfile = new ApplicationProfile
                    {
                        ProduceVisa = true,
                        ProduceInvitation = true,
                    },
                },
            },
            ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>(),
        };
}
