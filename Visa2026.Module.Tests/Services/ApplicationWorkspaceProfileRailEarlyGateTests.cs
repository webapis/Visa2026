using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Profile-strip actions must not open wizards or create cases without a profile id.
/// </summary>
public sealed class ApplicationWorkspaceProfileRailEarlyGateTests
{
    [Fact]
    public void TryCreateNewApplicationFromProfile_null_application_rejects()
    {
        Assert.False(ApplicationWorkspaceProfileRailHelper.TryCreateNewApplicationFromProfile(
            application: null!,
            applicationProfileId: Guid.NewGuid(),
            contextApplicationProfileInstanceId: Guid.Empty,
            sourceFrame: null,
            out var error));
        Assert.Equal("Select an Application Profile first.", error);
    }

    [Fact]
    public void TryCreateNewApplicationFromProfile_empty_profileId_rejects()
    {
        Assert.False(ApplicationWorkspaceProfileRailHelper.TryCreateNewApplicationFromProfile(
            application: null!,
            applicationProfileId: Guid.Empty,
            contextApplicationProfileInstanceId: Guid.NewGuid(),
            sourceFrame: null,
            out var error));
        Assert.Equal("Select an Application Profile first.", error);
    }

    [Fact]
    public void TryOpenProfileConfiguration_null_or_empty_returns_false()
    {
        Assert.False(ApplicationWorkspaceProfileRailHelper.TryOpenProfileConfiguration(
            application: null!,
            applicationProfileId: Guid.NewGuid(),
            sourceFrame: null));
        Assert.False(ApplicationWorkspaceProfileRailHelper.TryOpenProfileConfiguration(
            application: null!,
            applicationProfileId: Guid.Empty,
            sourceFrame: null));
    }
}
