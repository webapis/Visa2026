using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressOrderHelperTests
{
    [Theory]
    [InlineData("IS_BEING_PREPARED", 0)]
    [InlineData("1_REVIEW_STARTED", 11)]
    [InlineData("1_REVIEW_APPROVED", 12)]
    [InlineData("2_REVIEW_STARTED", 13)]
    [InlineData("2_REVIEW_APPROVED", 14)]
    [InlineData("3_REVIEW_STARTED", 15)]
    [InlineData("3_REVIEW_APPROVED", 16)]
    [InlineData("PROCESS_STARTED", 999)]
    [InlineData("PROCESS_ISSUED", 1000)]
    public void GetWorkflowSortKey_MatchesCanonicalTimeline(string stateCode, int expected)
    {
        Assert.Equal(expected, ApplicationProfileInstanceProgressOrderHelper.GetWorkflowSortKey(stateCode));
    }

    [Fact]
    public void CompareTimelineOrder_PrefersWorkflowSequenceOverDate()
    {
        var compare = ApplicationProfileInstanceProgressOrderHelper.CompareTimelineOrder(
            "1_REVIEW_APPROVED",
            new DateTime(2026, 5, 2),
            Guid.Empty,
            "2_REVIEW_APPROVED",
            new DateTime(2026, 4, 25),
            Guid.Empty);

        Assert.True(compare < 0);

        compare = ApplicationProfileInstanceProgressOrderHelper.CompareTimelineOrder(
            "1_REVIEW_STARTED",
            new DateTime(2026, 4, 30),
            Guid.Empty,
            "1_REVIEW_APPROVED",
            new DateTime(2026, 5, 3),
            Guid.Empty);

        Assert.True(compare < 0);
    }

    [Fact]
    public void CompareSiblingOrder_PrefersOrderOverDate()
    {
        var earlierDateLaterOrder = Progress(order: 3, date: new DateTime(2026, 5, 1));
        var laterDateEarlierOrder = Progress(order: 2, date: new DateTime(2026, 6, 1));

        Assert.True(ApplicationProfileInstanceProgressOrderHelper.CompareSiblingOrder(earlierDateLaterOrder, laterDateEarlierOrder) > 0);
    }

    [Fact]
    public void CompareSiblingOrder_SelectsHighestOrderAsLatest()
    {
        var application = new ApplicationProfileInstance();
        var steps = new List<ApplicationProfileInstanceProgress>
        {
            Progress(order: 1, date: new DateTime(2026, 1, 1)),
            Progress(order: 2, date: new DateTime(2026, 1, 2)),
            Progress(order: 3, date: new DateTime(2026, 1, 1)),
        };
        foreach (var step in steps)
        {
            step.ApplicationProfileInstance = application;
            application.ProgressHistory.Add(step);
        }

        var last = application.ProgressHistory
            .OrderByDescending(p => p, Comparer<ApplicationProfileInstanceProgress>.Create(ApplicationProfileInstanceProgressOrderHelper.CompareSiblingOrder))
            .First();

        Assert.Same(steps[2], last);
    }

    private static ApplicationProfileInstanceProgress Progress(int order, DateTime date) =>
        new()
        {
            Order = order,
            Date = date,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
        };
}
