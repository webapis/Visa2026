using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Cancelled / changed / used for issued documents. Not stored flags:
/// cancelled or changed when the document is linked on a completed
/// (<c>PROCESS_ISSUED</c>) Application Profile instance of that family;
/// invitation used when a visa points at the invitation line.
/// </summary>
public static class IssuedDocumentLifecycle
{
    public const int ActionFamilyCancellation = (int)ApplicationProfileActionFamily.Cancellation;
    public const int ActionFamilyChange = (int)ApplicationProfileActionFamily.Change;

    public static bool IsCancelled(InvitationItem? item) =>
        HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Cancellation);

    public static bool IsChanged(InvitationItem? item) =>
        !IsCancelled(item)
        && HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Change);

    public static bool IsUsed(InvitationItem? item) =>
        item?.IssuedVisa != null;

    public static bool IsCancelled(Visa? visa) =>
        HasCompletedFamily(visa?.ApplicationProfileInstances, ApplicationProfileActionFamily.Cancellation);

    public static bool IsChanged(Visa? visa) =>
        !IsCancelled(visa)
        && HasCompletedFamily(visa?.ApplicationProfileInstances, ApplicationProfileActionFamily.Change);

    public static bool IsCancelled(WorkPermitItem? item) =>
        HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Cancellation);

    public static bool IsChanged(WorkPermitItem? item) =>
        !IsCancelled(item)
        && HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Change);

    public static bool IsCancelled(BorderZoneItem? item) =>
        HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Cancellation);

    public static bool IsChanged(BorderZoneItem? item) =>
        !IsCancelled(item)
        && HasCompletedFamily(item?.ApplicationProfileInstances, ApplicationProfileActionFamily.Change);

    public static bool IsCancelled(BorderZone? zone) =>
        zone?.BorderZoneItems != null && zone.BorderZoneItems.Any(IsCancelled);

    public static bool HasCompletedFamily(
        IEnumerable<ApplicationProfileInstance>? instances,
        ApplicationProfileActionFamily family)
    {
        if (instances == null)
            return false;

        foreach (var instance in instances)
        {
            if (instance?.ApplicationProfile?.ActionFamily != family)
                continue;
            if (IsProcessIssued(instance))
                return true;
        }

        return false;
    }

    public static bool IsProcessIssued(ApplicationProfileInstance? instance)
    {
        if (instance == null)
            return false;

        var code = instance.LatestPrimaryStateCode;
        if (string.IsNullOrWhiteSpace(code))
            code = instance.LatestProgress?.State?.Code;
        return string.Equals(
            code?.Trim(),
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// EF-translatable: skip-nav Cancellation instance at PROCESS_ISSUED.
    /// Do not use <see cref="IsCancelled(Visa)"/> inside <c>IQueryable</c> — it is [NotMapped].
    /// </summary>
    public static IQueryable<Visa> WhereVisaNotCancelled(IQueryable<Visa> query)
    {
        var family = ApplicationProfileActionFamily.Cancellation;
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return query.Where(v => !v.ApplicationProfileInstances.Any(a =>
            a.ApplicationProfile != null
            && a.ApplicationProfile.ActionFamily == family
            && a.LatestPrimaryStateCode == issued));
    }

    public static IQueryable<InvitationItem> WhereInvitationItemNotCancelled(IQueryable<InvitationItem> query)
    {
        var family = ApplicationProfileActionFamily.Cancellation;
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return query.Where(i => !i.ApplicationProfileInstances.Any(a =>
            a.ApplicationProfile != null
            && a.ApplicationProfile.ActionFamily == family
            && a.LatestPrimaryStateCode == issued));
    }

    public static IQueryable<InvitationItem> WhereInvitationItemNotChanged(IQueryable<InvitationItem> query)
    {
        var family = ApplicationProfileActionFamily.Change;
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return query.Where(i => !i.ApplicationProfileInstances.Any(a =>
            a.ApplicationProfile != null
            && a.ApplicationProfile.ActionFamily == family
            && a.LatestPrimaryStateCode == issued));
    }

    public static IQueryable<InvitationItem> WhereInvitationItemNotUsed(IQueryable<InvitationItem> query) =>
        query.Where(i => i.IssuedVisa == null);

    public static IQueryable<InvitationItem> WhereInvitationItemUsed(IQueryable<InvitationItem> query) =>
        query.Where(i => i.IssuedVisa != null);

    public static IQueryable<WorkPermitItem> WhereWorkPermitItemNotCancelled(IQueryable<WorkPermitItem> query)
    {
        var family = ApplicationProfileActionFamily.Cancellation;
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return query.Where(w => !w.ApplicationProfileInstances.Any(a =>
            a.ApplicationProfile != null
            && a.ApplicationProfile.ActionFamily == family
            && a.LatestPrimaryStateCode == issued));
    }
}
