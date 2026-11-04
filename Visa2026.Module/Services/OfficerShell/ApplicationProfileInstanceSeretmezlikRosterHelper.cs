using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Seretmezlik standalone profile: roster people on the Seretmezlik instance are excluded
/// on the linked target Application Profile Instance as soon as they are saved.
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

        if (person.ID == Guid.Empty)
        {
            error = "Person is required.";
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
            // Reload target people via query in case navigation is stale.
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

        var excluded = ApplicationProfileInstanceExclusionQueries.GetExcludedPersonIds(target, objectSpace);
        if (excluded.Contains(person.ID))
        {
            error = "This person is already excluded on the target application.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// People still active on the target roster (not excluded by any Seretmezlik / legacy letter).
    /// Empty when target is unset — link search shows no candidates.
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

        var target = seretmezlik.TargetApplicationProfileInstance
            ?? objectSpace.GetObjectByKey<ApplicationProfileInstance>(targetId);
        if (target == null)
            return Array.Empty<Guid>();

        var excluded = ApplicationProfileInstanceExclusionQueries.GetExcludedPersonIds(target, objectSpace);
        var rosterIds = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ID == target.ID)
            .SelectMany(a => a.People)
            .Select(p => p.ID)
            .Distinct()
            .AsEnumerable()
            .Where(id => id != Guid.Empty && !excluded.Contains(id))
            .ToHashSet();

        return rosterIds;
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
}
