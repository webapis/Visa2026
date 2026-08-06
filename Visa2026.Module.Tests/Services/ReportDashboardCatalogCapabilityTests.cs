using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Catalog capability / routing helpers (not BuildListCriteria — covered by open PR #18).
/// </summary>
public class ReportDashboardCatalogCapabilityTests
{
    [Fact]
    public void PersonSearchTokens_EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Empty(ReportDashboardCatalog.PersonSearchTokens(null));
        Assert.Empty(ReportDashboardCatalog.PersonSearchTokens("   "));
    }

    [Theory]
    [InlineData(ReportDashboardCategory.WorkPermit, true)]
    [InlineData(ReportDashboardCategory.Education, true)]
    [InlineData(ReportDashboardCategory.MedicalRecord, true)]
    [InlineData(ReportDashboardCategory.VisaExtension, false)]
    [InlineData(ReportDashboardCategory.PersonSearch, false)]
    public void SupportsIncludeArchivedPersons_ByCategory(
        ReportDashboardCategory category,
        bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.SupportsIncludeArchivedPersons(category));
    }

    [Fact]
    public void ApplicationProgressRouteFor_MapsApplicationCategories()
    {
        Assert.Equal(
            ApplicationProgressRouteKind.ViaMinistries,
            ReportDashboardCatalog.ApplicationProgressRouteFor(
                ReportDashboardCategory.ApplicationViaMinistry));
        Assert.Equal(
            ApplicationProgressRouteKind.DirectToMigrationService,
            ReportDashboardCatalog.ApplicationProgressRouteFor(
                ReportDashboardCategory.ApplicationDirectMigration));
        Assert.Null(ReportDashboardCatalog.ApplicationProgressRouteFor(
            ReportDashboardCategory.VisaExtension));
    }

    [Theory]
    [InlineData("Expired", 0)]
    [InlineData("< 7 days", 1)]
    [InlineData("≥ 6 months", 6)]
    [InlineData("unknown", 99)]
    [InlineData(null, 99)]
    public void RegistrationExpiringStateBucketSortKey_OrdersBuckets(string label, int expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.RegistrationExpiringStateBucketSortKey(label));
    }

    [Theory]
    [InlineData("< 1 week", 1)]
    [InlineData("≥ 1 month", 6)]
    [InlineData("nope", 99)]
    public void RegistrationToBeCheckedInBucketSortKey_OrdersBuckets(string label, int expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.RegistrationToBeCheckedInBucketSortKey(label));
    }

    [Theory]
    [InlineData("Expired", 0)]
    [InlineData("< 7 days", 7)]
    [InlineData("x", 99)]
    public void RegistrationToBeCheckedOutBucketSortKey_OrdersBuckets(string label, int expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.RegistrationToBeCheckedOutBucketSortKey(label));
    }

    [Theory]
    [InlineData("extension-required", true)]
    [InlineData("extension-required-by-period-category-type", true)]
    [InlineData("by-days-remaining", false)]
    [InlineData(null, false)]
    public void UsesVisaExtensionRequiredListView_RecognizesKeys(string subReport, bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.UsesVisaExtensionRequiredListView(subReport));
    }

    [Theory]
    [InlineData("by-days-remaining", true)]
    [InlineData("extension-required", false)]
    public void UsesVisaByDaysRemainingListView_RecognizesKeys(string subReport, bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.UsesVisaByDaysRemainingListView(subReport));
    }

    [Fact]
    public void UsesApplicationViaMinistryRdListView_IncludesOnProcessAndCompletedKeys()
    {
        Assert.True(ReportDashboardCatalog.UsesApplicationViaMinistryRdListView(
            ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey));
        Assert.True(ReportDashboardCatalog.UsesApplicationViaMinistryRdListView(
            "invitation-completed"));
        Assert.False(ReportDashboardCatalog.UsesApplicationViaMinistryRdListView("unknown-key"));
    }

    [Fact]
    public void ToPersonRole_MapsDashboardPersonTypes()
    {
        Assert.Equal(
            PersonRecordRole.Employee,
            ReportDashboardCatalog.ToPersonRole(ReportDashboardPersonType.Employees));
        Assert.Equal(
            PersonRecordRole.FamilyMember,
            ReportDashboardCatalog.ToPersonRole(ReportDashboardPersonType.FamilyMembers));
        Assert.True(ReportDashboardCatalog.IsAllPersonTypes(ReportDashboardPersonType.All));
        Assert.Null(ReportDashboardCatalog.TryGetPersonRole(ReportDashboardPersonType.All));
    }
}
