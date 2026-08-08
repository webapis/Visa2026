using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Builds a detached <see cref="ApplicationItem"/> projection from an <see cref="ApplicationPerson"/> roster row
/// (resolved links only) for PDF mapping and <see cref="ApplicationSupportingDocumentsPacker"/> parity.
/// Not persisted — do not call <see cref="IObjectSpace.CommitChanges"/> for the projection.
/// </summary>
public static class ApplicationPersonPdfPackageLineHydrator
{
    public static ApplicationItem Hydrate(IObjectSpace objectSpace, ApplicationPerson applicationPerson)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (applicationPerson == null)
            throw new ArgumentNullException(nameof(applicationPerson));

        applicationPerson = objectSpace.GetObject(applicationPerson);
        var application = applicationPerson.Application != null
            ? objectSpace.GetObject(applicationPerson.Application)
            : null;
        var person = applicationPerson.Person != null
            ? objectSpace.GetObject(applicationPerson.Person)
            : null;

        var item = new ApplicationItem
        {
            Application = application,
            Person = person,
            ApplicationItemName = person?.FullName ?? string.Empty,
        };

        foreach (var link in applicationPerson.ResolvedLinks?.ToList() ?? [])
        {
            if (link?.LinkKind == null || link.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                continue;

            switch (link.LinkKind.Value)
            {
                case ApplicationPersonLinkKind.Passport:
                    item.CurrentPassport = objectSpace.GetObjectByKey<Passport>(linkedId);
                    break;
                case ApplicationPersonLinkKind.Visa:
                    item.CurrentVisa = objectSpace.GetObjectByKey<Visa>(linkedId);
                    break;
                case ApplicationPersonLinkKind.Education:
                    item.CurrentEducation = objectSpace.GetObjectByKey<Education>(linkedId);
                    break;
                case ApplicationPersonLinkKind.AddressOfResidence:
                    item.CurrentAddressOfResidence = objectSpace.GetObjectByKey<AddressOfResidence>(linkedId);
                    break;
                case ApplicationPersonLinkKind.Position:
                    item.CurrentPositionHistory = objectSpace.GetObjectByKey<EmployeePositionHistory>(linkedId);
                    break;
                case ApplicationPersonLinkKind.Salary:
                    item.CurrentSalary = objectSpace.GetObjectByKey<EmployeeSalary>(linkedId);
                    break;
                case ApplicationPersonLinkKind.MedicalRecord:
                    item.CurrentMedicalRecord = objectSpace.GetObjectByKey<MedicalRecord>(linkedId);
                    break;
                case ApplicationPersonLinkKind.InvitationItem:
                    item.CurrentInvitationItem = objectSpace.GetObjectByKey<InvitationItem>(linkedId);
                    break;
                case ApplicationPersonLinkKind.WorkPermitItem:
                    item.CurrentWorkPermitItem = objectSpace.GetObjectByKey<WorkPermitItem>(linkedId);
                    break;
                case ApplicationPersonLinkKind.RejectionItem:
                    break;
                case ApplicationPersonLinkKind.BorderZoneItem:
                case ApplicationPersonLinkKind.TravelHistory:
                    break;
            }
        }

        return item;
    }
}
