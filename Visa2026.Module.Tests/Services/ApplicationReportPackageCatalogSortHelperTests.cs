using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationReportPackageCatalogSortHelperTests
{
    [Fact]
    public void Newest_first_orders_by_CreatedOnUtc_descending()
    {
        var a = Entry("B", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var b = Entry("A", new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        var sorted = ApplicationReportPackageCatalogSortHelper.Sort(
            [a, b],
            ApplicationReportPackageCatalogSort.NewestFirst);

        Assert.Equal(["A", "B"], sorted.Select(e => e.DisplayName).ToArray());
    }

    [Fact]
    public void Name_asc_is_case_insensitive()
    {
        var a = Entry("beta", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var b = Entry("Alpha", new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        var sorted = ApplicationReportPackageCatalogSortHelper.Sort(
            [a, b],
            ApplicationReportPackageCatalogSort.NameAsc);

        Assert.Equal(["Alpha", "beta"], sorted.Select(e => e.DisplayName).ToArray());
    }

    private static ApplicationWordReportPackageCatalogEntry Entry(string name, DateTime created) =>
        new()
        {
            EntryKey = name,
            DisplayName = name,
            Kind = ApplicationWordReportPackageEntryKind.UserWord,
            Readiness = ApplicationWordReportPackageReadinessLevel.Ready,
            CreatedOnUtc = created,
        };
}