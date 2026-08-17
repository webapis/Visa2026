using System;
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

        foreach (var link in links)
        {
            if (link?.LinkKind == null || link.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                continue;

            switch (link.LinkKind.Value)
            {
                case ApplicationProfileInstancePersonLinkKind.Passport:
                    item.CurrentPassport = objectSpace.GetObjectByKey<Passport>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.Visa:
                    item.CurrentVisa = objectSpace.GetObjectByKey<Visa>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.Education:
                    item.CurrentEducation = objectSpace.GetObjectByKey<Education>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                    item.CurrentAddressOfResidence = objectSpace.GetObjectByKey<AddressOfResidence>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.Position:
                    item.CurrentPositionHistory = objectSpace.GetObjectByKey<EmployeePositionHistory>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.WorkDuty:
                    item.CurrentWorkDuty = objectSpace.GetObjectByKey<WorkDuty>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.Salary:
                    item.CurrentSalary = objectSpace.GetObjectByKey<EmployeeSalary>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
                    item.CurrentMedicalRecord = objectSpace.GetObjectByKey<MedicalRecord>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.InvitationItem:
                    item.CurrentInvitationItem = objectSpace.GetObjectByKey<InvitationItem>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
                    item.CurrentWorkPermitItem = objectSpace.GetObjectByKey<WorkPermitItem>(linkedId);
                    break;
                case ApplicationProfileInstancePersonLinkKind.RejectionItem:
                    break;
                case ApplicationProfileInstancePersonLinkKind.BorderZoneItem:
                case ApplicationProfileInstancePersonLinkKind.TravelHistory:
                    break;
            }
        }

        return item;
    }
}
