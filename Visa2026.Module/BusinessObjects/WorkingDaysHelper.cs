using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Mon–Fri working days (weekends excluded; public holidays not modeled yet).</summary>
public static class WorkingDaysHelper
{
    public static bool IsWorkingDay(DateTime date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    /// <summary>
    /// Inclusive count of working days from <paramref name="startDate"/> through <paramref name="endDate"/>.
    /// Day 1 is the start date when it is a working day.
    /// </summary>
    public static int CountWorkingDaysInclusive(DateTime startDate, DateTime endDate)
    {
        var from = startDate.Date;
        var to = endDate.Date;
        if (to < from)
            return 0;

        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Date of the Nth working day starting at <paramref name="startDate"/> as day 1
    /// (the start date counts when it is a working day).
    /// </summary>
    public static DateTime AddWorkingDaysInclusive(DateTime startDate, int workingDays)
    {
        if (workingDays <= 0)
            return startDate.Date;

        var day = startDate.Date;
        var counted = 0;
        while (counted < workingDays)
        {
            if (IsWorkingDay(day))
                counted++;
            if (counted >= workingDays)
                return day;
            day = day.AddDays(1);
        }

        return day;
    }

    public static DateTime NextWorkingDay(DateTime date)
    {
        var day = date.Date.AddDays(1);
        while (!IsWorkingDay(day))
            day = day.AddDays(1);
        return day;
    }
}
