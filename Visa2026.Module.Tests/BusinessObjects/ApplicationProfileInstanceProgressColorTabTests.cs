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
    public void BuildTabs_includes_All_and_only_present_colors()
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
        Assert.DoesNotContain(tabs, t => t.Key == ApplicationProfileInstanceProgressColorTab.Submitted);
    }

    [Fact]
    public void NormalizeSelection_falls_back_to_All_when_missing()
    {
        var tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            [AppWithAppearance("PROCESS_ISSUED")],
            selectedKey: "office");

        var selected = ApplicationProfileInstanceProgressColorTab.NormalizeSelection("office", tabs);
        Assert.Equal(ApplicationProfileInstanceProgressColorTab.AllKey, selected);
    }

    [Fact]
    public void GetSearchLabel_uses_turkmen_and_english()
    {
        Assert.Equal("Gözleg", ApplicationProfileInstanceProgressColorTab.GetSearchLabel(new CultureInfo("tk-TM")));
        Assert.Equal("Search", ApplicationProfileInstanceProgressColorTab.GetSearchLabel(new CultureInfo("en-US")));
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