using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Copies Case summary work-permit locations onto linked
/// <see cref="WorkPermitItem.WorkPermittedLocations"/>. Fill empty on Join;
/// tick overwrites the same linked item when Case summary differs.
/// </summary>
public static class CaseWorkPermitLocationCopy
{
    public static bool IsEnabled(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null)
            return false;
        if (!profile.ProduceWorkPermit || !profile.RequirePersonWorkPermitItem)
            return false;
        return ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
            profile.ProduceWorkPermit,
            profile.RequireWorkPermitLocation);
    }

    /// <summary>
    /// Link-review overview row: visible when the case produces a work permit and the
    /// Work permit person tile is configured (profile or type dual-read).
    /// </summary>
    public static bool ShowInLinkReview(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null || !profile.ProduceWorkPermit)
            return false;
        if (!ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
                profile.ProduceWorkPermit,
                profile.RequireWorkPermitLocation))
            return false;
        return ApplicationProfileConfigurationResolver.ShowCurrentWorkPermitItem(application);
    }

    public static bool IsComplete(string? caseLocations) =>
        !string.IsNullOrWhiteSpace(caseLocations);

    public static string? ResolveCaseLocations(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;
        if (!string.IsNullOrWhiteSpace(application.MovementPermitLocation))
            return application.MovementPermitLocation.Trim();
        var fallback = application.ApplicationProfile?.DefaultWorkPermitLocation;
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }

    public static bool Matches(WorkPermitItem? item, string? caseLocations)
    {
        if (item == null || !IsComplete(caseLocations))
            return false;
        return string.Equals(
            item.WorkPermittedLocations?.Trim(),
            caseLocations!.Trim(),
            StringComparison.Ordinal);
    }

    public static bool OfferTick(string? caseLocations, WorkPermitItem? item) =>
        IsComplete(caseLocations) && item != null && !Matches(item, caseLocations);

    public static bool OfferTick(ApplicationProfileInstance? application, WorkPermitItem? item)
    {
        if (!IsEnabled(application))
            return false;
        return OfferTick(ResolveCaseLocations(application), item);
    }

    /// <summary>
    /// Join / Relink: fill empty locations on linked WorkPermitItem rows for this person.
    /// Does not overwrite a non-empty value.
    /// </summary>
    public static bool AcceptOnJoin(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return false;
        if (!IsEnabled(application))
            return false;

        var caseLocations = ResolveCaseLocations(application);
        if (!IsComplete(caseLocations))
            return false;

        var changed = false;
        foreach (var item in LinkedWorkPermitItems(objectSpace, application, person.ID))
        {
            if (!string.IsNullOrWhiteSpace(item.WorkPermittedLocations))
                continue;
            item.WorkPermittedLocations = caseLocations;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// After Case summary location is saved: fill empty linked WorkPermitItem locations
    /// for every person on the case.
    /// </summary>
    public static void EnsureForPeopleWithEmptyLocations(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (objectSpace == null || application == null || !IsEnabled(application))
            return;

        var caseLocations = ResolveCaseLocations(application);
        if (!IsComplete(caseLocations))
            return;

        foreach (var person in application.People.Where(p => p != null && p.ID != Guid.Empty))
            AcceptOnJoin(objectSpace, application, person);
    }

    /// <summary>
    /// Tick: overwrite WorkPermittedLocations on the same linked WorkPermitItem.
    /// </summary>
    /// <summary>
    /// Join/Save: write the officer's Work permit location multi-select onto the linked item.
    /// </summary>
    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid workPermitItemId,
        string? locations)
    {
        if (objectSpace == null || application == null || person == null)
            return false;
        if (!ShowInLinkReview(application))
            return false;

        var item = workPermitItemId != Guid.Empty
            ? objectSpace.GetObjectByKey<WorkPermitItem>(workPermitItemId)
            : null;
        item ??= LinkedWorkPermitItems(objectSpace, application, person.ID).FirstOrDefault();
        if (item == null)
            return false;

        var trimmed = string.IsNullOrWhiteSpace(locations) || locations.Trim() == "—"
            ? null
            : locations.Trim();
        item.WorkPermittedLocations = trimmed;
        return true;
    }
    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid workPermitItemId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty || workPermitItemId == Guid.Empty)
        {
            error = "Missing work permit link.";
            return false;
        }

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (application == null)
        {
            error = "Application not found.";
            return false;
        }

        ApplicationProfileInstancePersonResolver.EnsureApplicationProfileLoaded(objectSpace, application);
        if (!IsEnabled(application))
        {
            error = "Work permit location defaulting is not enabled for this profile.";
            return false;
        }

        var caseLocations = ResolveCaseLocations(application);
        if (!IsComplete(caseLocations))
        {
            error = "Case summary work permit location is empty.";
            return false;
        }

        var linked = LinkedWorkPermitItems(objectSpace, application, personId)
            .FirstOrDefault(item => item.ID == workPermitItemId);
        if (linked == null)
        {
            error = "Work permit item is not linked to this person on the case.";
            return false;
        }

        linked.WorkPermittedLocations = caseLocations;
        return true;
    }

    private static System.Collections.Generic.IEnumerable<WorkPermitItem> LinkedWorkPermitItems(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid personId)
    {
        var ids = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == personId
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId!.Value)
            .Distinct()
            .ToList();

        foreach (var id in ids)
        {
            var item = objectSpace.GetObjectByKey<WorkPermitItem>(id);
            if (item != null)
                yield return item;
        }
    }
}