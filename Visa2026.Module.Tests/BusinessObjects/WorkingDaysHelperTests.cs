using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class WorkingDaysHelperTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void IsWorkingDay_RecognizesWeekdays(DayOfWeek dayOfWeek, bool expected)
    {
        var date = DateTime.Today;
        while (date.DayOfWeek != dayOfWeek)
            date = date.AddDays(1);

        Assert.Equal(expected, WorkingDaysHelper.IsWorkingDay(date));
    }

    [Fact]
    public void CountWorkingDaysInclusive_CountsMonThroughFri()
    {
        var monday = new DateTime(2026, 6, 1);
        var friday = new DateTime(2026, 6, 5);

        Assert.Equal(5, WorkingDaysHelper.CountWorkingDaysInclusive(monday, friday));
    }

    [Fact]
    public void CountWorkingDaysInclusive_SkipsWeekendInRange()
    {
        var friday = new DateTime(2026, 6, 5);
        var nextMonday = new DateTime(2026, 6, 8);

        Assert.Equal(2, WorkingDaysHelper.CountWorkingDaysInclusive(friday, nextMonday));
    }
}
