using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Case summary work-permit locations → case↔person
/// <see cref="ApplicationProfileInstancePersonRequestedLocation"/> (Kind WorkPermit).
/// Join / tick never mutate <see cref="WorkPermitItem.WorkPermittedLocations"/>.
/// </summary>
public static class CaseWorkPermitLocationCopy
{
    public static bool IsEnabled(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null)
            return false;
        if (!profile.RequirePersonWorkPermitItem)
            return false;
        if (profile.ProduceWorkLocation && profile.RequireWorkPermitLocation)
            return true;
        if (!profile.ProduceWorkPermit)
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
        if (profile == null)
            return false;
        var locationConfigured = profile.ProduceWorkLocation && profile.RequireWorkPermitLocation
            || ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
                profile.ProduceWorkPermit,
                profile.RequireWorkPermitLocation);
        if (!locationConfigured)
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
            ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit);
        return OfferTick(ResolveCaseLocations(application), stored);
    }

    /// <summary>
    /// Join / Relink: fill empty requested WP location from Case summary.
    /// </summary>
    public static bool AcceptOnJoin(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return false;
        if (!IsEnabled(application))
            return false;

        return CasePersonRequestedLocation.UpsertIfEmpty(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit,
            ResolveCaseLocations(application));
    }

    /// <summary>
    /// After Case summary location is saved: fill empty requested WP locations for every person.
    /// </summary>
    public static void EnsureForPeopleWithEmptyLocations(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (objectSpace == null || application == null || !IsEnabled(application))
            return;

        foreach (var person in application.People.Where(p => p != null && p.ID != Guid.Empty))
            AcceptOnJoin(objectSpace, application, person);
    }

    /// <summary>
    /// Join/Save: write the officer's chip union onto the case↔person requested WP location.
    /// </summary>
    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid workPermitItemId,
        string? locations)
    {
        _ = workPermitItemId;
        if (objectSpace == null || application == null || person == null)
            return false;
        if (!ShowInLinkReview(application))
            return false;

        return CasePersonRequestedLocation.Upsert(
            objectSpace,
            application,
            person,
            ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit,
            locations);
    }

    /// <summary>
    /// Tick: overwrite requested WP location from Case summary (does not mutate WorkPermitItem).
    /// </summary>
    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid workPermitItemId,
        out string? error)
    {
        _ = workPermitItemId;
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
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
            ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit,
            caseLocations);
        return true;
    }
}