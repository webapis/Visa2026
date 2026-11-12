using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Locks <see cref="docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md"/> palette + last-approval label.
/// </summary>
public class ApplicationProfileListViewProgressColorsTests
{
    [Theory]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, "#FEDEAF", "#7c4a03")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.Review1Started, "#3FE0D0", "#134e4a")]
    [InlineData(ApplicationProfileInstanceProgressLegCodes.LastApprovalCompleteListAppearance, "#4169E2", "#ffffff")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration, "#FFF59D", "#854d0e")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessStarted, "#FED700", "#1a1a1a")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessIssued, "#86efac", "#15803d")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessRejected, "#fca5a5", "#b91c1c")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.Review1Rejected, "#fca5a5", "#b91c1c")]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, "#fecaca", "#991b1b")]
    public void Registry_matches_listview_progress_color_strategy(string code, string background, string text)
    {
        Assert.True(BoStateAppearanceColors.TryGet(code, out var appearance));
        Assert.Equal(background, appearance.CssBackgroundHex, ignoreCase: true);
        Assert.Equal(text, appearance.CssTextHex, ignoreCase: true);
    }

    [Fact]
    public void Yellow_pair_keeps_submitted_lighter_than_as_co()
    {
        Assert.True(BoStateAppearanceColors.TryGet(
            ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration, out var submitted));
        Assert.True(BoStateAppearanceColors.TryGet(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted, out var asCo));

        Assert.Equal("#FFF59D", submitted.CssBackgroundHex, ignoreCase: true);
        Assert.Equal("#FED700", asCo.CssBackgroundHex, ignoreCase: true);
        Assert.False(string.Equals(submitted.CssBackgroundHex, asCo.CssBackgroundHex, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsLastApprovalLegComplete_only_for_final_leg()
    {
        Assert.True(ApplicationProfileInstanceProgressLegCodes.IsLastApprovalLegComplete("2_REVIEW_APPROVED", 2));
        Assert.False(ApplicationProfileInstanceProgressLegCodes.IsLastApprovalLegComplete("1_REVIEW_APPROVED", 2));
        Assert.True(ApplicationProfileInstanceProgressLegCodes.IsLastApprovalLegComplete("1_REVIEW_APPROVED", 1));
        Assert.False(ApplicationProfileInstanceProgressLegCodes.IsLastApprovalLegComplete("2_REVIEW_STARTED", 2));
    }

    [Fact]
    public void Resolve_uses_last_approval_appearance_and_ministrlikden_cykan_label()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tk-TM");

            var app = new ApplicationProfileInstance
            {
                ApplicationProfile = new ApplicationProfile
                {
                    ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                },
                ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>
                {
                    new() { MinistryShortName = "Energetika", Sequence = 1 },
                    new() { MinistryShortName = "Türkmengaz", Sequence = 2 },
                },
                ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
                {
                    new()
                    {
                        Date = DateTime.Today,
                        State = new ApplicationState
                        {
                            Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2),
                            NameTm = "Ylalaşykdan çykdy",
                        },
                    },
                },
            };

            var state = ApplicationListViewDisplayState.Resolve(app);

            Assert.Equal(
                ApplicationProfileInstanceProgressLegCodes.LastApprovalCompleteListAppearance,
                state.ListRowAppearanceStateCode);
            Assert.Contains("LAST_APPROVAL_COMPLETE", state.ListRowCssClass, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Ministrlikden çykan", state.CurrentState);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Resolve_keeps_intermediate_approval_teal_appearance()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            },
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>
            {
                new() { MinistryShortName = "Energetika", Sequence = 1 },
                new() { MinistryShortName = "Türkmengaz", Sequence = 2 },
            },
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
            {
                new()
                {
                    Date = DateTime.Today,
                    State = new ApplicationState
                    {
                        Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
                        NameTm = "Ylalaşykdan çykdy",
                    },
                },
            },
        };

        var state = ApplicationListViewDisplayState.Resolve(app);

        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
            state.ListRowAppearanceStateCode);
        Assert.DoesNotContain("LAST_APPROVAL_COMPLETE", state.ListRowCssClass, StringComparison.OrdinalIgnoreCase);
    }
}