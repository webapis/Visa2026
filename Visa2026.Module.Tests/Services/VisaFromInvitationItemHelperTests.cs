using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class VisaFromInvitationItemHelperTests
{
    [Fact]
    public void CanIssue_null_item_is_not_available()
    {
        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(null, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.NotAvailable", key);
    }

    [Fact]
    public void CanIssue_blocks_closed_or_used_invitation_item()
    {
        var item = new InvitationItem
        {
            ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>
            {
                new()
                {
                    ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
                    LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
                },
            },
        };

        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.ItemUsedOrClosed", key);
    }

    [Fact]
    public void CanIssue_blocks_when_IssuedVisa_already_linked_without_object_space()
    {
        var item = BuildEligibleItem();
        item.IssuedVisa = new Visa();

        // IssuedVisa makes IssuedDocumentLifecycle.IsUsed true, so the used/closed gate fires first.
        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.ItemUsedOrClosed", key);
    }

    [Fact]
    public void CanIssue_blocks_when_issuing_instance_missing()
    {
        var item = new InvitationItem
        {
            Person = new Person(),
            Passport = new Passport(),
            Invitation = new Invitation(),
        };

        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.NoIssuingInstance", key);
    }

    [Fact]
    public void CanIssue_blocks_ineligible_issuing_instance()
    {
        var item = BuildEligibleItem();
        item.Invitation!.ApplicationProfileInstance!.ApplicationProfile!.ProduceVisa = false;
        item.Invitation.ApplicationProfileInstance.ApplicationProfile.ProduceInvitation = false;

        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.IneligibleInstance", key);
    }

    [Fact]
    public void CanIssue_blocks_when_person_or_passport_missing()
    {
        var item = BuildEligibleItem();
        item.Passport = null;

        Assert.False(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
        Assert.Equal("InvitationItem.IssueVisa.NoPassport", key);
    }

    [Fact]
    public void CanIssue_allows_open_eligible_item_without_object_space()
    {
        var item = BuildEligibleItem();

        Assert.True(VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(item, objectSpace: null, out var key));
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
        };
}
