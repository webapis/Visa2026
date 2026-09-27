using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class VisaIssuingApplicationProfileInstanceHelperTests
{
    [Fact]
    public void GetEffectiveIssuingApplicationProfileInstance_returns_linked_instance()
    {
        var instance = new ApplicationProfileInstance();
        var visa = new Visa { IssuingApplicationProfileInstance = instance };

        Assert.Same(instance, VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(visa));
        Assert.Null(VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(null));
        Assert.Null(VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(new Visa()));
    }

    [Fact]
    public void IsEligible_prefers_profile_ProduceVisa_or_ProduceInvitation()
    {
        var eligible = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceVisa = true, ProduceInvitation = false },
        };
        var invitationOnly = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceVisa = false, ProduceInvitation = true },
        };
        var neither = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceVisa = false, ProduceInvitation = false },
        };

        Assert.True(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(eligible));
        Assert.True(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(invitationOnly));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(neither));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(null));
    }

    [Fact]
    public void IsEligible_falls_back_to_ApplicationType_when_profile_missing()
    {
        var visaType = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { CanIssueVisa = true, CanIssueInvitation = false },
        };
        var invitationType = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { CanIssueVisa = false, CanIssueInvitation = true },
        };
        var neither = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { CanIssueVisa = false, CanIssueInvitation = false },
        };
        var missingType = new ApplicationProfileInstance();

        Assert.True(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(visaType));
        Assert.True(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(invitationType));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(neither));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(missingType));
    }

    [Fact]
    public void CanIssueInvitationForApplication_uses_profile_ProduceInvitation_over_type()
    {
        var profileAllows = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
            ApplicationType = new ApplicationType { CanIssueInvitation = false },
        };
        var profileBlocks = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = false },
            ApplicationType = new ApplicationType { CanIssueInvitation = true },
        };
        var typeAllows = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { CanIssueInvitation = true },
        };

        Assert.True(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(profileAllows));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(profileBlocks));
        Assert.True(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(typeAllows));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(null));
    }

    [Fact]
    public void CanIssueInvitationForVisa_reads_effective_issuing_instance()
    {
        var visa = new Visa
        {
            IssuingApplicationProfileInstance = new ApplicationProfileInstance
            {
                ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
            },
        };

        Assert.True(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForVisa(visa));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForVisa(new Visa()));
        Assert.False(VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForVisa(null));
    }
}
