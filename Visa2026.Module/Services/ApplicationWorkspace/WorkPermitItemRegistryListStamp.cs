using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Batch-stamps Work Permits (Employee) registry ListView life keys and linked-detail captions.
/// </summary>
public static class WorkPermitItemRegistryListStamp
{
    public static void Apply(IObjectSpace? objectSpace, IReadOnlyList<WorkPermitItem> items)
    {
        if (objectSpace == null || items.Count == 0)
            return;

        var ids = items.Select(i => i.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var cancelled = IssuedDocumentLifecycle.LoadCancelledWorkPermitItemIds(objectSpace, ids);
        var changed = IssuedDocumentLifecycle.LoadChangedWorkPermitItemIds(objectSpace, ids);

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        var familyRows = objectSpace.GetObjectsQuery<WorkPermitItem>()
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
            var renewedRows = objectSpace.GetObjectsQuery<WorkPermitItem>()
                .Where(ii => ii.WorkPermit != null
                    && ii.WorkPermit.ApplicationProfileInstance != null
                    && changeAppIds.Contains(ii.WorkPermit.ApplicationProfileInstance.ID)
                    && ii.Person != null)
                .Select(ii => new
                {
                    AppId = ii.WorkPermit!.ApplicationProfileInstance!.ID,
                    PersonId = ii.Person!.ID,
                    Number = ii.WorkPermitNumber,
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
            var expired = WorkPermitItemLaterLife.IsExpired(item.ExpirationDate);
            var life = WorkPermitItemLaterLife.Resolve(isCancelled, isChanged, expired)
                ?? WorkPermitItemLaterLife.InForce;
            item.SetListViewLifeKey(life);

            item.SetListViewPersonName(item.Person?.FullName);
            item.SetListViewPassportNumber(item.Passport?.PassportNumber);
            item.SetListViewDepartmentName(
                item.Person != null
                    ? PersonCurrentItems.FormatEmployeeListDepartmentName(item.Person)
                    : null);
            item.SetListViewWorkPermitNumber(
                string.IsNullOrWhiteSpace(item.WorkPermitNumber) ? null : item.WorkPermitNumber.Trim());

            var owningApp = item.WorkPermit?.ApplicationProfileInstance;
            var applicationId = owningApp != null && owningApp.ID != Guid.Empty ? owningApp.ID : (Guid?)null;
            item.SetListViewApplicationProfileInstanceId(applicationId);
            item.SetListViewProcessNumber(
                WorkPermitItem.FormatProcessWithDate(owningApp?.ProcessNumber, owningApp?.ApplicationDate));

            var workPermitId = item.WorkPermit != null && item.WorkPermit.ID != Guid.Empty
                ? item.WorkPermit.ID
                : (Guid?)null;
            item.SetListViewWorkPermitId(workPermitId);

            string detail;
            if (life == WorkPermitItemLaterLife.Cancelled)
            {
                detail = cancelCaption.TryGetValue(id, out var cancelApp) ? cancelApp : "Cancellation";
            }
            else if (life == WorkPermitItemLaterLife.Changed)
            {
                var changeApp = changeByItem.TryGetValue(id, out var ch) ? ch.AppCaption : "Change";
                renewedByItem.TryGetValue(id, out var renewedNum);
                detail = FormatChangedDetail(changeApp, renewedNum);
            }
            else if (life == WorkPermitItemLaterLife.Expired)
            {
                detail = item.ExpirationDate.ToString("dd.MM.yyyy");
            }
            else
            {
                var days = ExpirationLogicHelper.CalculateDaysRemaining(item.ExpirationDate, forceZero: false);
                detail = item.ExpirationDate.ToString("dd.MM.yyyy") + " · " + days + "d";
            }

            item.SetListViewLinkedDetail(detail);
        }
    }

    private static string FormatChangedDetail(string changeApp, string? renewedNumber)
    {
        if (string.IsNullOrWhiteSpace(renewedNumber))
            return changeApp;
        return changeApp + " -> " + renewedNumber;
    }
}