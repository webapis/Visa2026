using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Copies Case summary border-zone locations onto linked
/// <see cref="BorderZoneItem.BorderZoneLocation"/>. Fill empty on Join;
/// tick overwrites the same linked item when Case summary differs.
/// Dual gate: <c>ProduceBorderZone</c> + <c>RequirePersonBorderZoneItem</c> + <c>EffectiveRequireBorderZone</c>.
/// </summary>
public static class CaseBorderZoneLocationCopy
{
    public static bool IsEnabled(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null)
            return false;
        if (!profile.ProduceBorderZone || !profile.RequirePersonBorderZoneItem)
            return false;
        return ApplicationProfileConfigurationResolver.EffectiveRequireBorderZone(profile);
    }

    /// <summary>
    /// Link-review overview row / tiles: profile produces border zone, Case requires
    /// border zone, and the Border zone person tile is configured.
    /// </summary>
    public static bool ShowInLinkReview(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null || !profile.ProduceBorderZone)
            return false;
        if (!ApplicationProfileConfigurationResolver.EffectiveRequireBorderZone(profile))
            return false;
        return ApplicationProfileConfigurationResolver.RequirePersonBorderZoneItem(application);
    }

    /// <summary>
    /// Non-empty including the <c>Ýok</c> sentinel counts as complete (same as Case summary).
    /// </summary>
    public static bool IsComplete(string? caseLocations) =>
        !string.IsNullOrWhiteSpace(caseLocations);

    public static string? ResolveCaseLocations(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;
        if (!string.IsNullOrWhiteSpace(application.BorderZoneLocation))
            return application.BorderZoneLocation.Trim();
        var fallback = application.ApplicationProfile?.DefaultBorderZoneLocation;
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }

    public static bool Matches(BorderZoneItem? item, string? caseLocations)
    {
        if (item == null || !IsComplete(caseLocations))
            return false;
        return string.Equals(
            item.BorderZoneLocation?.Trim(),
            caseLocations!.Trim(),
            StringComparison.Ordinal);
    }

    public static bool OfferTick(string? caseLocations, BorderZoneItem? item) =>
        IsComplete(caseLocations) && item != null && !Matches(item, caseLocations);

    public static bool OfferTick(ApplicationProfileInstance? application, BorderZoneItem? item)
    {
        if (!IsEnabled(application))
            return false;
        return OfferTick(ResolveCaseLocations(application), item);
    }

    /// <summary>
    /// Join / Relink: fill empty locations on linked BorderZoneItem rows for this person.
    /// Does not overwrite a non-empty value (including <c>Ýok</c>).
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
        foreach (var item in LinkedBorderZoneItems(objectSpace, application, person.ID))
        {
            if (!string.IsNullOrWhiteSpace(item.BorderZoneLocation))
                continue;
            item.BorderZoneLocation = caseLocations;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// After Case summary border zone is saved: fill empty linked BorderZoneItem locations
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
    /// Join/Save: write the officer's Border zone location multi-select onto the linked item.
    /// </summary>
    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid borderZoneItemId,
        string? locations)
    {
        if (objectSpace == null || application == null || person == null)
            return false;
        if (!ShowInLinkReview(application))
            return false;

        var item = borderZoneItemId != Guid.Empty
            ? objectSpace.GetObjectByKey<BorderZoneItem>(borderZoneItemId)
            : null;
        item ??= LinkedBorderZoneItems(objectSpace, application, person.ID).FirstOrDefault();
        if (item == null)
            return false;

        var trimmed = string.IsNullOrWhiteSpace(locations) || locations.Trim() == "—"
            ? null
            : locations.Trim();
        item.BorderZoneLocation = trimmed;
        return true;
    }

    /// <summary>
    /// Tick: overwrite BorderZoneLocation on the same linked BorderZoneItem.
    /// </summary>
    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid borderZoneItemId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty || borderZoneItemId == Guid.Empty)
        {
            error = "Missing border zone link.";
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
            error = "Border zone location defaulting is not enabled for this profile.";
            return false;
        }

        var caseLocations = ResolveCaseLocations(application);
        if (!IsComplete(caseLocations))
        {
            error = "Case summary border zone location is empty.";
            return false;
        }

        var linked = LinkedBorderZoneItems(objectSpace, application, personId)
            .FirstOrDefault(item => item.ID == borderZoneItemId);
        if (linked == null)
        {
            error = "Border zone item is not linked to this person on the case.";
            return false;
        }

        linked.BorderZoneLocation = caseLocations;
        return true;
    }

    private static System.Collections.Generic.IEnumerable<BorderZoneItem> LinkedBorderZoneItems(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid personId)
    {
        var ids = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == personId
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.BorderZoneItem
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId!.Value)
            .Distinct()
            .ToList();

        foreach (var id in ids)
        {
            var item = objectSpace.GetObjectByKey<BorderZoneItem>(id);
            if (item != null)
                yield return item;
        }
    }
}