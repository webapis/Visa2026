using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Work Permit lists keep current permits without a visa filter and without the three
/// archived / valid-visa / one-last-permit checkboxes. Archived people stay off the list.
/// </summary>
public class ReportDashboardWorkPermitFilterSupportTests
{
    [Fact]
    public void WorkPermit_DoesNotExposeArchivedValidVisaOrOneLastToggles()
    {
        Assert.False(ReportDashboardCatalog.SupportsIncludeArchivedPersons(ReportDashboardCategory.WorkPermit));
        Assert.False(ReportDashboardCatalog.SupportsValidVisaPersonsOnly(ReportDashboardCategory.WorkPermit));
        Assert.False(ReportDashboardCatalog.SupportsOneLastValidWorkPermitPerPerson(ReportDashboardCategory.WorkPermit));
        Assert.False(ReportDashboardCatalog.SupportsOneLastValidWorkPermitPerPerson(ReportDashboardCategory.Travel));
    }

    [Fact]
    public void NeighborCategories_StillExposeTheirToggles()
    {
        Assert.True(ReportDashboardCatalog.SupportsIncludeArchivedPersons(ReportDashboardCategory.Education));
        Assert.True(ReportDashboardCatalog.SupportsValidVisaPersonsOnly(ReportDashboardCategory.Travel));
        Assert.True(ReportDashboardCatalog.SupportsValidVisaPersonsOnly(ReportDashboardCategory.Education));
        Assert.False(ReportDashboardCatalog.SupportsValidVisaPersonsOnly(ReportDashboardCategory.IncompletePersons));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildListCriteria_WorkPermit_AlwaysExcludesArchivedPersons(bool includeArchivedPersons)
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.WorkPermit,
            projectKey: null,
            statusLabel: null,
            includeArchivedPersons: includeArchivedPersons,
            subReport: ReportDashboardCatalog.WorkPermitMaglumat2Key);

        Assert.Contains("[Person.IsArchived] = False", criteria, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildListCriteria_Education_RespectsIncludeArchivedToggle()
    {
        var whenOff = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Education,
            projectKey: null,
            statusLabel: null,
            includeArchivedPersons: false);

        var whenOn = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Education,
            projectKey: null,
            statusLabel: null,
            includeArchivedPersons: true);

        Assert.Contains("[Person.IsArchived] = False", whenOff, StringComparison.Ordinal);
        Assert.DoesNotContain("[Person.IsArchived] = False", whenOn, StringComparison.Ordinal);
    }
}
