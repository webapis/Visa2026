using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

public static class ApplicationPersonResolver
{
    public static void RefreshResolvedLinks(IObjectSpace objectSpace, ApplicationPerson applicationPerson)
    {
        if (objectSpace == null || applicationPerson == null)
            return;

        var person = applicationPerson.Person
            ?? (applicationPerson.PersonId != Guid.Empty
                ? objectSpace.GetObjectByKey<Person>(applicationPerson.PersonId)
                : null);
        if (person == null)
            return;

        var existing = applicationPerson.ResolvedLinks?.ToList() ?? [];
        foreach (var link in existing)
            objectSpace.Delete(link);

        foreach (var (kind, entity) in ResolveEntities(objectSpace, person))
        {
            if (entity is not BaseObject bo || bo.ID == Guid.Empty)
                continue;

            var link = objectSpace.CreateObject<ApplicationPersonResolvedLink>();
            link.ApplicationPerson = applicationPerson;
            link.LinkKind = kind;
            link.LinkedObjectId = bo.ID;
        }
    }

    public static IReadOnlyList<(ApplicationPersonLinkKind Kind, object? Entity)> ResolveEntities(
        IObjectSpace objectSpace,
        Person person) =>
    [
        (ApplicationPersonLinkKind.Passport, ApplicationPersonValidItems.ResolvePassport(person)),
        (ApplicationPersonLinkKind.Visa, ApplicationPersonValidItems.ResolveVisa(person)),
        (ApplicationPersonLinkKind.Education, ApplicationPersonValidItems.ResolveEducation(person)),
        (ApplicationPersonLinkKind.AddressOfResidence, ApplicationPersonValidItems.ResolveAddress(person)),
        (ApplicationPersonLinkKind.Position, ApplicationPersonValidItems.ResolvePosition(person)),
        (ApplicationPersonLinkKind.Salary, ApplicationPersonValidItems.ResolveSalary(person)),
        (ApplicationPersonLinkKind.MedicalRecord, ApplicationPersonValidItems.ResolveMedical(person)),
        (ApplicationPersonLinkKind.InvitationItem, ApplicationPersonValidItems.ResolveInvitationItem(person)),
        (ApplicationPersonLinkKind.WorkPermitItem, ApplicationPersonValidItems.ResolveWorkPermitItem(person)),
        (ApplicationPersonLinkKind.BorderZoneItem, ApplicationPersonValidItems.ResolveBorderZoneItem(objectSpace, person)),
        (ApplicationPersonLinkKind.RejectionItem, ApplicationPersonValidItems.ResolveRejectionItem(person)),
        (ApplicationPersonLinkKind.TravelHistory, ApplicationPersonValidItems.ResolveTravelHistory(person)),
    ];
}
