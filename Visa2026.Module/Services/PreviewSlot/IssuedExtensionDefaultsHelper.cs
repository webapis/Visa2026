using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Prefill helpers for issued Visa / WorkPermit on non-invitation cases:
/// linked document end date + case <see cref="VisaPeriod"/> duration.
/// </summary>
public static class IssuedExtensionDefaultsHelper
{
    public const string UnitMonths = "AY";
    public const string UnitDays = "GUN";
    public const string UnitYears = "YYL";

    /// <summary>
    /// Adds case visa-period duration to <paramref name="start"/> using
    /// <see cref="VisaPeriod.PdfForm__Code"/> (<c>AY</c>/<c>GUN</c>/<c>YYL</c>) and <see cref="VisaPeriod.PdfForm_Count"/>.
    /// </summary>
    public static bool TryAddVisaPeriod(DateTime start, VisaPeriod? period, out DateTime end)
    {
        end = default;
        if (period == null || period.PdfForm_Count <= 0)
            return false;

        var count = period.PdfForm_Count;
        var unit = ResolveUnit(period);
        if (unit == null)
            return false;

        var s = start.Date;
        end = unit switch
        {
            UnitMonths => s.AddMonths(count),
            UnitDays => s.AddDays(count),
            UnitYears => s.AddYears(count),
            _ => default,
        };
        return end != default;
    }

    /// <summary>
    /// Start = linked end + 1 day (clamped to issue/today if earlier).
    /// End = linked end + case period when still after Start; otherwise clamped Start + period.
    /// </summary>
    public static bool TryComputeExtensionDates(
        DateTime? linkedEnd,
        DateTime issueOrToday,
        VisaPeriod? period,
        out DateTime start,
        out DateTime? end,
        out string note)
    {
        start = default;
        end = null;
        note = string.Empty;

        if (linkedEnd is not DateTime linked || linked == default)
        {
            note = "No linked document end date — enter Start and End.";
            return false;
        }

        var linkedDate = linked.Date;
        var issue = issueOrToday.Date;
        var dayAfterLinkedEnd = linkedDate.AddDays(1);
        start = dayAfterLinkedEnd < issue ? issue : dayAfterLinkedEnd;

        if (!TryAddVisaPeriod(linkedDate, period, out var fromLinked))
        {
            note = $"Start day after linked end {linkedDate:dd MMM yyyy}; case period missing or not computable — enter End.";
            return true;
        }

        if (fromLinked.Date > start.Date)
        {
            end = fromLinked.Date;
        }
        else if (TryAddVisaPeriod(start, period, out var fromClamped) && fromClamped.Date > start.Date)
        {
            end = fromClamped.Date;
        }

        note = end.HasValue
            ? $"Start day after linked end {linkedDate:dd MMM yyyy}; End = linked end + case period."
            : $"Start day after linked end {linkedDate:dd MMM yyyy}; enter End.";
        return true;
    }

    public static Visa? TryResolveLinkedVisa(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? instance,
        Person? person)
    {
        if (objectSpace == null || person == null)
            return null;

        if (TryGetResolvedLinkedId(instance, person.ID, ApplicationProfileInstancePersonLinkKind.Visa, out var linkedId))
        {
            var fromLink = objectSpace.GetObjectByKey<Visa>(linkedId);
            if (fromLink != null)
                return fromLink;
        }

        return PersonCurrentItems.GetCurrentVisa(person);
    }

    public static WorkPermitItem? TryResolveLinkedWorkPermitItem(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? instance,
        Person? person)
    {
        if (person == null)
            return null;

        if (objectSpace != null
            && TryGetResolvedLinkedId(instance, person.ID, ApplicationProfileInstancePersonLinkKind.WorkPermitItem, out var linkedId))
        {
            var fromLink = objectSpace.GetObjectByKey<WorkPermitItem>(linkedId);
            if (fromLink != null)
                return fromLink;
        }

        return PersonCurrentItems.GetCurrentWorkPermitItem(person);
    }

    public static string ResolveWorkPermitLocations(WorkPermitItem? linked, string? caseDefaultLocations)
    {
        var fromLinked = linked?.WorkPermittedLocations?.Trim();
        if (!string.IsNullOrWhiteSpace(fromLinked))
            return fromLinked;

        return caseDefaultLocations?.Trim() ?? string.Empty;
    }

    private static bool TryGetResolvedLinkedId(
        ApplicationProfileInstance? instance,
        Guid personId,
        ApplicationProfileInstancePersonLinkKind kind,
        out Guid linkedId)
    {
        linkedId = Guid.Empty;
        if (instance?.PersonResolvedLinks == null || personId == Guid.Empty)
            return false;

        var link = instance.PersonResolvedLinks
            .Where(l => l != null
                && l.PersonId == personId
                && l.LinkKind == kind
                && l.LinkedObjectId is Guid id
                && id != Guid.Empty)
            .OrderByDescending(l => l.LinkedObjectId)
            .FirstOrDefault();

        if (link?.LinkedObjectId is not Guid id || id == Guid.Empty)
            return false;

        linkedId = id;
        return true;
    }

    private static string? ResolveUnit(VisaPeriod period)
    {
        var code = period.PdfForm__Code?.Trim().ToUpperInvariant();
        if (code is UnitMonths or UnitDays or UnitYears)
            return code;

        var key = period.LocalizationKey?.Trim() ?? string.Empty;
        if (key.StartsWith("Month", StringComparison.OrdinalIgnoreCase))
            return UnitMonths;
        if (key.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
            return UnitDays;
        if (key.StartsWith("Year", StringComparison.OrdinalIgnoreCase))
            return UnitYears;

        // Legacy contract merge treated PdfForm_Count as months when unit was blank.
        if (period.PdfForm_Count > 0 && string.IsNullOrWhiteSpace(code))
            return UnitMonths;

        return null;
    }
}