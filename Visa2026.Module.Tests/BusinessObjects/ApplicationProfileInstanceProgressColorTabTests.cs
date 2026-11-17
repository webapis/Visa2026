using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressColorTabTests
{
    [Theory]
    [InlineData("IS_BEING_PREPARED", null, ApplicationProfileInstanceProgressColorTab.Office)]
    [InlineData("1_REVIEW_STARTED", null, ApplicationProfileInstanceProgressColorTab.ApprovalInProgress)]
    [InlineData("1_REVIEW_APPROVED", null, ApplicationProfileInstanceProgressColorTab.ApprovalInProgress)]
    [InlineData("LAST_APPROVAL_COMPLETE", null, ApplicationProfileInstanceProgressColorTab.LastApproval)]
    [InlineData("SUBMITTED_MIGRATION", null, ApplicationProfileInstanceProgressColorTab.Submitted)]
    [InlineData("PROCESS_STARTED", null, ApplicationProfileInstanceProgressColorTab.ProcessStarted)]
    [InlineData("PROCESS_ISSUED", null, ApplicationProfileInstanceProgressColorTab.Issued)]
    [InlineData("PROCESS_REJECTED", null, ApplicationProfileInstanceProgressColorTab.Rejected)]
    [InlineData("2_REVIEW_REJECTED", null, ApplicationProfileInstanceProgressColorTab.Rejected)]
    [InlineData("PROCESS_CANCELLED", null, ApplicationProfileInstanceProgressColorTab.Cancelled)]
    [InlineData("APP_PROGRESS_SLA_WARNING", "PROCESS_STARTED", ApplicationProfileInstanceProgressColorTab.ProcessStarted)]
    public void ResolveBucketKey_maps_appearance_to_color_tab(
        string appearance,
        string? primary,
        string expected)
    {
        Assert.Equal(expected, ApplicationProfileInstanceProgressColorTab.ResolveBucketKey(appearance, primary));
    }

    [Fact]
    public void BuildTabs_always_includes_every_color_bucket_even_when_count_is_zero()
    {
        var apps = new[]
        {
            AppWithAppearance("IS_BEING_PREPARED"),
            AppWithAppearance("PROCESS_ISSUED"),
            AppWithAppearance("PROCESS_ISSUED"),
        };

        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(apps, selectedKey: "issued");

        Assert.Equal(ApplicationProfileInstanceProgressColorTab.AllKey, tabs[0].Key);
        Assert.Equal(3, tabs[0].Count);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Office && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Issued && t.Count == 2);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Submitted && t.Count == 0);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Rejected && t.Count == 0);
        Assert.DoesNotContain(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Cancelled);
        Assert.Equal(ApplicationProfileInstanceProgressColorTab.SpecialFollowUp, tabs[^1].Key);
        Assert.Equal(0, tabs[^1].Count);
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.OrderedBucketKeys.Count + 2,
            tabs.Count);
    }

    [Fact]
    public void NormalizeSelection_falls_back_when_cancelled_tab_removed()
    {
        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [AppWithAppearance("PROCESS_ISSUED")],
            selectedKey: "office");

        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.AllKey,
            ApplicationProfileInstanceProgressColorTab.NormalizeSelection(
                ApplicationProfileInstanceProgressColorTab.Cancelled,
                tabs));
    }

    [Fact]
    public void ResolveBucketKey_instance_uses_Application_Result_Rejection()
    {
        var withRejection = AppWithAppearance("PROCESS_ISSUED");
        withRejection.Rejections = new ObservableCollection<Rejection>
        {
            new Rejection(),
        };

        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.Rejected,
            ApplicationProfileInstanceProgressColorTab.ResolveBucketKey(withRejection));

        var progressRejectedOnly = AppWithAppearance("PROCESS_REJECTED");
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.ProcessStarted,
            ApplicationProfileInstanceProgressColorTab.ResolveBucketKey(progressRejectedOnly));

        var reviewRejectedOnly = AppWithAppearance("2_REVIEW_REJECTED");
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.ApprovalInProgress,
            ApplicationProfileInstanceProgressColorTab.ResolveBucketKey(reviewRejectedOnly));
    }

    [Fact]
    public void BuildTabs_counts_Garşylyk_from_Rejection_headers()
    {
        var withRejection = AppWithAppearance("PROCESS_ISSUED");
        withRejection.Rejections = new ObservableCollection<Rejection> { new Rejection() };

        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [withRejection, AppWithAppearance("PROCESS_ISSUED")],
            selectedKey: ApplicationProfileInstanceProgressColorTab.Rejected);

        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Rejected && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Issued && t.Count == 1);
    }

    [Fact]
    public void NormalizeSelection_keeps_zero_count_bucket_and_falls_back_for_unknown()
    {
        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [AppWithAppearance("PROCESS_ISSUED")],
            selectedKey: "office");

        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.Office,
            ApplicationProfileInstanceProgressColorTab.NormalizeSelection("office", tabs));
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.AllKey,
            ApplicationProfileInstanceProgressColorTab.NormalizeSelection("not-a-bucket", tabs));
    }

    [Fact]
    public void GetSearchLabel_uses_turkmen_and_english()
    {
        Assert.Equal("Gözleg", ApplicationProfileInstanceProgressColorTab.GetSearchLabel(new CultureInfo("tk-TM")));
        Assert.Equal("Search", ApplicationProfileInstanceProgressColorTab.GetSearchLabel(new CultureInfo("en-US")));
    }

    [Fact]
    public void BuildTabs_special_follow_up_is_last_and_ignores_progress_color()
    {
        var flaggedIssued = AppWithAppearance("PROCESS_ISSUED");
        flaggedIssued.IsSpecialFollowUp = true;
        var flaggedOffice = AppWithAppearance("IS_BEING_PREPARED");
        flaggedOffice.IsSpecialFollowUp = true;
        var plain = AppWithAppearance("SUBMITTED_MIGRATION");

        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [flaggedIssued, flaggedOffice, plain],
            selectedKey: ApplicationProfileInstanceProgressColorTab.SpecialFollowUp);

        Assert.Equal(ApplicationProfileInstanceProgressColorTab.SpecialFollowUp, tabs[^1].Key);
        Assert.Equal(2, tabs[^1].Count);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Issued && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Office && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Submitted && t.Count == 1);
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.SpecialFollowUp,
            ApplicationProfileInstanceProgressColorTab.NormalizeSelection(
                ApplicationProfileInstanceProgressColorTab.SpecialFollowUp,
                tabs));
    }

    [Fact]
    public void BuildTabs_special_follow_up_live_ids_override_stale_property()
    {
        var stale = AppWithAppearance("PROCESS_ISSUED");
        stale.IsSpecialFollowUp = true;

        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [stale],
            selectedKey: ApplicationProfileInstanceProgressColorTab.SpecialFollowUp,
            specialFollowUpIds: new HashSet<Guid>());

        Assert.Equal(0, tabs[^1].Count);
    }

    [Fact]
    public void BuildTabs_can_include_search_tab_with_match_count()
    {
        var apps = new[]
        {
            AppWithAppearance("PROCESS_ISSUED"),
            AppWithAppearance("IS_BEING_PREPARED"),
        };

        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            apps,
            selectedKey: ApplicationProfileInstanceProgressColorTab.SearchKey,
            includeSearchTab: true,
            searchMatchCount: 2);

        Assert.Contains(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.SearchKey && t.Count == 2);
        Assert.Equal(
            ApplicationProfileInstanceProgressColorTab.SearchKey,
            ApplicationProfileInstanceProgressColorTab.NormalizeSelection(
                ApplicationProfileInstanceProgressColorTab.SearchKey,
                tabs));
    }

    private static ApplicationProfileInstance AppWithAppearance(string appearanceCode)
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            },
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
            Rejections = new ObservableCollection<Rejection>(),
        };

        if (string.Equals(appearanceCode, "IS_BEING_PREPARED", System.StringComparison.OrdinalIgnoreCase))
            return app;

        app.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            Date = System.DateTime.Today,
            State = new ApplicationState { Code = appearanceCode, NameTm = appearanceCode },
        });
        return app;
    }
}