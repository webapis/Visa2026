using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssueIssuedVisaComposeServiceTests
{
    [Fact]
    public void CanOpenInSlot_RequiresVisaProduceFlag()
    {
        var invitationAndVisa = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
            },
        };
        var visaOnly = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = false,
                ProduceVisa = true,
            },
        };
        var invitationOnly = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = false,
            },
        };

        Assert.True(IssueIssuedVisaComposeService.CanOpenInSlot(invitationAndVisa));
        Assert.True(IssueIssuedVisaComposeService.CanOpenInSlot(visaOnly));
        Assert.False(IssueIssuedVisaComposeService.CanOpenInSlot(invitationOnly));
        Assert.False(IssueIssuedVisaComposeService.CanOpenInSlot(null));
    }

    [Fact]
    public void UsesInvitationSource_OnlyWhenProfileProducesInvitation()
    {
        var invitationAndVisa = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
            },
        };
        var visaOnly = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = false,
                ProduceVisa = true,
            },
        };

        Assert.True(IssueIssuedVisaComposeService.UsesInvitationSource(invitationAndVisa));
        Assert.False(IssueIssuedVisaComposeService.UsesInvitationSource(visaOnly));
        Assert.False(IssueIssuedVisaComposeService.UsesInvitationSource(null));
    }

    [Fact]
    public void Delete_RejectsMissingArguments()
    {
        var result = IssueIssuedVisaComposeService.Delete(null!, Guid.Empty, Guid.Empty);
        Assert.False(result.Succeeded);
        Assert.Equal("Delete is not available.", result.ErrorMessage);
    }

    [Fact]
    public void OccupantKey_IncludesVisaIdWhenEditing()
    {
        var appId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var visaId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.Equal(
            $"issue-issued-visa:{appId:N}",
            VisaPreviewSlotOccupantKeys.ForIssueIssuedVisa(new IssueIssuedVisaSlotRequest
            {
                ApplicationProfileInstanceId = appId,
            }));
        Assert.Equal(
            $"issue-issued-visa:{appId:N}|visa:{visaId:N}",
            VisaPreviewSlotOccupantKeys.ForIssueIssuedVisa(new IssueIssuedVisaSlotRequest
            {
                ApplicationProfileInstanceId = appId,
                ExistingVisaId = visaId,
            }));
    }
}