using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Application list result chips are binary issued / not issued.
/// Later life (used/expired/cancelled) lives on each person mark, not on the chip fill.
/// </summary>
public sealed class ApplicationWorkspaceIssuedResultChipToneTests
{
    [Fact]
    public void Tone_full_coverage_is_issued()
    {
        var chip = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation, "Invitation", 3, 3, false);

        Assert.Equal("done", ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 5)]
    public void Tone_partial_or_empty_expected_is_not_issued(int coverage, int expected)
    {
        var chip = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, "Visa", coverage, expected, false);

        Assert.Equal("none", ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
    }

    [Fact]
    public void Tone_zero_expected_is_zero_not_half()
    {
        var chip = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, "Border zone", 0, 0, false);

        Assert.Equal("zero", ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
    }

    [Theory]
    [InlineData(0, "rej-empty")]
    [InlineData(1, "rej")]
    [InlineData(4, "rej")]
    public void Tone_rejection_stays_empty_or_red(int coverage, string tone)
    {
        var chip = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.Rejection, "Rejection", coverage, 0, true);

        Assert.Equal(tone, ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
    }
}
