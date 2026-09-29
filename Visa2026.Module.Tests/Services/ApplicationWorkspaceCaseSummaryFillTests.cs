using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Case-summary tile fill-state tokens drive red/blue/officer styling and completeness gates.
/// </summary>
public class ApplicationWorkspaceCaseSummaryFillTests
{
    [Theory]
    [InlineData(ApplicationWorkspaceCaseSummaryFillState.Empty, "empty")]
    [InlineData(ApplicationWorkspaceCaseSummaryFillState.Default, "default")]
    [InlineData(ApplicationWorkspaceCaseSummaryFillState.Officer, "officer")]
    public void CssToken_maps_fill_state(ApplicationWorkspaceCaseSummaryFillState state, string expected) =>
        Assert.Equal(expected, ApplicationWorkspaceCaseSummaryFill.CssToken(state));

    [Theory]
    [InlineData(true, false, ApplicationWorkspaceCaseSummaryFillState.Empty)]
    [InlineData(true, true, ApplicationWorkspaceCaseSummaryFillState.Empty)]
    [InlineData(false, true, ApplicationWorkspaceCaseSummaryFillState.Default)]
    [InlineData(false, false, ApplicationWorkspaceCaseSummaryFillState.Officer)]
    public void Resolve_prefers_empty_then_default_then_officer(
        bool isEmpty,
        bool matchesDefault,
        ApplicationWorkspaceCaseSummaryFillState expected) =>
        Assert.Equal(expected, ApplicationWorkspaceCaseSummaryFill.Resolve(isEmpty, matchesDefault));
}
