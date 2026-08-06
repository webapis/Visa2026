using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationTypeCapabilitiesTests
{
    [Fact]
    public void CanIssue_NullType_ReturnsFalse()
    {
        Assert.False(ApplicationTypeCapabilities.CanIssueVisa(null));
        Assert.False(ApplicationTypeCapabilities.CanIssueInvitation(null));
        Assert.False(ApplicationTypeCapabilities.CanIssueWorkPermit(null));
        Assert.False(ApplicationTypeCapabilities.CanBeIssuingApplicationForVisa(null));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void CanBeIssuingApplicationForVisa_IsUnionOfVisaAndInvitation(
        bool canIssueVisa,
        bool canIssueInvitation,
        bool expected)
    {
        var type = new ApplicationType
        {
            CanIssueVisa = canIssueVisa,
            CanIssueInvitation = canIssueInvitation
        };

        Assert.Equal(expected, ApplicationTypeCapabilities.CanBeIssuingApplicationForVisa(type));
        Assert.Equal(canIssueVisa, ApplicationTypeCapabilities.CanIssueVisa(type));
        Assert.Equal(canIssueInvitation, ApplicationTypeCapabilities.CanIssueInvitation(type));
    }

    [Fact]
    public void CanIssueWorkPermit_RespectsFlag()
    {
        Assert.True(ApplicationTypeCapabilities.CanIssueWorkPermit(
            new ApplicationType { CanIssueWorkPermit = true }));
        Assert.False(ApplicationTypeCapabilities.CanIssueWorkPermit(
            new ApplicationType { CanIssueWorkPermit = false }));
    }
}
