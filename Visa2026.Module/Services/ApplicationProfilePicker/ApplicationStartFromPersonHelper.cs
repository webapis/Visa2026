using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public sealed class ApplicationStartFromPersonValidation
{
    public bool IsBlocked { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FlaggedIncompletePeople { get; init; } = Array.Empty<string>();
}

public static class ApplicationStartFromPersonHelper
{
    public static IReadOnlyList<ApplicationStartPersonCandidate> GetPeopleCandidates(
        IObjectSpace objectSpace,
        Person seedPerson,
        ApplicationProfile profile)
    {
        if (objectSpace == null || seedPerson == null || profile == null || seedPerson.ID == Guid.Empty)
            return Array.Empty<ApplicationStartPersonCandidate>();

        var seed = objectSpace.GetObject(seedPerson);
        var suggestedFamilyIds = GetSuggestedFamilyMemberIds(seed, profile);

        IQueryable<Person> query = objectSpace.GetObjectsQuery<Person>();
        if (profile.ProgressRoute == ApplicationProgressRouteKind.ViaMinistries)
        {
            var contractId = seed.ProjectContract?.ID;
            if (contractId == null || contractId == Guid.Empty)
                return BuildSeedOnly(seed, suggestedFamilyIds);

            query = query.Where(p =>
                p.ProjectContract != null && p.ProjectContract.ID == contractId);
        }

        return query
            .AsEnumerable()
            .OrderByDescending(p => p.ID == seed.ID)
            .ThenByDescending(p => suggestedFamilyIds.Contains(p.ID))
            .ThenBy(p => p.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ApplicationStartPersonCandidate
            {
                PersonId = p.ID,
                FullName = p.FullName ?? string.Empty,
                RoleLabel = p.PersonRole.ToString(),
                PersonalNumber = p.PersonalNumber ?? string.Empty,
                IsSeedPerson = p.ID == seed.ID,
                IsSuggestedFamily = suggestedFamilyIds.Contains(p.ID),
                IsPreSelected = p.ID == seed.ID || suggestedFamilyIds.Contains(p.ID),
            })
            .ToList();
    }

    public static ApplicationStartFromPersonValidation Validate(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        Person seedPerson,
        IReadOnlyList<Person> selectedPeople)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (objectSpace == null || profile == null || seedPerson == null)
        {
            errors.Add("Missing profile or person context.");
            return Blocked(errors, warnings, []);
        }

        if (selectedPeople == null || selectedPeople.Count == 0)
        {
            errors.Add("Select at least one person for this Application.");
            return Blocked(errors, warnings, []);
        }

        if (profile.ProgressRoute == ApplicationProgressRouteKind.ViaMinistries
            && (seedPerson.ProjectContract == null || seedPerson.ProjectContract.ID == Guid.Empty))
        {
            errors.Add(
                "This profile is via ministry — set a Project contract on the seed person before starting an Application.");
            return Blocked(errors, warnings, []);
        }

        foreach (var person in selectedPeople)
        {
            if (!MatchesAudience(profile, person))
            {
                errors.Add(
                    $"{person.FullName} does not match this profile audience (Employee / Family / Visitor).");
            }
        }

        if (profile.ProgressRoute == ApplicationProgressRouteKind.ViaMinistries)
        {
            var contractId = seedPerson.ProjectContract!.ID;
            foreach (var person in selectedPeople)
            {
                if (person.ProjectContract?.ID != contractId)
                {
                    errors.Add(
                        $"{person.FullName} is not on the same Project contract as the seed person.");
                }
            }
        }

        foreach (var person in selectedPeople)
        {
            if (HasOpenApplication(objectSpace, person, profile))
            {
                warnings.Add(
                    $"{person.FullName} already has an open Application on profile {profile.Name}.");
            }
        }

        var flagged = FlagIncompletePeople(profile, selectedPeople);
        if (flagged.Count > 0)
        {
            warnings.Add(
                "Some selected people lack required valid data: " + string.Join(", ", flagged) + ".");
        }

        return new ApplicationStartFromPersonValidation
        {
            IsBlocked = errors.Count > 0,
            Errors = errors,
            Warnings = warnings,
            FlaggedIncompletePeople = flagged,
        };
    }

    public static void LinkPeople(IObjectSpace objectSpace, Application application, IEnumerable<Person> people)
    {
        if (objectSpace == null || application == null || people == null)
            return;

        foreach (var person in people)
            ApplicationPersonService.LinkPerson(objectSpace, application, person);
    }

    public static bool HasOpenApplication(IObjectSpace objectSpace, Person person, ApplicationProfile profile)
    {
        if (objectSpace == null || person == null || profile == null || person.ID == Guid.Empty)
            return false;

        var personId = person.ID;
        var profileId = profile.ID;

        return objectSpace.GetObjectsQuery<ApplicationPerson>()
            .Where(ap =>
                ap.PersonId == personId
                && ap.Application != null
                && ap.Application.ApplicationProfile != null
                && ap.Application.ApplicationProfile.ID == profileId)
            .AsEnumerable()
            .Any(ap => !IsApplicationTerminal(ap.Application));
    }

    private static bool IsApplicationTerminal(Application? application)
    {
        var latest = application?.LatestProgress ?? application?.ProgressHistory?
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();
        return ApplicationProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code);
    }

    private static IReadOnlyList<string> FlagIncompletePeople(
        ApplicationProfile profile,
        IReadOnlyList<Person> selectedPeople)
    {
        var flagged = new List<string>();
        foreach (var person in selectedPeople)
        {
            if (IsPersonIncompleteForProfile(profile, person))
                flagged.Add(person.FullName ?? person.ID.ToString());
        }

        return flagged;
    }

    private static bool IsPersonIncompleteForProfile(ApplicationProfile profile, Person person)
    {
        if (profile.RequirePersonPassport && ApplicationPersonValidItems.ResolvePassport(person) == null)
            return true;
        if (profile.RequirePersonVisa && ApplicationPersonValidItems.ResolveVisa(person) == null)
            return true;
        if (profile.RequirePersonEducation && ApplicationPersonValidItems.ResolveEducation(person) == null)
            return true;
        if (profile.RequirePersonAddressOfResidence && ApplicationPersonValidItems.ResolveAddress(person) == null)
            return true;
        if (profile.RequirePersonPosition && ApplicationPersonValidItems.ResolvePosition(person) == null)
            return true;
        if (profile.RequirePersonSalary && ApplicationPersonValidItems.ResolveSalary(person) == null)
            return true;
        if (profile.RequirePersonMedical && ApplicationPersonValidItems.ResolveMedical(person) == null)
            return true;
        return false;
    }

    private static bool MatchesAudience(ApplicationProfile profile, Person person) =>
        (profile.ForEmployee && person.PersonRole == PersonRecordRole.Employee)
        || (profile.ForFamilyMember && person.PersonRole == PersonRecordRole.FamilyMember)
        || (profile.ForTemporaryVisitor && person.PersonRole == PersonRecordRole.TemporaryVisitor);

    private static HashSet<Guid> GetSuggestedFamilyMemberIds(Person seed, ApplicationProfile profile)
    {
        if (profile.ActionFamily != ApplicationProfileActionFamily.Registration
            || seed.PersonRole != PersonRecordRole.Employee
            || seed.FamilyMembers == null)
        {
            return [];
        }

        return seed.FamilyMembers
            .Where(m => m != null && m.ID != Guid.Empty)
            .Select(m => m.ID)
            .ToHashSet();
    }

    private static IReadOnlyList<ApplicationStartPersonCandidate> BuildSeedOnly(
        Person seed,
        HashSet<Guid> suggestedFamilyIds) =>
    [
        new ApplicationStartPersonCandidate
        {
            PersonId = seed.ID,
            FullName = seed.FullName ?? string.Empty,
            RoleLabel = seed.PersonRole.ToString(),
            PersonalNumber = seed.PersonalNumber ?? string.Empty,
            IsSeedPerson = true,
            IsSuggestedFamily = false,
            IsPreSelected = true,
        },
    ];

    private static ApplicationStartFromPersonValidation Blocked(
        List<string> errors,
        List<string> warnings,
        IReadOnlyList<string> flagged) =>
        new()
        {
            IsBlocked = true,
            Errors = errors,
            Warnings = warnings,
            FlaggedIncompletePeople = flagged,
        };
}

public sealed class ApplicationStartPersonCandidate
{
    public Guid PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string RoleLabel { get; init; } = string.Empty;

    public string PersonalNumber { get; init; } = string.Empty;

    public bool IsSeedPerson { get; init; }

    public bool IsSuggestedFamily { get; init; }

    public bool IsPreSelected { get; init; }
}
