using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Batch-stamps Cakylyklar ListView life keys and linked-detail captions.
/// </summary>
public static class InvitationItemCakylyklarListStamp
{
    public static void Apply(IObjectSpace? objectSpace, IReadOnlyList<InvitationItem> items)
    {
        if (objectSpace == null || items.Count == 0)
            return;

        var ids = items.Select(i => i.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, ids);
        var cancelled = IssuedDocumentLifecycle.LoadCancelledInvitationItemIds(objectSpace, ids);
        var changed = IssuedDocumentLifecycle.LoadChangedInvitationItemIds(objectSpace, ids);

        var visasByItem = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.IssuingInvitationItem != null && ids.Contains(v.IssuingInvitationItem.ID))
            .Select(v => new { ItemId = v.IssuingInvitationItem!.ID, VisaId = v.ID, Caption = v.VisaNumber })
            .ToList()
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var appIds = items
            .Select(i => i.Invitation?.ApplicationProfileInstance?.ID ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var personIds = items
            .Select(i => i.Person?.ID ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        // One WorkPermitItem per person per issuing case (match person + ApplicationProfileInstance).
        var workPermitsByAppPerson = new Dictionary<(Guid AppId, Guid PersonId), (Guid ItemId, Guid WorkPermitId, string Number)>();
        if (appIds.Count > 0 && personIds.Count > 0)
        {
            var wpRows = objectSpace.GetObjectsQuery<WorkPermitItem>()
                .Where(w => w.WorkPermit != null
                    && w.WorkPermit.ApplicationProfileInstance != null
                    && appIds.Contains(w.WorkPermit.ApplicationProfileInstance.ID)
                    && w.Person != null
                    && personIds.Contains(w.Person.ID))
                .Select(w => new
                {
                    AppId = w.WorkPermit!.ApplicationProfileInstance!.ID,
                    PersonId = w.Person!.ID,
                    ItemId = w.ID,
                    WorkPermitId = w.WorkPermit.ID,
                    Number = w.WorkPermitNumber,
                })
                .ToList();

            foreach (var row in wpRows)
            {
                var key = (row.AppId, row.PersonId);
                if (workPermitsByAppPerson.ContainsKey(key))
                    continue;
                workPermitsByAppPerson[key] = (
                    row.ItemId,
                    row.WorkPermitId,
                    row.Number?.Trim() ?? string.Empty);
            }
        }

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        var familyRows = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => ids.Contains(i.ID))
            .SelectMany(i => i.ApplicationProfileInstances
                .Where(a => a.ApplicationProfile != null
                    && a.LatestPrimaryStateCode == issued
                    && (a.ApplicationProfile.ActionFamily == ApplicationProfileActionFamily.Cancellation
                        || a.ApplicationProfile.ActionFamily == ApplicationProfileActionFamily.Change))
                .Select(a => new
                {
                    ItemId = i.ID,
                    PersonId = i.Person != null ? i.Person.ID : Guid.Empty,
                    Family = a.ApplicationProfile!.ActionFamily,
                    AppCaption = a.ProcessNumber ?? a.ID.ToString(),
                    AppId = a.ID,
                }))
            .ToList();

        var cancelCaption = familyRows
            .Where(r => r.Family == ApplicationProfileActionFamily.Cancellation)
            .GroupBy(r => r.ItemId)
            .ToDictionary(g => g.Key, g => g.First().AppCaption);

        var changeByItem = familyRows
            .Where(r => r.Family == ApplicationProfileActionFamily.Change)
            .GroupBy(r => r.ItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var changeAppIds = changeByItem.Values.Select(v => v.AppId).Distinct().ToList();
        var renewedByItem = new Dictionary<Guid, string>();
        if (changeAppIds.Count > 0)
        {
            var renewedRows = objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(ii => ii.Invitation != null
                    && ii.Invitation.ApplicationProfileInstance != null
                    && changeAppIds.Contains(ii.Invitation.ApplicationProfileInstance.ID)
                    && ii.Person != null)
                .Select(ii => new
                {
                    AppId = ii.Invitation!.ApplicationProfileInstance!.ID,
                    PersonId = ii.Person!.ID,
                    Number = ii.Invitation.InvitationNumber,
                })
                .ToList();

            foreach (var row in changeByItem.Values)
            {
                var match = renewedRows.FirstOrDefault(r =>
                    r.AppId == row.AppId
                    && (row.PersonId == Guid.Empty || r.PersonId == row.PersonId));
                if (match != null && !string.IsNullOrWhiteSpace(match.Number))
                    renewedByItem[row.ItemId] = match.Number!;
            }
        }

        foreach (var item in items)
        {
            var id = item.ID;
            var isCancelled = cancelled.Contains(id);
            var isChanged = !isCancelled && changed.Contains(id);
            var isUsed = used.Contains(id) || item.IssuedVisa != null;
            var expired = InvitationLaterLife.IsExpired(item.ExpirationDate);
            var life = InvitationLaterLife.Resolve(isCancelled, isChanged, isUsed, expired)
                ?? InvitationLaterLife.InForce;
            item.SetListViewLifeKey(life);

            item.SetListViewPersonName(item.Person?.FullName);
            item.SetListViewPassportNumber(item.Passport?.PassportNumber);
            item.SetListViewDepartmentName(
                item.Person != null
                    ? PersonCurrentItems.FormatEmployeeListDepartmentName(item.Person)
                    : null);
            item.SetListViewInvitationNumber(item.Invitation?.InvitationNumber);

            var owningApp = item.Invitation?.ApplicationProfileInstance;
            var applicationId = owningApp != null && owningApp.ID != Guid.Empty ? owningApp.ID : (Guid?)null;
            item.SetListViewApplicationProfileInstanceId(applicationId);
            item.SetListViewProcessNumber(
                InvitationItem.FormatProcessWithDate(owningApp?.ProcessNumber, owningApp?.ApplicationDate));

            var invitation = item.Invitation;
            item.SetListViewVisaLookups(
                LookupLocalization.GetDisplayName(
                    owningApp?.VisaType
                    ?? item.IssuedVisa?.VisaType),
                LookupLocalization.GetDisplayName(
                    invitation?.VisaCategory
                    ?? owningApp?.VisaCategory
                    ?? item.IssuedVisa?.VisaCategory),
                LookupLocalization.GetDisplayName(
                    invitation?.VisaPeriod
                    ?? owningApp?.VisaPeriod));

            var personId = item.Person?.ID ?? Guid.Empty;
            if (applicationId is Guid appId
                && appId != Guid.Empty
                && personId != Guid.Empty
                && workPermitsByAppPerson.TryGetValue((appId, personId), out var wp))
            {
                item.SetListViewWorkPermit(
                    string.IsNullOrWhiteSpace(wp.Number) ? null : wp.Number,
                    wp.WorkPermitId != Guid.Empty ? wp.WorkPermitId : null,
                    wp.ItemId != Guid.Empty ? wp.ItemId : null);
            }
            else
            {
                item.SetListViewWorkPermit(null, null, null);
            }

            string detail;
            Guid? visaId = null;
            if (life == InvitationLaterLife.Used)
            {
                if (visasByItem.TryGetValue(id, out var visa))
                {
                    detail = visa.Caption ?? "Visa";
                    visaId = visa.VisaId != Guid.Empty ? visa.VisaId : null;
                }
                else if (item.IssuedVisa != null)
                {
                    detail = item.IssuedVisa.VisaNumber ?? "Visa";
                    visaId = item.IssuedVisa.ID != Guid.Empty ? item.IssuedVisa.ID : null;
                }
                else
                {
                    detail = "Visa";
                }
            }
            else if (life == InvitationLaterLife.Cancelled)
            {
                detail = cancelCaption.TryGetValue(id, out var cancelApp) ? cancelApp : "Cancellation";
            }
            else if (life == InvitationLaterLife.Changed)
            {
                var changeApp = changeByItem.TryGetValue(id, out var ch) ? ch.AppCaption : "Change";
                renewedByItem.TryGetValue(id, out var renewedNum);
                detail = FormatChangedDetail(changeApp, renewedNum);
            }
            else if (life == InvitationLaterLife.ExpiredUnused)
            {
                detail = item.ExpirationDate?.ToString("dd.MM.yyyy") ?? "Expired";
            }
            else
            {
                // Taýýar (in force): expiry date + days remaining
                var exp = item.ExpirationDate;
                if (exp is { } d)
                {
                    var days = ExpirationLogicHelper.CalculateDaysRemaining(d);
                    detail = d.ToString("dd.MM.yyyy") + " · " + days + "d";
                }
                else
                {
                    detail = string.Empty;
                }
            }

            item.SetListViewLinkedDetail(detail);
            item.SetListViewVisaId(visaId);
        }
    }

    private static string FormatChangedDetail(string changeApp, string? renewedInvitation)
    {
        if (string.IsNullOrWhiteSpace(renewedInvitation))
            return changeApp;
        return changeApp + " -> " + renewedInvitation;
    }
}