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
        var candidates = ResolveEntities(objectSpace, trackedPerson, trackedApplication);
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
    /// Returns kinds that should be created: required by profile, under Last-N for that kind,
    /// not already sticky-linked to that object, and a valid candidate exists.
    /// Never replaces an existing LinkedObjectId.
    /// </summary>
    public static IReadOnlyList<(ApplicationProfileInstancePersonLinkKind Kind, Guid LinkedObjectId)> CollectMissingAutoLinks(
        ApplicationProfileInstance? application,
        IEnumerable<ApplicationProfileInstancePersonResolvedLink> existingLinks,
        IEnumerable<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)> candidates)
    {
        var existingIdsByKind = new Dictionary<ApplicationProfileInstancePersonLinkKind, HashSet<Guid>>();
        foreach (var link in existingLinks ?? [])
        {
            if (link?.LinkKind is not { } kind)
                continue;
            if (!existingIdsByKind.TryGetValue(kind, out var ids))
            {
                ids = [];
                existingIdsByKind[kind] = ids;
            }

            if (link.LinkedObjectId is Guid existingId && existingId != Guid.Empty)
                ids.Add(existingId);
        }

        var missing = new List<(ApplicationProfileInstancePersonLinkKind Kind, Guid LinkedObjectId)>();
        var addedByKind = new Dictionary<ApplicationProfileInstancePersonLinkKind, int>();
        foreach (var (kind, entity) in candidates ?? [])
        {
            if (!IsAutoLinkEnabled(application, kind))
                continue;
            if (entity is not BaseObject bo || bo.ID == Guid.Empty)
                continue;
            if (ApplicationProfileInstancePersonValidItems.EnforceOfficerLinkValidity
                && !ApplicationProfileInstancePersonValidItems.CanLinkEntity(entity))
                continue;

            if (!existingIdsByKind.TryGetValue(kind, out var existingIds))
            {
                existingIds = [];
                existingIdsByKind[kind] = existingIds;
            }

            if (existingIds.Contains(bo.ID))
                continue;

            var lastCount = ApplicationProfilePersonLastCount.For(application, kind);
            if (lastCount <= 0)
                continue;

            var already = existingIds.Count + addedByKind.GetValueOrDefault(kind);
            if (already >= lastCount)
                continue;

            missing.Add((kind, bo.ID));
            addedByKind[kind] = addedByKind.GetValueOrDefault(kind) + 1;
            existingIds.Add(bo.ID);
        }

        return missing;
    }

    public enum EnsureResolvedLinkDecision
    {
        None,
        Create,
        FillEmpty,
    }

    /// <summary>
    /// After an officer creates a person-owned BO from People &amp; links, pin that
    /// object onto the sticky ResolvedLink. Does not replace a non-empty LinkedObjectId.
    /// </summary>
    public static EnsureResolvedLinkDecision DecideEnsureResolvedLink(
        IEnumerable<ApplicationProfileInstancePersonResolvedLink>? existingLinks,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId,
        out ApplicationProfileInstancePersonResolvedLink? emptyRow) =>
        DecideEnsureResolvedLink(existingLinks, kind, linkedObjectId, lastCount: 1, out emptyRow);

    public static EnsureResolvedLinkDecision DecideEnsureResolvedLink(
        IEnumerable<ApplicationProfileInstancePersonResolvedLink>? existingLinks,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId,
        int lastCount,
        out ApplicationProfileInstancePersonResolvedLink? emptyRow)
    {
        emptyRow = null;
        if (linkedObjectId == Guid.Empty)
            return EnsureResolvedLinkDecision.None;

        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        var nonEmpty = 0;
        foreach (var link in existingLinks ?? [])
        {
            if (link?.LinkKind != kind)
                continue;
            if (link.LinkedObjectId == linkedObjectId)
                return EnsureResolvedLinkDecision.None;
            if (link.LinkedObjectId is Guid existingId && existingId != Guid.Empty)
            {
                nonEmpty++;
                continue;
            }

            emptyRow = link;
            return EnsureResolvedLinkDecision.FillEmpty;
        }

        if (nonEmpty >= lastCount)
            return EnsureResolvedLinkDecision.None;

        return EnsureResolvedLinkDecision.Create;
    }

    public static void EnsureResolvedLink(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedObjectId)
    {
        if (objectSpace == null || application == null || person == null || linkedObjectId == Guid.Empty)
            return;

        var trackedPerson = person.ID != Guid.Empty
            ? objectSpace.GetObject(person) ?? person
            : person;
        var trackedApplication = application.ID != Guid.Empty
            ? objectSpace.GetObject(application) ?? application
            : application;
        if (trackedPerson == null || trackedApplication == null)
            return;
        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(trackedApplication))
            return;

        var existing = LoadLinks(objectSpace, trackedApplication.ID, trackedPerson.ID);
        var lastCount = ApplicationProfilePersonLastCount.For(trackedApplication, kind);
        if (lastCount <= 0)
            return;

        var decision = DecideEnsureResolvedLink(existing, kind, linkedObjectId, lastCount, out var emptyRow);
        if (decision == EnsureResolvedLinkDecision.None)
            return;

        if (decision == EnsureResolvedLinkDecision.FillEmpty && emptyRow != null)
        {
            emptyRow.LinkedObjectId = linkedObjectId;
            ApplicationProfileInstanceChildMembership.Add(objectSpace, trackedApplication, kind, linkedObjectId);
            return;
        }

        var link = objectSpace.CreateObject<ApplicationProfileInstancePersonResolvedLink>();
        link.ApplicationProfileInstance = trackedApplication;
        link.Person = trackedPerson;
        link.LinkKind = kind;
        link.LinkedObjectId = linkedObjectId;
        trackedApplication.PersonResolvedLinks?.Add(link);
        ApplicationProfileInstanceChildMembership.Add(objectSpace, trackedApplication, kind, linkedObjectId);
    }

    public static IReadOnlyList<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)> ResolveEntities(
        IObjectSpace objectSpace,
        Person person,
        ApplicationProfileInstance? application = null)
    {
        var rows = new List<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)>();
        AddRange(rows, ApplicationProfileInstancePersonLinkKind.Passport,
            ApplicationProfileInstancePersonValidItems.ResolvePassports(
                person, ApplicationProfilePersonLastCount.For(application, ApplicationProfileInstancePersonLinkKind.Passport)));
        AddRange(rows, ApplicationProfileInstancePersonLinkKind.Visa,
            ApplicationProfileInstancePersonValidItems.ResolveVisas(
                person, ApplicationProfilePersonLastCount.For(application, ApplicationProfileInstancePersonLinkKind.Visa)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.Education, ApplicationProfileInstancePersonValidItems.ResolveEducation(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.AddressOfResidence, ApplicationProfileInstancePersonValidItems.ResolveAddress(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.Position, ApplicationProfileInstancePersonValidItems.ResolvePosition(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.WorkDuty, ApplicationProfileInstancePersonValidItems.ResolveWorkDuty(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.Salary, ApplicationProfileInstancePersonValidItems.ResolveSalary(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.MedicalRecord, ApplicationProfileInstancePersonValidItems.ResolveMedical(person)));
        AddRange(rows, ApplicationProfileInstancePersonLinkKind.InvitationItem,
            ApplicationProfileInstancePersonValidItems.ResolveInvitationItems(
                person, ApplicationProfilePersonLastCount.For(application, ApplicationProfileInstancePersonLinkKind.InvitationItem)));
        AddRange(rows, ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItems(
                person, ApplicationProfilePersonLastCount.For(application, ApplicationProfileInstancePersonLinkKind.WorkPermitItem)));
        AddRange(rows, ApplicationProfileInstancePersonLinkKind.BorderZoneItem,
            ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItems(
                objectSpace,
                person,
                ApplicationProfilePersonLastCount.For(application, ApplicationProfileInstancePersonLinkKind.BorderZoneItem)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.RejectionItem, ApplicationProfileInstancePersonValidItems.ResolveRejectionItem(person)));
        rows.Add((ApplicationProfileInstancePersonLinkKind.TravelHistory, ApplicationProfileInstancePersonValidItems.ResolveTravelHistory(person)));
        return rows;
    }

    private static void AddRange<T>(
        List<(ApplicationProfileInstancePersonLinkKind Kind, object? Entity)> rows,
        ApplicationProfileInstancePersonLinkKind kind,
        IEnumerable<T> entities)
    {
        foreach (var entity in entities ?? [])
        {
            if (entity != null)
                rows.Add((kind, entity));
        }
    }
}
