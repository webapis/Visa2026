using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Seretmezlik standalone profile: roster people on the Seretmezlik instance are excluded
/// on the linked target Application Profile Instance as soon as they are saved.
/// The same target person may join more than one Seretmezlik when Submit-to destinations differ.
/// </summary>
public static class ApplicationProfileInstanceSeretmezlikRosterHelper
{
    public static bool IsSeretmezlik(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.IsSeretmezlikInstance(application);

    public static bool IsTargetLocked(ApplicationProfileInstance? seretmezlik) =>
        IsSeretmezlik(seretmezlik)
        && seretmezlik!.TargetApplicationProfileInstanceId.HasValue
        && seretmezlik.TargetApplicationProfileInstanceId.Value != Guid.Empty
        && seretmezlik.People?.Any(p => p != null && p.ID != Guid.Empty) == true;

    public static string GetDestinationKey(ApplicationProfileInstance? application) =>
        ApplicationProfileSubmitToOrganizationHelper.EncodeValue(application);

    public static bool HasDestination(ApplicationProfileInstance? application) =>
        !string.IsNullOrWhiteSpace(GetDestinationKey(application));

    /// <summary>
    /// Via-ministry Seretmezlik shows Submit-to; require it before Link.
    /// Direct-migration Seretmezlik has no Submit-to field — do not block the picker.
    /// </summary>
    public static bool RequiresSubmitToBeforeLink(ApplicationProfileInstance? application) =>
        IsSeretmezlik(application)
        && ApplicationProfileConfigurationResolver.ShowSubmitToOrganization(application);

    public static bool CanUnlinkPerson(ApplicationProfileInstance? application, Person? person)
    {
        if (!IsSeretmezlik(application) || person == null || person.ID == Guid.Empty)
            return true;

        // People already on a Seretmezlik roster cannot be removed (irreversible exclusion).
        return application!.People?.Any(p => p != null && p.ID == person.ID) != true;
    }

    public static bool TryValidateLinkPerson(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        out string? error)
    {
        error = null;
        if (!IsSeretmezlik(application))
            return true;

        var targetId = application.TargetApplicationProfileInstanceId
            ?? application.TargetApplicationProfileInstance?.ID
            ?? Guid.Empty;
        if (targetId == Guid.Empty)
        {
            error = "Set the target application before linking people to Seretmezlik.";
            return false;
        }

        if (RequiresSubmitToBeforeLink(application) && !HasDestination(application))
        {
            error = "Set Submit to organization before linking people to Seretmezlik.";
            return false;
        }

        if (person.ID == Guid.Empty)
        {
            error = "Person is required.";
            return false;
        }

        if (objectSpace == null)
        {
            error = "Application is not ready.";
            return false;
        }

        var target = application.TargetApplicationProfileInstance
            ?? objectSpace.GetObjectByKey<ApplicationProfileInstance>(targetId);
        if (target == null)
        {
            error = "Target application was not found.";
            return false;
        }

        var onTarget = target.People?.Any(p => p != null && p.ID == person.ID) == true;
        if (!onTarget)
        {
            onTarget = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Where(a => a.ID == target.ID)
                .SelectMany(a => a.People)
                .Any(p => p.ID == person.ID);
        }

        if (!onTarget)
        {
            error = "Only people on the target application roster can be linked to Seretmezlik.";
            return false;
        }

        var conflict = FindSameDestinationSeretmezlikForPerson(
            objectSpace,
            targetId,
            application.ID,
            GetDestinationKey(application),
            person.ID);
        if (conflict != null)
        {
            var number = ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(conflict);
            error = $"This person is already on Seretmezlik {number} for the same Submit to organization.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Target-roster people who may still be linked to this Seretmezlik.
    /// Empty when target is unset, or when via-ministry Submit-to is still unset.
    /// Direct-migration Seretmezlik (no Submit-to field) lists the full target roster.
    /// People already excluded only by a different-destination Seretmezlik remain linkable.
    /// </summary>
    public static IReadOnlyCollection<Guid> GetLinkableTargetPersonIds(
        IObjectSpace objectSpace,
        ApplicationProfileInstance seretmezlik)
    {
        if (objectSpace == null || !IsSeretmezlik(seretmezlik))
            return Array.Empty<Guid>();

        var targetId = seretmezlik.TargetApplicationProfileInstanceId
            ?? seretmezlik.TargetApplicationProfileInstance?.ID
            ?? Guid.Empty;
        if (targetId == Guid.Empty)
            return Array.Empty<Guid>();

        if (RequiresSubmitToBeforeLink(seretmezlik) && !HasDestination(seretmezlik))
            return Array.Empty<Guid>();

        var destinationKey = GetDestinationKey(seretmezlik);
        var blocked = GetPersonIdsOnSameDestinationSeretmezliks(
            objectSpace,
            targetId,
            seretmezlik.ID,
            destinationKey);

        var rosterIds = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ID == targetId)
            .SelectMany(a => a.People)
            .Select(p => p.ID)
            .Distinct()
            .AsEnumerable()
            .Where(id => id != Guid.Empty && !blocked.Contains(id))
            .ToHashSet();

        return rosterIds;
    }

    /// <summary>
    /// Other Seretmezlik case numbers (same target, any destination) that already include these people.
    /// Used for the Link existing picker note.
    /// </summary>
    public static IReadOnlyDictionary<Guid, string> GetOtherSeretmezlikNotesForTargetPeople(
        IObjectSpace objectSpace,
        ApplicationProfileInstance seretmezlik)
    {
        var result = new Dictionary<Guid, string>();
        if (objectSpace == null || !IsSeretmezlik(seretmezlik))
            return result;

        var targetId = seretmezlik.TargetApplicationProfileInstanceId
            ?? seretmezlik.TargetApplicationProfileInstance?.ID
            ?? Guid.Empty;
        if (targetId == Guid.Empty)
            return result;

        var others = LoadOtherSeretmezlikCases(objectSpace, targetId, seretmezlik.ID);
        foreach (var other in others.OrderBy(o => o.ApplicationDate))
        {
            var number = ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(other);
            var people = other.People
                ?? objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.ID == other.ID)
                    .SelectMany(a => a.People)
                    .ToList();
            foreach (var person in people)
            {
                if (person == null || person.ID == Guid.Empty)
                    continue;
                if (result.TryGetValue(person.ID, out var existing))
                {
                    if (!existing.Contains(number, StringComparison.OrdinalIgnoreCase))
                        result[person.ID] = existing + ", " + number;
                }
                else
                {
                    result[person.ID] = "Already on Seretmezlik " + number;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Blocks clearing Submit-to while people are linked, and blocks changing Submit-to to a
    /// destination that already has any of this roster on another Seretmezlik for the same target.
    /// </summary>
    public static bool TryValidateSubmitToChange(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string? newDestinationValue,
        out string? error)
    {
        error = null;
        if (!IsSeretmezlik(application))
            return true;

        var hasPeople = application.People?.Any(p => p != null && p.ID != Guid.Empty) == true;
        var newKey = (newDestinationValue ?? string.Empty).Trim();
        if (hasPeople && string.IsNullOrWhiteSpace(newKey))
        {
            error = "Submit to organization cannot be cleared while people are linked to this Seretmezlik.";
            return false;
        }

        if (!hasPeople || string.IsNullOrWhiteSpace(newKey))
            return true;

        var targetId = application.TargetApplicationProfileInstanceId
            ?? application.TargetApplicationProfileInstance?.ID
            ?? Guid.Empty;
        if (targetId == Guid.Empty || objectSpace == null)
            return true;

        foreach (var person in application.People!)
        {
            if (person == null || person.ID == Guid.Empty)
                continue;

            var conflict = FindSameDestinationSeretmezlikForPerson(
                objectSpace,
                targetId,
                application.ID,
                newKey,
                person.ID);
            if (conflict == null)
                continue;

            var number = ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(conflict);
            var name = string.IsNullOrWhiteSpace(person.FullName) ? "A person" : person.FullName.Trim();
            error = $"{name} is already on Seretmezlik {number} for that Submit to organization.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// After a successful Seretmezlik LinkPerson commit, true when the target has no remaining active people.
    /// </summary>
    public static bool IsTargetFullyExcluded(IObjectSpace objectSpace, ApplicationProfileInstance seretmezlik)
    {
        if (!IsSeretmezlik(seretmezlik))
            return false;

        var targetId = seretmezlik.TargetApplicationProfileInstanceId
            ?? seretmezlik.TargetApplicationProfileInstance?.ID
            ?? Guid.Empty;
        if (targetId == Guid.Empty)
            return false;

        var target = seretmezlik.TargetApplicationProfileInstance
            ?? objectSpace.GetObjectByKey<ApplicationProfileInstance>(targetId);
        if (target == null)
            return false;

        return ApplicationProfileInstanceExclusionQueries.CountRemainingActiveOnTarget(objectSpace, target) == 0;
    }

    private static HashSet<Guid> GetPersonIdsOnSameDestinationSeretmezliks(
        IObjectSpace objectSpace,
        Guid targetId,
        Guid currentSeretmezlikId,
        string destinationKey)
    {
        var blocked = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(destinationKey))
            return blocked;

        foreach (var other in LoadOtherSeretmezlikCases(objectSpace, targetId, currentSeretmezlikId))
        {
            if (!string.Equals(GetDestinationKey(other), destinationKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var people = other.People
                ?? objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.ID == other.ID)
                    .SelectMany(a => a.People)
                    .ToList();
            foreach (var person in people)
            {
                if (person != null && person.ID != Guid.Empty)
                    blocked.Add(person.ID);
            }
        }

        return blocked;
    }

    private static ApplicationProfileInstance? FindSameDestinationSeretmezlikForPerson(
        IObjectSpace objectSpace,
        Guid targetId,
        Guid currentSeretmezlikId,
        string destinationKey,
        Guid personId)
    {
        if (personId == Guid.Empty || string.IsNullOrWhiteSpace(destinationKey))
            return null;

        foreach (var other in LoadOtherSeretmezlikCases(objectSpace, targetId, currentSeretmezlikId))
        {
            if (!string.Equals(GetDestinationKey(other), destinationKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var onOther = other.People?.Any(p => p != null && p.ID == personId) == true;
            if (!onOther)
            {
                onOther = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.ID == other.ID)
                    .SelectMany(a => a.People)
                    .Any(p => p.ID == personId);
            }

            if (onOther)
                return other;
        }

        return null;
    }

    private static List<ApplicationProfileInstance> LoadOtherSeretmezlikCases(
        IObjectSpace objectSpace,
        Guid targetId,
        Guid currentSeretmezlikId)
    {
        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(i => i.TargetApplicationProfileInstanceId == targetId
                && i.ID != currentSeretmezlikId)
            .OrderBy(i => i.ApplicationDate)
            .ToList();
    }
}