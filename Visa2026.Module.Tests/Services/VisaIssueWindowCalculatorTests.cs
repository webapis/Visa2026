using System;
using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class VisaIssueWindowCalculatorTests
{
    private static readonly DateTime Today = new(2026, 11, 16);

    private static ApplicationWorkspaceIssuedResultListCoverage.Chip Chip(string key, int covered, int expected) =>
        new(key, key, covered, expected, false);

    private static List<ApplicationWorkspaceIssuedResultListCoverage.Chip> Chips(int workPermits, int visas) =>
    [
        Chip(ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, workPermits, 2),
        Chip(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, visas, 2),
    ];

    [Fact]
    public void Issue_day_shows_the_full_window()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            VisaIssueWindowCalculator.ProfileCode,
            Chips(2, 0),
            new DateTime(2026, 11, 16),
            lastVisaIssued: null,
            Today,
            45,
            5);

        Assert.NotNull(badge);
        Assert.Equal(45, badge.Value.Days);
        Assert.Equal("ok", badge.Value.Tone);
        Assert.False(badge.Value.RecolorVisaChip);
    }

    [Fact]
    public void Open_window_stays_calm_above_the_warning()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(2, 0),
            new DateTime(2026, 11, 3),
            null,
            Today,
            45,
            5);

        Assert.Equal(32, badge!.Value.Days);
        Assert.Equal("ok", badge.Value.Tone);
        Assert.False(badge.Value.RecolorVisaChip);
    }

    [Fact]
    public void Last_five_days_warn_and_recolor_the_visa_chip()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(2, 1),
            new DateTime(2026, 10, 6),
            null,
            Today,
            45,
            5);

        Assert.Equal(4, badge!.Value.Days);
        Assert.Equal("warn", badge.Value.Tone);
        Assert.True(badge.Value.RecolorVisaChip);
    }

    [Fact]
    public void Deadline_day_holds_at_zero_and_recolors_the_visa_chip()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(2, 0),
            new DateTime(2026, 10, 2),
            null,
            Today,
            45,
            5);

        Assert.Equal(0, badge!.Value.Days);
        Assert.Equal("warn", badge.Value.Tone);
        Assert.True(badge.Value.RecolorVisaChip);
    }

    [Fact]
    public void Completed_visas_freeze_the_days_left_on_the_last_visa_day()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(2, 2),
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 10),
            Today,
            45,
            5);

        Assert.Equal(36, badge!.Value.Days);
        Assert.Equal("done", badge.Value.Tone);
        Assert.False(badge.Value.RecolorVisaChip);
        Assert.Contains("10.11.2026", badge.Value.Title);
    }

    [Fact]
    public void Late_last_visa_freezes_at_zero_without_recoloring_the_visa_chip()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(2, 2),
            new DateTime(2026, 9, 1),
            new DateTime(2026, 11, 1),
            Today,
            45,
            5);

        Assert.Equal(0, badge!.Value.Days);
        Assert.Equal("done", badge.Value.Tone);
        Assert.False(badge.Value.RecolorVisaChip);
    }

    [Fact]
    public void Hidden_until_every_person_has_a_work_permit()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "extend_visa_wp",
            Chips(1, 0),
            new DateTime(2026, 11, 10),
            null,
            Today,
            45,
            5);

        Assert.Null(badge);
    }

    [Fact]
    public void Other_profiles_have_no_counter()
    {
        var badge = VisaIssueWindowCalculator.ForCoverage(
            "get_invitation_wp",
            Chips(2, 0),
            new DateTime(2026, 11, 1),
            null,
            Today,
            45,
            5);

        Assert.Null(badge);
    }

    [Theory]
    [InlineData(40, 5, 40, 5)]
    [InlineData(0, 0, 45, 5)]
    [InlineData(30, 40, 30, 30)]
    public void Days_fall_back_and_warning_cannot_exceed_the_window(
        int window, int warning, int expectedWindow, int expectedWarning)
    {
        Assert.Equal(expectedWindow, VisaIssueWindowCalculator.NormalizeWindowDays(window));
        Assert.Equal(expectedWarning, VisaIssueWindowCalculator.NormalizeWarningDays(warning, window));
    }
}
