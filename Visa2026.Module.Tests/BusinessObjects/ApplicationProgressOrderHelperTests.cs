using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressOrderHelperTests
{
    [Theory]
    [InlineData("IS_BEING_PREPARED", 0)]
    [InlineData("1_REVIEW_STARTED", 12)]
    [InlineData("1_REVIEW_APPROVED", 13)]
    [InlineData("2_REVIEW_STARTED", 14)]
    [InlineData("2_REVIEW_APPROVED", 15)]
    [InlineData("3_REVIEW_STARTED", 16)]
    [InlineData("3_REVIEW_APPROVED", 17)]
    [InlineData("PROCESS_STARTED", 999)]
    [InlineData("PROCESS_ISSUED", 1000)]
    public void GetWorkflowSortKey_MatchesCanonicalTimeline(string stateCode, int expected)
    {
        Assert.Equal(expected, ApplicationProgressOrderHelper.GetWorkflowSortKey(stateCode));
    }

    [Fact]
    public void CompareTimelineOrder_PrefersWorkflowSequenceOverDate()
    {
        var compare = ApplicationProgressOrderHelper.CompareTimelineOrder(
            "1_REVIEW_STARTED",
            new DateTime(2026, 4, 1),
            Guid.Empty,
            "3_REVIEW_STARTED",
            new DateTime(2026, 4, 30),
            Guid.Empty);

        Assert.True(compare < 0);

        compare = ApplicationProgressOrderHelper.CompareTimelineOrder(
            "1_REVIEW_APPROVED",
            new DateTime(2026, 5, 2),
            Guid.Empty,
            "2_REVIEW_STARTED",
            new DateTime(2026, 4, 25),
            Guid.Empty);

        Assert.True(compare < 0);

        compare = ApplicationProgressOrderHelper.CompareTimelineOrder(
            "3_REVIEW_STARTED",
            new DateTime(2026, 4, 30),
            Guid.Empty,
            "3_REVIEW_APPROVED",
            new DateTime(2026, 5, 3),
            Guid.Empty);

        Assert.True(compare < 0);
    }
}