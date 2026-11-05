using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Upserts case↔person requested locations (WP / BZ). Join and tick write here only —
/// never onto <see cref="Visa"/>, <see cref="WorkPermitItem"/>, or <see cref="BorderZoneItem"/>.
/// </summary>
public static class CasePersonRequestedLocation
{
    public static string? GetLocations(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        ApplicationProfileInstancePersonRequestedLocationKind kind)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return null;

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        var row = Find(objectSpace, application, applicationId, personId, kind);
        var value = row?.Locations?.Trim();
        return string.IsNullOrWhiteSpace(value) || value == "—" ? null : value;
    }

    public static bool Matches(string? stored, string? caseLocations)
    {
        if (string.IsNullOrWhiteSpace(caseLocations))
            return false;
        return string.Equals(
            stored?.Trim(),
            caseLocations.Trim(),
            StringComparison.Ordinal);
    }

    public static bool OfferTick(string? caseLocations, string? stored) =>
        !string.IsNullOrWhiteSpace(caseLocations) && !Matches(stored, caseLocations);

    /// <summary>
    /// Application-result prefill: Joined requested location, else Case (or other) fallback.
    /// </summary>
    public static string ResolveForPrefill(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        ApplicationProfileInstancePersonRequestedLocationKind kind,
        string? caseFallback)
    {
        var requested = GetLocations(objectSpace, applicationId, personId, kind);
        if (!string.IsNullOrWhiteSpace(requested))
            return requested;
        return string.IsNullOrWhiteSpace(caseFallback) ? string.Empty : caseFallback.Trim();
    }

    /// <summary>
    /// Invitation header has one border-zone field: first roster person with a Joined
    /// requested border zone, else Case fallback.
    /// </summary>
    public static string ResolveBorderZoneForInvitationHeader(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        string? caseFallback)
    {
        if (objectSpace != null && application != null && application.ID != Guid.Empty)
        {
            var people = application.People?
                .Where(p => p != null && p.ID != Guid.Empty)
                .OrderByDescending(p => p!.IsEmployee)
                .ThenBy(p => p!.LastName)
                .ThenBy(p => p!.FirstName)
                .ToList() ?? [];
            foreach (var person in people)
            {
                var requested = GetLocations(
                    objectSpace,
                    application.ID,
                    person!.ID,
                    ApplicationProfileInstancePersonRequestedLocationKind.BorderZone);
                if (!string.IsNullOrWhiteSpace(requested))
                    return requested;
            }
        }

        if (!string.IsNullOrWhiteSpace(caseFallback))
            return caseFallback.Trim();
        return BorderZoneSelectionHelper.NoneValue;
    }

    /// <summary>
    /// Join / Save: replace the person×kind row with the chip union (create, update, or clear).
    /// </summary>
    public static bool Upsert(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonRequestedLocationKind kind,
        string? locations)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return false;
        if (application.ID == Guid.Empty)
            return false;

        var trimmed = Normalize(locations);
        var existing = Find(objectSpace, application, application.ID, person.ID, kind);
        if (trimmed == null)
        {
            if (existing == null)
                return false;
            application.PersonRequestedLocations?.Remove(existing);
            objectSpace.Delete(existing);
            return true;
        }

        if (existing == null)
        {
            existing = objectSpace.CreateObject<ApplicationProfileInstancePersonRequestedLocation>();
            existing.ApplicationProfileInstance = application;
            existing.ApplicationProfileInstanceId = application.ID;
            existing.Person = person;
            existing.PersonId = person.ID;
            existing.Kind = kind;
            application.PersonRequestedLocations?.Add(existing);
        }

        if (string.Equals(existing.Locations?.Trim(), trimmed, StringComparison.Ordinal))
            return false;

        existing.Locations = trimmed;
        return true;
    }

    /// <summary>Fill empty BO from Case; do not overwrite a non-empty requested value.</summary>
    public static bool UpsertIfEmpty(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonRequestedLocationKind kind,
        string? caseLocations)
    {
        var trimmed = Normalize(caseLocations);
        if (trimmed == null)
            return false;
        if (!string.IsNullOrWhiteSpace(GetLocations(objectSpace, application.ID, person.ID, kind)))
            return false;
        return Upsert(objectSpace, application, person, kind, trimmed);
    }

    public static void DeleteForPersonOnCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId)
    {
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return;

        foreach (var row in EnumerateForPerson(objectSpace, applicationId, personId).ToList())
            objectSpace.Delete(row);
    }

    public static void DeleteForPerson(IObjectSpace objectSpace, Guid personId)
    {
        if (objectSpace == null || personId == Guid.Empty)
            return;

        foreach (var row in objectSpace.GetObjects<ApplicationProfileInstancePersonRequestedLocation>()
                     .Where(r => r != null && r.PersonId == personId)
                     .ToList())
            objectSpace.Delete(row);
    }

    /// <summary>
    /// Prefer in-memory / CreateObject rows. <see cref="IObjectSpace.GetObjectsQuery{T}"/>
    /// often misses unsaved inserts, which caused Join 23505 duplicate-key errors.
    /// </summary>
    private static ApplicationProfileInstancePersonRequestedLocation? Find(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Guid applicationId,
        Guid personId,
        ApplicationProfileInstancePersonRequestedLocationKind kind)
    {
        if (application?.PersonRequestedLocations != null)
        {
            var fromNav = application.PersonRequestedLocations.FirstOrDefault(r =>
                r != null
                && !objectSpace.IsDeletedObject(r)
                && r.PersonId == personId
                && r.Kind == kind);
            if (fromNav != null)
                return fromNav;
        }

        foreach (var row in objectSpace.GetObjects<ApplicationProfileInstancePersonRequestedLocation>())
        {
            if (row == null || objectSpace.IsDeletedObject(row))
                continue;
            if (row.ApplicationProfileInstanceId == applicationId
                && row.PersonId == personId
                && row.Kind == kind)
                return row;
        }

        return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonRequestedLocation>()
            .FirstOrDefault(r => r.ApplicationProfileInstanceId == applicationId
                && r.PersonId == personId
                && r.Kind == kind);
    }

    private static System.Collections.Generic.IEnumerable<ApplicationProfileInstancePersonRequestedLocation> EnumerateForPerson(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId)
    {
        foreach (var row in objectSpace.GetObjects<ApplicationProfileInstancePersonRequestedLocation>())
        {
            if (row == null || objectSpace.IsDeletedObject(row))
                continue;
            if (row.ApplicationProfileInstanceId == applicationId && row.PersonId == personId)
                yield return row;
        }
    }

    private static string? Normalize(string? locations)
    {
        if (string.IsNullOrWhiteSpace(locations) || locations.Trim() == "—")
            return null;
        return locations.Trim();
    }
}