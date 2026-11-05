using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

/// <summary>
/// Copies Case summary border-zone locations onto linked
/// <see cref="Visa.BorderZoneLocation"/> for visa-producing profiles that do not
/// produce a border-zone permit. Keep a real visa location; fill blank/Ýok from Case
/// when Case has a value. Tick overwrites the same linked visa.
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

    public static bool Matches(Visa? visa, string? caseLocations)
    {
        if (visa == null || !IsComplete(caseLocations))
            return false;
        return string.Equals(
            visa.BorderZoneLocation?.Trim(),
            caseLocations!.Trim(),
            StringComparison.Ordinal);
    }

    public static bool OfferTick(string? caseLocations, Visa? visa) =>
        IsComplete(caseLocations) && visa != null && !Matches(visa, caseLocations);

    public static bool OfferTick(ApplicationProfileInstance? application, Visa? visa)
    {
        if (!IsEnabled(application))
            return false;
        return OfferTick(ResolveCaseLocations(application), visa);
    }

    /// <summary>
    /// Join: keep a real <see cref="Visa.BorderZoneLocation"/>; if blank or Ýok,
    /// copy Case when Case has a (non-empty) value.
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
        foreach (var visa in VisasForPerson(objectSpace, application, person))
        {
            if (!ShouldFillFromCase(visa.BorderZoneLocation, caseLocations))
                continue;
            visa.BorderZoneLocation = caseLocations;
            changed = true;
        }

        return changed;
    }

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

    public static bool TryApplyEditedLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid visaId,
        string? locations)
    {
        if (objectSpace == null || application == null || person == null)
            return false;
        if (!ShowInLinkReview(application))
            return false;

        var visa = visaId != Guid.Empty
            ? objectSpace.GetObjectByKey<Visa>(visaId)
            : null;
        visa ??= VisasForPerson(objectSpace, application, person).FirstOrDefault();
        if (visa == null)
            return false;

        var trimmed = string.IsNullOrWhiteSpace(locations) || locations.Trim() == "—"
            ? null
            : locations.Trim();
        visa.BorderZoneLocation = trimmed;
        return true;
    }

    public static bool TryOverwriteFromCase(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid visaId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty || visaId == Guid.Empty)
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

        var linked = VisasForPerson(objectSpace, application, person)
            .FirstOrDefault(v => v.ID == visaId);
        if (linked == null)
        {
            error = "Visa is not linked to this person on the case.";
            return false;
        }

        linked.BorderZoneLocation = caseLocations;
        return true;
    }

    private static System.Collections.Generic.IEnumerable<Visa> VisasForPerson(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        var seen = new System.Collections.Generic.HashSet<Guid>();
        foreach (var visa in LinkedVisas(objectSpace, application, person.ID))
        {
            if (seen.Add(visa.ID))
                yield return visa;
        }

        var fallback = ApplicationProfileInstancePersonValidItems.ResolveVisa(person);
        if (fallback != null && seen.Add(fallback.ID))
            yield return fallback;
    }

    private static System.Collections.Generic.IEnumerable<Visa> LinkedVisas(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid personId)
    {
        var ids = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == personId
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.Visa
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId!.Value)
            .Distinct()
            .ToList();

        foreach (var id in ids)
        {
            var visa = objectSpace.GetObjectByKey<Visa>(id);
            if (visa != null)
                yield return visa;
        }
    }
}