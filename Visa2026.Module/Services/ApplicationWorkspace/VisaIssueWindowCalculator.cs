using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Days left to issue visas after the work-permit letter on
/// Wiza we Iş Rugsatnamasyny Uzaltmak (<c>extend_visa_wp</c>).
/// The badge sits on the list controls. Day 0 with visas still missing also turns the
/// extend_visa_wp list row green; the badge and the Wiza pill stay amber.
/// </summary>
public static class VisaIssueWindowCalculator
{
    public const string ProfileCode = "extend_visa_wp";
    public const int DefaultWindowDays = 45;
    public const int DefaultWarningDays = 5;

    public readonly record struct Badge(int Days, string Tone, bool RecolorVisaChip, string Title);

    public static bool Applies(string? profileCode) =>
        string.Equals(profileCode, ProfileCode, StringComparison.OrdinalIgnoreCase);

    public static int NormalizeWindowDays(int windowDays) =>
        windowDays > 0 ? windowDays : DefaultWindowDays;

    public static int NormalizeWarningDays(int warningDays, int windowDays)
    {
        var window = NormalizeWindowDays(windowDays);
        var warning = warningDays > 0 ? warningDays : DefaultWarningDays;
        return warning > window ? window : warning;
    }

    /// <summary>Calendar days from <paramref name="asOf"/> until the deadline. Zero on the deadline day and after it.</summary>
    public static int DaysLeft(DateTime workPermitIssued, DateTime asOf, int windowDays)
    {
        var days = (workPermitIssued.Date.AddDays(NormalizeWindowDays(windowDays)) - asOf.Date).Days;
        return days < 0 ? 0 : days;
    }

    public static Badge? ForCoverage(
        string? profileCode,
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips,
        DateTime? workPermitIssued,
        DateTime? lastVisaIssued,
        DateTime today,
        int windowDays,
        int warningDays)
    {
        if (!Applies(profileCode))
            return null;
        if (!TryChip(chips, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, out var workPermit))
            return null;
        if (!TryChip(chips, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, out var visa))
            return null;
        if (workPermit.ExpectedCount <= 0 || workPermit.CoverageCount < workPermit.ExpectedCount)
            return null;
        if (workPermitIssued is not { } issued || issued == default)
            return null;

        var window = NormalizeWindowDays(windowDays);
        var warning = NormalizeWarningDays(warningDays, window);
        var visasComplete = visa.ExpectedCount > 0 && visa.CoverageCount >= visa.ExpectedCount;
        if (visasComplete)
        {
            var asOf = lastVisaIssued is { } visaDate && visaDate != default ? visaDate : today;
            var left = DaysLeft(issued, asOf, window);
            return new Badge(left, "done", false, Title(issued, asOf, window, complete: true));
        }

        var open = DaysLeft(issued, today, window);
        var warn = open <= warning;
        return new Badge(open, warn ? "warn" : "ok", warn, Title(issued, today, window, complete: false));
    }

    private static string Title(DateTime issued, DateTime asOf, int windowDays, bool complete)
    {
        var letter = issued.ToString("dd.MM.yyyy");
        var deadline = issued.Date.AddDays(windowDays).ToString("dd.MM.yyyy");
        if (complete)
            return $"Soňky wiza {asOf:dd.MM.yyyy}. Iş rugsady {letter} · {deadline} çenli";
        return $"Iş rugsady {letter} · wiza {deadline} çenli";
    }

    private static bool TryChip(
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips,
        string key,
        out ApplicationWorkspaceIssuedResultListCoverage.Chip chip)
    {
        if (chips != null)
        {
            for (var i = 0; i < chips.Count; i++)
            {
                if (string.Equals(chips[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    chip = chips[i];
                    return true;
                }
            }
        }

        chip = default;
        return false;
    }
}
