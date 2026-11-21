using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Case summary border-zone locations → case↔person
/// <see cref="ApplicationProfileInstancePersonRequestedLocation"/> (Kind BorderZone).
/// Join / tick never mutate <see cref="BorderZoneItem.BorderZoneLocation"/>.
/// Linked-records rows: <c>ProduceBorderZone</c> + person BZ tile + EffectiveRequire.
/// Case default copy: <see cref="ApplicationProfileConfigurationResolver.ShowCaseRequestedBorderZone"/>.
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

    /// <summary>Case default may copy into empty person requested BZ rows.</summary>
    public static bool CanApplyCaseDefault(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.ShowCaseRequestedBorderZone(application);

    /// <summary>
    /// Link-review row when the profile produces a border-zone permit and Case Results
    /// requires border zone (same as Case). Person Border zone item = document tile only.
    /// </summary>
    public static bool ShowInLinkReview(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null || !profile.ProduceBorderZone)
            return false;
        return ApplicationProfileConfigurationResolver.ShowCaseRequestedBorderZone(application);
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

    public static bool Matches(string? stored, string? caseLocations) =>
        CasePersonRequestedLocation.Matches(stored, caseLocations);

    public static bool OfferTick(string? caseLocations, string? stored) =>
        CasePersonRequestedLocation.OfferTick(caseLocations, stored);

    public static bool OfferTick(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Guid personId)
    {
        if (objectSpace == null || application == null || personId == Guid.Empty || !IsEnabled(application))
            return false;
        var stored = CasePersonRequestedLocation.GetLocations(
            objectSpace,
            application.ID,
            personId,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone);
        return OfferTick(ResolveCaseLocations(application), stored);
    }

    /// <summary>
    /// Join / Relink: copy case BZ into person requested location only when that person row is empty.
    /// Clearing the case value does not wipe person rows.
    /// </summary>
    public static bool AcceptOnJoin(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return false;
        if (!CanApplyCaseDefault(application))
            return false;

        var caseLocations = ResolveCaseLocations(application);
        if (!IsComplete(caseLocations))
            return false;

        var existing = CasePersonRequestedLocation.GetLocations(
            objectSpace,
            application.ID,
            person.ID,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone);
        if (!string.IsNullOrWhiteSpace(existing))
            return false;

        return CasePersonRequestedLocation.Upsert(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone,
            caseLocations);
    }

    public static void EnsureForPeopleWithEmptyLocations(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (objectSpace == null || application == null || !CanApplyCaseDefault(application))
            return;

        foreach (var person in application.People.Where(p => p != null && p.ID != Guid.Empty))
            AcceptOnJoin(objectSpace, application, person);
    }

    /// <summary>
    /// Join/Save: write chip union onto case↔person requested border-zone location.
    /// </summary>
    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid borderZoneItemId,
        string? locations)
    {
        _ = borderZoneItemId;
        if (objectSpace == null || application == null || person == null)
            return false;
        if (!ShowInLinkReview(application))
            return false;

        return CasePersonRequestedLocation.Upsert(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone,
            locations);
    }

    /// <summary>
    /// Tick: overwrite requested border-zone location from Case (does not mutate BorderZoneItem).
    /// </summary>
    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid borderZoneItemId,
        out string? error)
    {
        _ = borderZoneItemId;
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
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

        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
        {
            error = "Person not found.";
            return false;
        }

        CasePersonRequestedLocation.Upsert(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone,
            caseLocations);
        return true;
    }
}