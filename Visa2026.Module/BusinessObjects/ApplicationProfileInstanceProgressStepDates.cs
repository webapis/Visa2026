using System;
using System.Globalization;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// A corrected progress date stays on or between the previous step and the next step.
/// </summary>
public static class ApplicationProfileInstanceProgressStepDates
{
    public static bool TryValidate(DateTime value, DateTime? earliest, DateTime? latest, out string? error)
    {
        var date = value.Date;
        if (date == default)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.DateRequired");
            return false;
        }

        var min = earliest?.Date;
        var max = latest?.Date;
        if (min is DateTime lower && max is DateTime upper && lower > upper)
        {
            error = null;
            return true;
        }

        if (min is DateTime from && max is DateTime to && (date < from || date > to))
        {
            error = VisaUiMessages.Format(
                "ApplicationProfileInstance.Workspace.DateBetween",
                Format(from),
                Format(to));
            return false;
        }

        if (min is DateTime after && date < after)
        {
            error = VisaUiMessages.Format("ApplicationProfileInstance.Workspace.DateOnOrAfter", Format(after));
            return false;
        }

        if (max is DateTime before && date > before)
        {
            error = VisaUiMessages.Format("ApplicationProfileInstance.Workspace.DateOnOrBefore", Format(before));
            return false;
        }

        error = null;
        return true;
    }

    private static string Format(DateTime date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}
