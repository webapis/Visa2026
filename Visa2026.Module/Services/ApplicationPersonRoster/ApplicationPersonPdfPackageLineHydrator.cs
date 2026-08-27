using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Builds a detached <see cref="ApplicationRosterMergeLine"/> projection from an (instance, person) pair
/// (resolved links only) for PDF mapping and <see cref="ApplicationSupportingDocumentsPacker"/> parity.
/// Not persisted — do not call <see cref="IObjectSpace.CommitChanges"/> for the projection.
/// </summary>
public static class ApplicationProfileInstancePersonPdfPackageLineHydrator
{
    public static ApplicationRosterMergeLine Hydrate(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (person == null)
            throw new ArgumentNullException(nameof(person));

        var trackedApplication = objectSpace.GetObject(application) ?? application;
        var trackedPerson = objectSpace.GetObject(person) ?? person;
        _ = trackedPerson?.Photo;

        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = trackedApplication,
            Person = trackedPerson,
            ApplicationItemName = trackedPerson?.FullName ?? string.Empty,
        };
        if (trackedPerson != null && trackedPerson.ID != Guid.Empty)
            item.ID = trackedPerson.ID;

        var links = ApplicationProfileInstancePersonResolver.LoadLinks(
            objectSpace,
            trackedApplication.ID,
            trackedPerson?.ID ?? Guid.Empty);

        var idsByKind = new Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>>();
        foreach (var link in links)
        {
            if (link?.LinkKind is not { } kind || link.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                continue;
            if (!idsByKind.TryGetValue(kind, out var ids))
            {
                ids = [];
                idsByKind[kind] = ids;
            }

            ids.Add(linkedId);
        }

        AssignPassports(objectSpace, item, idsByKind);
        AssignVisas(objectSpace, item, idsByKind);
        AssignInvitations(objectSpace, item, idsByKind);
        AssignWorkPermits(objectSpace, item, idsByKind);
        item.CurrentEducation = First<Education>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.Education);
        item.CurrentAddressOfResidence = First<AddressOfResidence>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.AddressOfResidence);
        item.CurrentPositionHistory = First<EmployeePositionHistory>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.Position);
        item.CurrentWorkDuty = First<WorkDuty>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.WorkDuty);
        item.CurrentSalary = First<EmployeeSalary>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.Salary);
        item.CurrentMedicalRecord = First<MedicalRecord>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.MedicalRecord);

        return item;
    }

    private static void AssignPassports(
        IObjectSpace objectSpace,
        ApplicationRosterMergeLine item,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind)
    {
        var passports = LoadMany<Passport>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.Passport)
            .OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
            .ThenByDescending(p => p.ID)
            .ToList();
        item.CurrentPassport = passports.ElementAtOrDefault(0);
        item.PreviousPassport = passports.ElementAtOrDefault(1);
    }

    private static void AssignVisas(
        IObjectSpace objectSpace,
        ApplicationRosterMergeLine item,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind)
    {
        var visas = LoadMany<Visa>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.Visa)
            .OrderByDescending(v => v.StartDate)
            .ThenByDescending(v => v.IssueDate)
            .ThenByDescending(v => v.ID)
            .ToList();
        item.CurrentVisa = visas.ElementAtOrDefault(0);
    }

    private static void AssignInvitations(
        IObjectSpace objectSpace,
        ApplicationRosterMergeLine item,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind)
    {
        var invitations = LoadMany<InvitationItem>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.InvitationItem)
            .OrderByDescending(i => i.Invitation?.IssuedDate ?? default)
            .ThenByDescending(i => i.ID)
            .ToList();
        item.CurrentInvitationItem = invitations.ElementAtOrDefault(0);
        item.PreviousInvitationItem = invitations.ElementAtOrDefault(1);
    }

    private static void AssignWorkPermits(
        IObjectSpace objectSpace,
        ApplicationRosterMergeLine item,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind)
    {
        var permits = LoadMany<WorkPermitItem>(objectSpace, idsByKind, ApplicationProfileInstancePersonLinkKind.WorkPermitItem)
            .OrderByDescending(w => w.StartDate)
            .ThenByDescending(w => w.ID)
            .ToList();
        item.CurrentWorkPermitItem = permits.ElementAtOrDefault(0);
        item.PreviousWorkPermitItem = permits.ElementAtOrDefault(1);
    }

    private static T? First<T>(
        IObjectSpace objectSpace,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind,
        ApplicationProfileInstancePersonLinkKind kind)
        where T : class =>
        LoadMany<T>(objectSpace, idsByKind, kind).FirstOrDefault();

    private static IEnumerable<T> LoadMany<T>(
        IObjectSpace objectSpace,
        Dictionary<ApplicationProfileInstancePersonLinkKind, List<Guid>> idsByKind,
        ApplicationProfileInstancePersonLinkKind kind)
        where T : class
    {
        if (!idsByKind.TryGetValue(kind, out var ids))
            yield break;

        foreach (var id in ids)
        {
            var entity = objectSpace.GetObjectByKey<T>(id);
            if (entity != null)
                yield return entity;
        }
    }
}
