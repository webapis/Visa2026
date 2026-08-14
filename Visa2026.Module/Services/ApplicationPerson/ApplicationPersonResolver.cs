using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Auto-resolves person-related links onto <see cref="ApplicationProfileInstancePersonResolvedLink"/>
/// per plan §10.1: RequirePerson* gate, sticky LinkedObjectId, toggle-off keeps existing.
/// </summary>
public static class ApplicationProfileInstancePersonResolver
{
    public static void RefreshResolvedLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return;

        var trackedPerson = person.ID != Guid.Empty
            ? objectSpace.GetObject(person) ?? person
            : person;
        var trackedApplication = application.ID != Guid.Empty
            ? objectSpace.GetObject(application) ?? application
            : application;

        if (trackedPerson == null)
            return;

        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(trackedApplication))
            return;

        var existing = LoadLinks(objectSpace, trackedApplication.ID, trackedPerson.ID);
        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(objectSpace, trackedApplication, existing);
        var candidates = ResolveEntities(objectSpace, trackedPerson);
        foreach (var (kind, linkedObjectId) in CollectMissingAutoLinks(trackedApplication, existing, candidates))
        {
            var link = objectSpace.CreateObject<ApplicationProfileInstancePersonResolvedLink>();
            link.ApplicationProfileInstance = trackedApplication;
            link.Person = trackedPerson;
            link.LinkKind = kind;
            link.LinkedObjectId = linkedObjectId;
            trackedApplication.PersonResolvedLinks?.Add(link);
            ApplicationProfileInstanceChildMembership.Add(objectSpace, trackedApplication, kind, linkedObjectId);
        }
    }

    public static IList<ApplicationProfileInstancePersonResolvedLink> LoadLinks(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return [];

        return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == applicationId && l.PersonId == personId)
            .ToList();
    }

    /// <summary>
    /// Whether the instance profile (or Type fallback) allows new auto-links of this kind.
    /// Existing sticky links are kept even when this returns false (§10.1 #12).
    /// </summary>
    public static bool IsAutoLinkEnabled(ApplicationProfileInstance? application, ApplicationProfileInstancePersonLinkKind kind) =>
        kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport =>
                ApplicationProfileConfigurationResolver.ShowPreviousPassport(application),
            ApplicationProfileInstancePersonLinkKind.Visa =>
                ApplicationProfileConfigurationResolver.ShowCurrentVisa(application),
            ApplicationProfileInstancePersonLinkKind.Education =>
                ApplicationProfileConfigurationResolver.ShowCurrentEducation(application),
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence =>
                ApplicationProfileConfigurationResolver.ShowCurrentAddressOfResidence(application),
            ApplicationProfileInstancePersonLinkKind.Position =>
                ApplicationProfileConfigurationResolver.ShowCurrentWorkDuty(application),
            ApplicationProfileInstancePersonLinkKind.WorkDuty =>
                ApplicationProfileConfigurationResolver.ShowCurrentWorkDuty(application),
            ApplicationProfileInstancePersonLinkKind.Salary =>
                ApplicationProfileConfigurationResolver.ShowCurrentSalary(application),
            ApplicationProfileInstancePersonLinkKind.MedicalRecord =>
                ApplicationProfileConfigurationResolver.ShowCurrentMedicalRecord(application),
            ApplicationProfileInstancePersonLinkKind.InvitationItem =>
                ApplicationProfileConfigurationResolver.ShowCurrentInvitationItem(application),
            ApplicationProfileInstancePersonLinkKind.WorkPermitItem =>
                ApplicationProfileConfigurationResolver.ShowCurrentWorkPermitItem(application),
            ApplicationProfileInstancePersonLinkKind.BorderZoneItem =>
                ApplicationProfileConfigurationResolver.RequirePersonBorderZoneItem(application),
            ApplicationProfileInstancePersonLinkKind.RejectionItem =>
                ApplicationProfileConfigurationResolver.RequirePersonRejectionItem(application),
            ApplicationProfileInstancePersonLinkKind.TravelHistory =>
                ApplicationProfileConfigurationResolver.RequirePersonTravelHistory(application),
            _ => false,
        };

    /// <summary>
    /// Returns kinds that should be created: required by profile, not already sticky-linked,
    /// and a valid candidate entity exists. Never replaces an existing LinkedObjectId.
    /// </summary>
    public static IReadOnlyList<(ApplicationProfileInstancePersonLinkKind Kind, Guid LinkedObjectId)> CollectMissingAutoLinks(
        ApplicationProfileInstance? application,
        IEnumerable<ApplicationProfileInstancePersonResolvedLink> existingLinks,
        IEnumerable<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)> candidates)
    {
        var existingKinds = new HashSet<ApplicationProfileInstancePersonLinkKind>();
        foreach (var link in existingLinks ?? [])
        {
            if (link?.LinkKind is { } kind)
                existingKinds.Add(kind);
        }

        var missing = new List<(ApplicationProfileInstancePersonLinkKind Kind, Guid LinkedObjectId)>();
        foreach (var (kind, entity) in candidates ?? [])
        {
            if (existingKinds.Contains(kind))
                continue;
            if (!IsAutoLinkEnabled(application, kind))
                continue;
            if (entity is not BaseObject bo || bo.ID == Guid.Empty)
                continue;

            missing.Add((kind, bo.ID));
            existingKinds.Add(kind);
        }

        return missing;
    }

    public static IReadOnlyList<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)> ResolveEntities(
        IObjectSpace objectSpace,
        Person person) =>
    [
        (ApplicationProfileInstancePersonLinkKind.Passport, ApplicationProfileInstancePersonValidItems.ResolvePassport(person)),
        (ApplicationProfileInstancePersonLinkKind.Visa, ApplicationProfileInstancePersonValidItems.ResolveVisa(person)),
        (ApplicationProfileInstancePersonLinkKind.Education, ApplicationProfileInstancePersonValidItems.ResolveEducation(person)),
        (ApplicationProfileInstancePersonLinkKind.AddressOfResidence, ApplicationProfileInstancePersonValidItems.ResolveAddress(person)),
        (ApplicationProfileInstancePersonLinkKind.Position, ApplicationProfileInstancePersonValidItems.ResolvePosition(person)),
        (ApplicationProfileInstancePersonLinkKind.WorkDuty, ApplicationProfileInstancePersonValidItems.ResolveWorkDuty(person)),
        (ApplicationProfileInstancePersonLinkKind.Salary, ApplicationProfileInstancePersonValidItems.ResolveSalary(person)),
        (ApplicationProfileInstancePersonLinkKind.MedicalRecord, ApplicationProfileInstancePersonValidItems.ResolveMedical(person)),
        (ApplicationProfileInstancePersonLinkKind.InvitationItem, ApplicationProfileInstancePersonValidItems.ResolveInvitationItem(person)),
        (ApplicationProfileInstancePersonLinkKind.WorkPermitItem, ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person)),
        (ApplicationProfileInstancePersonLinkKind.BorderZoneItem, ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItem(objectSpace, person)),
        (ApplicationProfileInstancePersonLinkKind.RejectionItem, ApplicationProfileInstancePersonValidItems.ResolveRejectionItem(person)),
        (ApplicationProfileInstancePersonLinkKind.TravelHistory, ApplicationProfileInstancePersonValidItems.ResolveTravelHistory(person)),
    ];
}
