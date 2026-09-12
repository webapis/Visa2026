using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Keeps skip-navigation child M2M collections in sync with sticky ResolvedLinks.
/// Officers still only link/unlink Person for person-related children (§10.1 #5).
/// Output headers (Invitation / WorkPermit / BorderZone / Rejection / IssuedVisas) are 1:N via the issuing FK — not skip-nav.
/// </summary>
public static class ApplicationProfileInstanceChildMembership
{
    public static void SyncFromResolvedLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        IEnumerable<ApplicationProfileInstancePersonResolvedLink> links)
    {
        if (objectSpace == null || application == null)
            return;

        foreach (var link in links ?? [])
        {
            if (link?.LinkKind is not { } kind || link.LinkedObjectId is not Guid id || id == Guid.Empty)
                continue;
            Add(objectSpace, application, kind, id);
        }
    }

    public static void Add(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId)
    {
        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                AddUnique(application.Passports ??= New<Passport>(), objectSpace.GetObjectByKey<Passport>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.Visa:
                AddUnique(application.Visas ??= New<Visa>(), objectSpace.GetObjectByKey<Visa>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.Education:
                AddUnique(application.Educations ??= New<Education>(), objectSpace.GetObjectByKey<Education>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                AddUnique(application.AddressesOfResidence ??= New<AddressOfResidence>(), objectSpace.GetObjectByKey<AddressOfResidence>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.Position:
                AddUnique(application.PositionHistories ??= New<EmployeePositionHistory>(), objectSpace.GetObjectByKey<EmployeePositionHistory>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.Salary:
                AddUnique(application.Salaries ??= New<EmployeeSalary>(), objectSpace.GetObjectByKey<EmployeeSalary>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
                AddUnique(application.MedicalRecords ??= New<MedicalRecord>(), objectSpace.GetObjectByKey<MedicalRecord>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.WorkDuty:
                AddUnique(application.WorkDuties ??= New<WorkDuty>(), objectSpace.GetObjectByKey<WorkDuty>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.InvitationItem:
                AddUnique(application.InvitationItems ??= New<InvitationItem>(), objectSpace.GetObjectByKey<InvitationItem>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
                AddUnique(application.WorkPermitItems ??= New<WorkPermitItem>(), objectSpace.GetObjectByKey<WorkPermitItem>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.BorderZoneItem:
                AddUnique(application.BorderZoneItems ??= New<BorderZoneItem>(), objectSpace.GetObjectByKey<BorderZoneItem>(linkedObjectId));
                break;
            case ApplicationProfileInstancePersonLinkKind.TravelHistory:
                AddUnique(application.TravelHistories ??= New<TravelHistory>(), objectSpace.GetObjectByKey<TravelHistory>(linkedObjectId));
                break;
        }
    }

    public static void Remove(
        ApplicationProfileInstance application,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId,
        IObjectSpace? objectSpace = null)
    {
        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                RemoveById(application.Passports, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<Passport>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.Visa:
                RemoveById(application.Visas, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<Visa>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.Education:
                RemoveById(application.Educations, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<Education>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                RemoveById(application.AddressesOfResidence, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<AddressOfResidence>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.Position:
                RemoveById(application.PositionHistories, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<EmployeePositionHistory>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.Salary:
                RemoveById(application.Salaries, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<EmployeeSalary>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
                RemoveById(application.MedicalRecords, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<MedicalRecord>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.WorkDuty:
                RemoveById(application.WorkDuties, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<WorkDuty>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.InvitationItem:
                RemoveById(application.InvitationItems, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<InvitationItem>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
                RemoveById(application.WorkPermitItems, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<WorkPermitItem>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.BorderZoneItem:
                RemoveById(application.BorderZoneItems, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<BorderZoneItem>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
            case ApplicationProfileInstancePersonLinkKind.TravelHistory:
                RemoveById(application.TravelHistories, linkedObjectId);
                RemoveInverse(objectSpace?.GetObjectByKey<TravelHistory>(linkedObjectId)?.ApplicationProfileInstances, application);
                break;
        }
    }

    private static ObservableCollection<T> New<T>() => new();

    private static void AddUnique<T>(IList<T> list, T? entity)
        where T : BaseObject
    {
        if (entity == null || list == null)
            return;
        if (list.Any(x => x != null && x.ID == entity.ID))
            return;
        list.Add(entity);
    }

    private static void RemoveById<T>(IList<T>? list, Guid id)
        where T : BaseObject
    {
        if (list == null)
            return;
        var match = list.FirstOrDefault(x => x != null && x.ID == id);
        if (match != null)
            list.Remove(match);
    }

    private static void RemoveInverse(
        IList<ApplicationProfileInstance>? instances,
        ApplicationProfileInstance application)
    {
        if (instances == null)
            return;
        var match = instances.FirstOrDefault(a => a != null && a.ID == application.ID);
        if (match != null)
            instances.Remove(match);
    }
}