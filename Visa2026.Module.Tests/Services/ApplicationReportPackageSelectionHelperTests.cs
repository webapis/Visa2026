using System.Collections.Generic;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationReportPackageSelectionHelperTests
{
    [Fact]
    public void ReconcileSelectedKeys_FirstLoad_SelectsEveryRow()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var known = new HashSet<string>(StringComparer.Ordinal);
        var initialized = false;

        ApplicationReportPackageSelectionHelper.ReconcileSelectedKeys(
            selected,
            known,
            new[] { "profile:a", "profile:b" },
            ref initialized);

        Assert.True(initialized);
        Assert.Equal(2, selected.Count);
        Assert.Contains("profile:a", selected);
        Assert.Contains("profile:b", selected);
        Assert.Equal(selected, known);
    }

    [Fact]
    public void ReconcileSelectedKeys_ApproveAddsRow_SelectsTheNewKey()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal) { "profile:a" };
        var known = new HashSet<string>(StringComparer.Ordinal) { "profile:a" };
        var initialized = true;

        ApplicationReportPackageSelectionHelper.ReconcileSelectedKeys(
            selected,
            known,
            new[] { "profile:a", "profile:new" },
            ref initialized);

        Assert.Contains("profile:a", selected);
        Assert.Contains("profile:new", selected);
        Assert.Equal(2, selected.Count);
    }

    [Fact]
    public void ReconcileSelectedKeys_KeepsOfficerUncheckedRows()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal) { "profile:b" };
        var known = new HashSet<string>(StringComparer.Ordinal) { "profile:a", "profile:b" };
        var initialized = true;

        ApplicationReportPackageSelectionHelper.ReconcileSelectedKeys(
            selected,
            known,
            new[] { "profile:a", "profile:b" },
            ref initialized);

        Assert.DoesNotContain("profile:a", selected);
        Assert.Contains("profile:b", selected);
    }

    [Fact]
    public void FindEntryKeyByName_MatchesThisProfileRowIgnoringCase()
    {
        var entries = new[]
        {
            new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = "profile:old",
                DisplayName = "YUZTUTMA",
            },
            new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = "profile:new",
                DisplayName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
            },
        };

        Assert.Equal(
            "profile:new",
            ApplicationReportPackageSelectionHelper.FindEntryKeyByName(
                entries,
                "Dasary_yurt_rayatlarynyn_sanawy_cakylyk"));
        Assert.Null(ApplicationReportPackageSelectionHelper.FindEntryKeyByName(entries, "missing"));
    }
}