using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceLastNExpectedTests
{
    [Fact]
    public void Visa_one_active_expects_one()
    {
        Assert.Equal(1, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Visa, lastN: 2, availableActive: 1, linksLocked: false));
    }

    [Fact]
    public void Visa_two_active_expects_two()
    {
        Assert.Equal(2, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Visa, lastN: 2, availableActive: 2, linksLocked: false));
    }

    [Fact]
    public void Visa_three_active_caps_at_last_n()
    {
        Assert.Equal(2, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Visa, lastN: 2, availableActive: 3, linksLocked: false));
    }

    [Fact]
    public void Visa_none_active_unlocked_expects_one()
    {
        Assert.Equal(1, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Visa, lastN: 2, availableActive: 0, linksLocked: false));
    }

    [Fact]
    public void Visa_none_active_locked_expects_zero()
    {
        Assert.Equal(0, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Visa, lastN: 2, availableActive: 0, linksLocked: true));
    }

    [Fact]
    public void Work_permit_and_invitation_match_visa()
    {
        Assert.Equal(1, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.WorkPermitItem, lastN: 2, availableActive: 1, linksLocked: false));
        Assert.Equal(2, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.InvitationItem, lastN: 2, availableActive: 2, linksLocked: false));
    }

    [Fact]
    public void Passport_keeps_last_n_quota()
    {
        Assert.Equal(2, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.Passport, lastN: 2, availableActive: 1, linksLocked: false));
    }
}