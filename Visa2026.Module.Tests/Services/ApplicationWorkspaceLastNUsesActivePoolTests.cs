#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Last-N ceiling uses the person's active pool only for issued/history kinds —
/// WorkDuty / Passport keep fixed quota behavior.
/// </summary>
public class ApplicationWorkspaceLastNUsesActivePoolTests
{
    [Theory]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Visa)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.WorkPermitItem)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.InvitationItem)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.TravelHistory)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.MedicalRecord)]
    public void UsesActivePool_true_for_issued_and_history_kinds(
        ApplicationProfileInstancePersonLinkKind kind)
    {
        Assert.True(ApplicationWorkspaceLastNExpected.UsesActivePool(kind));
    }

    [Theory]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Passport)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.WorkDuty)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Position)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Education)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Salary)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.AddressOfResidence)]
    [InlineData(ApplicationProfileInstancePersonLinkKind.BorderZoneItem)]
    public void UsesActivePool_false_for_fixed_quota_kinds(
        ApplicationProfileInstancePersonLinkKind kind)
    {
        Assert.False(ApplicationWorkspaceLastNExpected.UsesActivePool(kind));
    }

    [Fact]
    public void Resolve_WorkDuty_ignores_available_active_and_keeps_last_n_floor()
    {
        // Non-pool kinds: max(lastN, 1) regardless of availableActive / lock.
        Assert.Equal(2, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            lastN: 2,
            availableActive: 0,
            linksLocked: true));
        Assert.Equal(1, ApplicationWorkspaceLastNExpected.Resolve(
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            lastN: 0,
            availableActive: 5,
            linksLocked: false));
    }
}
