using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Case summary border-zone locations → case↔person
/// <see cref="ApplicationProfileInstancePersonRequestedLocation"/> (Kind BorderZone)
/// for visa-producing profiles that do not produce a border-zone permit.
/// Join / tick never mutate <see cref="Visa.BorderZoneLocation"/>.
/// </summary>
public static class CaseVisaBorderZoneLocationCopy
{
    public static bool IsEnabled(ApplicationProfileInstance? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile == null)
            return false;
        if (!profile.ProduceVisa || profile.ProduceBorderZone)
            return false;
        return ApplicationProfileConfigurationResolver.EffectiveRequireBorderZone(profile);
    }

    public static bool ShowInLinkReview(ApplicationProfileInstance? application) =>
        IsEnabled(application);

    public static bool IsComplete(string? caseLocations) =>
        !string.IsNullOrWhiteSpace(caseLocations);

    /// <summary>
    /// Empty or <c>Ýok</c> is fillable when Case has a non-none zone.
    /// A real visa zone is kept on Join.
    /// </summary>
    public static bool ShouldFillFromCase(string? visaLocations, string? caseLocations)
    {
        if (!IsComplete(caseLocations))
            return false;
        if (string.IsNullOrWhiteSpace(visaLocations))
            return true;
        if (BorderZoneSelectionHelper.IsNoneValue(visaLocations)
            && !BorderZoneSelectionHelper.IsNoneValue(caseLocations))
            return true;
        return false;
    }

    public static string? ResolveCaseLocations(ApplicationProfileInstance? application) =>
        CaseBorderZoneLocationCopy.ResolveCaseLocations(application);

    public static string ResolveDisplaySummary(string? visaLocations, string? caseLocations)
    {
        if (ShouldFillFromCase(visaLocations, caseLocations))
            return caseLocations!.Trim();
        if (!string.IsNullOrWhiteSpace(visaLocations))
            return visaLocations.Trim();
        if (!string.IsNullOrWhiteSpace(caseLocations))
            return caseLocations.Trim();
        return "—";
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
    /// Join: fill empty requested border-zone location from Case (visa-only profiles).
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
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone,
            ResolveCaseLocations(application));
    }

    public static void EnsureForPeopleWithEmptyLocations(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (objectSpace == null || application == null || !IsEnabled(application))
            return;

        foreach (var person in application.People.Where(p => p != null && p.ID != Guid.Empty))
            AcceptOnJoin(objectSpace, application, person);
    }

    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid visaId,
        string? locations)
    {
        _ = visaId;
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

    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid visaId,
        out string? error)
    {
        _ = visaId;
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty)
        {
            error = "Missing visa link.";
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
            error = "Visa border zone location defaulting is not enabled for this profile.";
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