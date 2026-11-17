using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public enum PersonIntakeItemState
{
    Filled,
    Pending,
    Unresolved
}

public sealed class PersonIntakeFinding
{
    public string TargetKey { get; init; }
    public string AreaKey { get; init; }
    public PersonIntakeItemState State { get; init; }
    public bool CanDefer { get; init; }
}

/// <summary>
/// Decides which person fields and child records are filled, explicitly pending, or still unresolved.
/// </summary>
public static class PersonIntakeCompleteness
{
    private enum Who
    {
        Both,
        Employee,
        Family
    }

    private sealed record Requirement(string TargetKey, string AreaKey, Who Who, bool BlocksWhenEmpty, bool CanDefer);

    private static readonly Requirement[] Requirements =
    [
        new(PersonPendingTargets.FirstName, PersonIncompleteDataLabels.PersonalData, Who.Both, true, false),
        new(PersonPendingTargets.LastName, PersonIncompleteDataLabels.PersonalData, Who.Both, true, false),
        new(PersonPendingTargets.DateOfBirth, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.Gender, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.PersonalNumber, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.BirthPlace, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.CountryOfBirth, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.Nationality, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.MaritalStatus, PersonIncompleteDataLabels.PersonalData, Who.Employee, true, true),
        new(PersonPendingTargets.ForeignAddress, PersonIncompleteDataLabels.PersonalData, Who.Employee, true, true),
        new(PersonPendingTargets.ForeignAddressCountry, PersonIncompleteDataLabels.PersonalData, Who.Employee, true, true),
        new(PersonPendingTargets.ProjectContract, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.Subcontractor, PersonIncompleteDataLabels.PersonalData, Who.Both, true, true),
        new(PersonPendingTargets.SponsoringEmployee, PersonIncompleteDataLabels.PersonalData, Who.Family, true, false),
        new(PersonPendingTargets.Relationship, PersonIncompleteDataLabels.PersonalData, Who.Family, true, false),
        new(PersonPendingTargets.Photo, PersonIncompleteDataLabels.Photo, Who.Both, false, true),
        new(PersonPendingTargets.Passport, PersonIncompleteDataLabels.Passport, Who.Both, true, true),
        new(PersonPendingTargets.Education, PersonIncompleteDataLabels.Education, Who.Both, true, true),
        new(PersonPendingTargets.MedicalRecord, PersonIncompleteDataLabels.Medical, Who.Both, true, true),
        new(PersonPendingTargets.AddressOfResidence, PersonIncompleteDataLabels.Address, Who.Both, true, true),
        new(PersonPendingTargets.PersonDocument, PersonIncompleteDataLabels.Cv, Who.Employee, false, true),
        new(PersonPendingTargets.EmployeePositionHistory, PersonIncompleteDataLabels.Other, Who.Employee, false, true),
        new(PersonPendingTargets.TravelHistory, PersonIncompleteDataLabels.Other, Who.Both, false, true),
        new(PersonPendingTargets.WorkDuty, PersonIncompleteDataLabels.Other, Who.Employee, false, true),
        new(PersonPendingTargets.EmployeeSalary, PersonIncompleteDataLabels.Other, Who.Employee, false, true),
        new(PersonPendingTargets.PersonFamilyRelationDocument, PersonIncompleteDataLabels.FamilyDocs, Who.Family, false, true),
    ];

    public static bool HasPending(Person person, string targetKey) =>
        person?.PendingItems != null
        && person.PendingItems.Any(item =>
            item != null && string.Equals(item.TargetKey, targetKey, StringComparison.Ordinal));

    public static IReadOnlyList<PersonIntakeFinding> Evaluate(Person person) =>
        Evaluate(person, draftPendingKeys: null);

    public static IReadOnlyList<PersonIntakeFinding> Evaluate(
        Person person,
        IEnumerable<string> draftPendingKeys)
    {
        HashSet<string> draft = null;
        if (draftPendingKeys != null)
        {
            draft = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in draftPendingKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    draft.Add(key);
            }
        }

        return Evaluate(person, draft);
    }

    private static IReadOnlyList<PersonIntakeFinding> Evaluate(Person person, HashSet<string> draftPendingKeys)
    {
        if (person == null)
            return Array.Empty<PersonIntakeFinding>();

        var role = person.PersonRole;
        var findings = new List<PersonIntakeFinding>(Requirements.Length);
        foreach (var requirement in Requirements)
        {
            if (!Applies(requirement.Who, role))
                continue;

            PersonIntakeItemState state;
            if (IsPending(person, requirement.TargetKey, draftPendingKeys))
                state = PersonIntakeItemState.Pending;
            else if (IsFilled(person, requirement.TargetKey))
                state = PersonIntakeItemState.Filled;
            else if (requirement.BlocksWhenEmpty)
                state = PersonIntakeItemState.Unresolved;
            else
                state = PersonIntakeItemState.Filled;

            findings.Add(new PersonIntakeFinding
            {
                TargetKey = requirement.TargetKey,
                AreaKey = requirement.AreaKey,
                State = state,
                CanDefer = requirement.CanDefer
            });
        }

        return findings;
    }

    private static bool IsPending(Person person, string targetKey, HashSet<string> draftPendingKeys) =>
        draftPendingKeys != null && draftPendingKeys.Contains(targetKey)
        || HasPending(person, targetKey);

    public static bool IsLegacyOnly(Person person)
    {
        if (person == null || !person.IsDataIncomplete)
            return false;

        return person.PendingItems == null
            || !person.PendingItems.Any(item => item != null && !string.IsNullOrWhiteSpace(item.TargetKey));
    }

    /// <summary>
    /// Area labels already checked on a person who was marked incomplete before pending rows existed.
    /// Does not write.
    /// </summary>
    public static IReadOnlyList<string> LegacyAreaKeys(Person person)
    {
        if (!IsLegacyOnly(person))
            return Array.Empty<string>();

        var areas = new List<string>(9);
        if (person.IncompleteMissingPersonalData) areas.Add(PersonIncompleteDataLabels.PersonalData);
        if (person.IncompleteMissingPassport) areas.Add(PersonIncompleteDataLabels.Passport);
        if (person.IncompleteMissingCv) areas.Add(PersonIncompleteDataLabels.Cv);
        if (person.IncompleteMissingPhoto) areas.Add(PersonIncompleteDataLabels.Photo);
        if (person.IncompleteMissingEducation) areas.Add(PersonIncompleteDataLabels.Education);
        if (person.IncompleteMissingMedical) areas.Add(PersonIncompleteDataLabels.Medical);
        if (person.IncompleteMissingAddress) areas.Add(PersonIncompleteDataLabels.Address);
        if (person.IncompleteMissingFamilyDocs) areas.Add(PersonIncompleteDataLabels.FamilyDocs);
        if (person.IncompleteMissingOther) areas.Add(PersonIncompleteDataLabels.Other);
        return areas;
    }

    /// <summary>Writes one legacy pending row per checked area. No-op unless <see cref="IsLegacyOnly"/>.</summary>
    public static void MaterializeLegacy(Person person)
    {
        if (!IsLegacyOnly(person))
            return;

        person.PendingItems ??= new System.Collections.ObjectModel.ObservableCollection<PersonPendingItem>();
        foreach (var area in LegacyAreaKeys(person))
        {
            person.PendingItems.Add(new PersonPendingItem
            {
                Person = person,
                TargetKey = PersonPendingTargets.LegacyPrefix + area,
                AreaKey = area,
                Note = person.IncompleteNotes
            });
        }
    }

    /// <summary>
    /// A pending scalar is stored empty. Personal number stays null rather than the sentinel "0".
    /// Child records are not created or deleted here.
    /// </summary>
    public static void ClearPendingScalars(Person person)
    {
        if (person == null)
            return;

        if (HasPending(person, PersonPendingTargets.DateOfBirth))
            person.DateOfBirth = default;
        if (HasPending(person, PersonPendingTargets.Gender))
            person.Gender = null;
        if (HasPending(person, PersonPendingTargets.PersonalNumber))
            person.PersonalNumber = null;
        if (HasPending(person, PersonPendingTargets.BirthPlace))
            person.BirthPlace = null;
        if (HasPending(person, PersonPendingTargets.CountryOfBirth))
            person.CountryOfBirth = null;
        if (HasPending(person, PersonPendingTargets.Nationality))
            person.Nationality = null;
        if (HasPending(person, PersonPendingTargets.MaritalStatus))
            person.MaritalStatus = null;
        if (HasPending(person, PersonPendingTargets.ForeignAddress))
            person.ForeignAddress = null;
        if (HasPending(person, PersonPendingTargets.ForeignAddressCountry))
            person.ForeignAddressCountry = null;
        if (HasPending(person, PersonPendingTargets.ProjectContract) && person.PersonRole != PersonRecordRole.FamilyMember)
            person.ProjectContract = null;
        if (HasPending(person, PersonPendingTargets.Subcontractor))
            person.Subcontractor = null;
        if (HasPending(person, PersonPendingTargets.Photo))
            person.Photo = null;
    }

    private static bool Applies(Who who, PersonRecordRole role) => who switch
    {
        Who.Employee => role == PersonRecordRole.Employee,
        Who.Family => role == PersonRecordRole.FamilyMember,
        _ => role == PersonRecordRole.Employee || role == PersonRecordRole.FamilyMember
    };

    private static bool IsFilled(Person person, string targetKey) => targetKey switch
    {
        PersonPendingTargets.FirstName => HasText(person.FirstName),
        PersonPendingTargets.LastName => HasText(person.LastName),
        PersonPendingTargets.DateOfBirth => person.DateOfBirth != default,
        PersonPendingTargets.Gender => person.Gender != null,
        PersonPendingTargets.PersonalNumber => HasText(person.PersonalNumber),
        PersonPendingTargets.BirthPlace => HasText(person.BirthPlace),
        PersonPendingTargets.CountryOfBirth => person.CountryOfBirth != null,
        PersonPendingTargets.Nationality => person.Nationality != null,
        PersonPendingTargets.MaritalStatus => person.MaritalStatus != null,
        PersonPendingTargets.ForeignAddress => HasText(person.ForeignAddress),
        PersonPendingTargets.ForeignAddressCountry => person.ForeignAddressCountry != null,
        PersonPendingTargets.ProjectContract => person.ProjectContract != null
            || (person.PersonRole == PersonRecordRole.FamilyMember && person.SponsoringEmployee?.ProjectContract != null),
        PersonPendingTargets.Subcontractor => person.Subcontractor != null,
        PersonPendingTargets.SponsoringEmployee => person.SponsoringEmployee != null,
        PersonPendingTargets.Relationship => person.Relationship != null,
        PersonPendingTargets.Photo => person.Photo is { Length: > 0 },
        PersonPendingTargets.Passport => HasAny(person.Passports),
        PersonPendingTargets.Education => HasAny(person.Educations),
        PersonPendingTargets.MedicalRecord => HasAny(person.MedicalRecords),
        PersonPendingTargets.AddressOfResidence => HasAny(person.AddressesOfResidence),
        PersonPendingTargets.PersonDocument => HasAny(person.Documents),
        PersonPendingTargets.EmployeePositionHistory => HasAny(person.PositionHistory),
        PersonPendingTargets.TravelHistory => HasAny(person.TravelHistories),
        PersonPendingTargets.WorkDuty => HasAny(person.WorkDuties),
        PersonPendingTargets.EmployeeSalary => HasAny(person.Salaries),
        PersonPendingTargets.PersonFamilyRelationDocument => HasAny(person.FamilyRelationDocuments),
        _ => false
    };

    private static bool HasText(string value) => !string.IsNullOrWhiteSpace(value);

    private static bool HasAny<T>(IList<T> items) => items != null && items.Any(item => item != null);
}
