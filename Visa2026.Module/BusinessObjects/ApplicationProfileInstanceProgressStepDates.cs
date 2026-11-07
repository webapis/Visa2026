using System;
using System.Globalization;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// A progress date stays on or between the previous step and the next step (same day allowed).
/// </summary>
public static class ApplicationProfileInstanceProgressStepDates
{
    public static bool TryValidate(DateTime value, DateTime? earliest, DateTime? latest, out string? error) =>
        TryValidate(value, earliest, latest, precedingStepLabel: null, followingStepLabel: null, out error);

    public static bool TryValidate(
        DateTime value,
        DateTime? earliest,
        DateTime? latest,
        string? precedingStepLabel,
        string? followingStepLabel,
        out string? error)
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

        var preceding = string.IsNullOrWhiteSpace(precedingStepLabel)
            ? null
            : ApplicationProfileLocalization.ProgressStepLabel(precedingStepLabel.Trim());
        var following = string.IsNullOrWhiteSpace(followingStepLabel)
            ? null
            : ApplicationProfileLocalization.ProgressStepLabel(followingStepLabel.Trim());
        if (string.IsNullOrWhiteSpace(preceding))
            preceding = null;
        if (string.IsNullOrWhiteSpace(following))
            following = null;

        if (min is DateTime from && max is DateTime to && (date < from || date > to))
        {
            if (preceding != null && following != null)
            {
                error = VisaUiMessages.Format(
                    "ApplicationProfileInstance.Workspace.DateBetweenPrecedingSteps",
                    preceding,
                    Format(from),
                    following,
                    Format(to));
            }
            else
            {
                error = VisaUiMessages.Format(
                    "ApplicationProfileInstance.Workspace.DateBetween",
                    Format(from),
                    Format(to));
            }

            return false;
        }

        if (min is DateTime after && date < after)
        {
            error = preceding != null
                ? VisaUiMessages.Format(
                    "ApplicationProfileInstance.Workspace.DateOnOrAfterPrecedingStep",
                    preceding,
                    Format(after))
                : VisaUiMessages.Format("ApplicationProfileInstance.Workspace.DateOnOrAfter", Format(after));
            return false;
        }

        if (max is DateTime before && date > before)
        {
            error = following != null
                ? VisaUiMessages.Format(
                    "ApplicationProfileInstance.Workspace.DateOnOrBeforeFollowingStep",
                    following,
                    Format(before))
                : VisaUiMessages.Format("ApplicationProfileInstance.Workspace.DateOnOrBefore", Format(before));
            return false;
        }

        error = null;
        return true;
    }

    public static string Format(DateTime date) =>
        date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Advance date must be on/after the previous timeline step and on/after this step's
    /// already-stored Started-on date (same day allowed).
    /// </summary>
    public static void ResolveAdvanceBounds(
        DateTime? precedingDate,
        string? precedingLabel,
        DateTime? currentStartedOn,
        string? currentStepLabel,
        DateTime? followingDate,
        string? followingLabel,
        out DateTime? earliest,
        out DateTime? latest,
        out string? earliestLabel,
        out string? latestLabel)
    {
        earliest = precedingDate?.Date;
        earliestLabel = precedingLabel;
        if (currentStartedOn is DateTime started && started != default)
        {
            var startedDate = started.Date;
            if (earliest is null || startedDate > earliest.Value)
            {
                earliest = startedDate;
                earliestLabel = currentStepLabel;
            }
        }

        latest = followingDate?.Date;
        latestLabel = followingLabel;
    }
}