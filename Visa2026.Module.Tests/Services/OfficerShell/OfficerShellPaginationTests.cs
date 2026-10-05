using System.Linq;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

public class OfficerShellPaginationTests
{
    [Fact]
    public void Paginate_EmptyList_ReportsZeroStartEndAndSinglePage()
    {
        var result = OfficerShellPagination.Paginate(
            Array.Empty<int>(),
            new OfficerShellPaginationState { Page = 3, PageSize = 25 });

        Assert.Empty(result.PageItems);
        Assert.Equal(0, result.Total);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.Start);
        Assert.Equal(0, result.End);
        Assert.Equal(25, result.PageSize);
    }

    [Fact]
    public void Paginate_ClampsPagePastEnd_AndUsesDefaultPageSizeWhenInvalid()
    {
        var items = Enumerable.Range(1, 12).ToList();

        var result = OfficerShellPagination.Paginate(
            items,
            new OfficerShellPaginationState { Page = 99, PageSize = 0 });

        Assert.Equal(OfficerShellPagination.PageSizeOptions[1], result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(12, result.Total);
        Assert.Equal(1, result.Start);
        Assert.Equal(12, result.End);
        Assert.Equal(Enumerable.Range(1, 12), result.PageItems);
    }

    [Fact]
    public void Paginate_SecondPage_UsesInclusiveStartEnd()
    {
        var items = Enumerable.Range(1, 30).ToList();

        var result = OfficerShellPagination.Paginate(
            items,
            new OfficerShellPaginationState { Page = 2, PageSize = 10 });

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(11, result.Start);
        Assert.Equal(20, result.End);
        Assert.Equal(Enumerable.Range(11, 10), result.PageItems);
    }

    [Fact]
    public void Paginate_NullItems_TreatedAsEmpty()
    {
        var result = OfficerShellPagination.Paginate<string>(
            null!,
            new OfficerShellPaginationState { Page = 1, PageSize = 10 });

        Assert.Empty(result.PageItems);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Start);
        Assert.Equal(0, result.End);
    }
}
