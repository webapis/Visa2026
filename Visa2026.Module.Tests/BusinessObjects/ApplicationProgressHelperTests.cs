using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class ApplicationProgressHelperTests
{
    [Fact]
    public void GetLatest_NullHistory_ReturnsNull()
    {
        Assert.Null(ApplicationProgressHelper.GetLatest(null));
    }

    [Fact]
    public void GetLatest_EmptyHistory_ReturnsNull()
    {
        Assert.Null(ApplicationProgressHelper.GetLatest(Array.Empty<ApplicationProgress>()));
    }

    [Fact]
    public void GetLatest_PrefersHigherSiblingOrderOverDate()
    {
        var olderHigherOrder = new ApplicationProgress
        {
            Order = 5,
            Date = new DateTime(2026, 1, 1),
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        };
        var newerLowerOrder = new ApplicationProgress
        {
            Order = 2,
            Date = new DateTime(2026, 6, 1),
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        var latest = ApplicationProgressHelper.GetLatest([newerLowerOrder, olderHigherOrder]);

        Assert.Same(olderHigherOrder, latest);
    }

    [Fact]
    public void GetLatest_SameOrder_PrefersLaterDateThenId()
    {
        var earlier = new ApplicationProgress
        {
            Order = 3,
            Date = new DateTime(2026, 4, 1),
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        };
        var later = new ApplicationProgress
        {
            Order = 3,
            Date = new DateTime(2026, 5, 1),
            ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        };

        Assert.Same(later, ApplicationProgressHelper.GetLatest([earlier, later]));
    }

    [Theory]
    [InlineData("ProgressHistory", "PROCESS_ISSUED", true,
        "ProgressHistory[Date = ^.ProgressHistory.Max(Date)].Single(State.Code) = 'PROCESS_ISSUED'")]
    [InlineData("Items.Progress", "IS_BEING_PREPARED", false,
        "Items.Progress[Date = ^.Items.Progress.Max(Date)].Single(State.Code) <> 'IS_BEING_PREPARED'")]
    public void BuildLatestStateCodeCriteria_BuildsDevExpressPath(
        string path,
        string stateCode,
        bool equals,
        string expected)
    {
        Assert.Equal(expected, ApplicationProgressHelper.BuildLatestStateCodeCriteria(path, stateCode, equals));
    }
}
