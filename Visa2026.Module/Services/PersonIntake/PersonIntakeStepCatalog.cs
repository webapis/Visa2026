using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public enum PersonIntakeStepKind
{
    Overview,
    Form,
    Record,
    Others,
    Review,
    /// <summary>Read-only list of this employee's family members. A row opens that person's wizard.</summary>
    FamilyMembers,
    /// <summary>Read-only list of this employee's work permit lines. Preview opens the scanned copy.</summary>
    WorkPermits,
    /// <summary>Read-only list of visas on this person's passports. Preview opens the scanned copy.</summary>
    Visas,
    /// <summary>Read-only list of this person's invitation lines. Preview opens the invitation scans.</summary>
    Invitations,
    /// <summary>Read-only list of this person's medical records. Preview opens the scanned copy.</summary>
    MedicalRecords,
    /// <summary>Read-only list of application profile instances this person is already on.</summary>
    ApplicationProfileInstances
}

public sealed class PersonIntakeStep
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Hint { get; init; }
    public PersonIntakeStepKind Kind { get; init; }
    public IReadOnlyList<string> FieldKeys { get; init; } = Array.Empty<string>();
    /// <summary>Fields shown in a column to the right of <see cref="FieldKeys"/> on the same step.</summary>
    public IReadOnlyList<string> CompanionFieldKeys { get; init; } = Array.Empty<string>();
    public string CompanionTitle { get; init; }
    public string RecordTargetKey { get; init; }
    public IReadOnlyList<PersonIntakeOtherRecordOption> OtherOptions { get; init; } = Array.Empty<PersonIntakeOtherRecordOption>();
}

public sealed class PersonIntakeOtherRecordOption
{
    public string TargetKey { get; init; }
    public string Label { get; init; }
    public string Help { get; init; }
}

public static class PersonIntakeStepCatalog
{
    public static IReadOnlyList<PersonIntakeStep> Steps(Person person)
    {
        var employee = person?.PersonRole == PersonRecordRole.Employee;
        // Identity: who the person is.
        var identityFields = new List<string>
        {
            PersonPendingTargets.FirstName,
            PersonPendingTargets.LastName,
            PersonPendingTargets.DateOfBirth,
            PersonPendingTargets.Age,
            PersonPendingTargets.BirthPlace,
            PersonPendingTargets.CountryOfBirth,
            PersonPendingTargets.Gender
        };
        if (employee)
            identityFields.Add(PersonPendingTargets.MaritalStatus);
        identityFields.Add(PersonPendingTargets.Nationality);
        identityFields.Add(PersonPendingTargets.PersonalNumber);
        identityFields.Add(PersonPendingTargets.Photo);

        // Work & address: assignment / contact context (shorter companion step).
        var workFields = new List<string>();
        if (!employee)
        {
            workFields.Add(PersonPendingTargets.SponsoringEmployee);
            workFields.Add(PersonPendingTargets.Relationship);
        }
        else
        {
            workFields.Add(PersonPendingTargets.VisaApplicationFamilyMembersText);
            workFields.Add(PersonPendingTargets.ForeignAddress);
            workFields.Add(PersonPendingTargets.ForeignAddressCountry);
        }

        workFields.Add(PersonPendingTargets.ProjectContract);
        workFields.Add(PersonPendingTargets.Subcontractor);

        var steps = new List<PersonIntakeStep>
        {
            FormStep(
                "identity",
                PersonIntakeText.Get("Step.Identity"),
                PersonIntakeText.Get("Step.Identity.Hint"),
                identityFields.ToArray(),
                workFields.ToArray(),
                PersonIntakeText.Get("Step.Work"))
        };

        steps.Add(RecordStep("passports", PersonIntakeText.Get("Step.Passports"), PersonPendingTargets.Passport));
        steps.Add(RecordStep("education", PersonIntakeText.Get("Step.Education"), PersonPendingTargets.Education));
        // Travel history is entered later from its own navigation, not this wizard.
        // Residence addresses are case-owned (ApplicationProfileInstance Address1/2); not Person intake.
        if (employee)
            steps.Add(RecordStep("documents", PersonIntakeText.Get("Step.Documents"), PersonPendingTargets.PersonDocument));
        if (employee)
            steps.Add(RecordStep(
                "salaries",
                PersonIntakeText.Get("Step.Salaries"),
                PersonPendingTargets.EmployeeSalary,
                PersonIntakeText.Get("Step.Salaries.Hint")));
        if (employee)
            steps.Add(RecordStep("positions", PersonIntakeText.Get("Step.Positions"), PersonPendingTargets.EmployeePositionHistory));
        if (employee)
            steps.Add(RecordStep(
                "work-duties",
                PersonIntakeText.Get("Step.WorkDuties"),
                PersonPendingTargets.WorkDuty,
                PersonIntakeText.Get("Step.WorkDuties.Hint")));
        var others = OthersStep(person);
        if (others.OtherOptions.Count > 0)
            steps.Add(others);
        // Read-only lists come after every step the officer can edit.
        if (employee && HasFamilyMembers(person))
            steps.Add(FamilyMembersStep());
        if (HasVisas(person))
            steps.Add(VisasStep());
        if (HasInvitations(person))
            steps.Add(InvitationsStep());
        if (HasMedicalRecords(person))
            steps.Add(MedicalRecordsStep());
        if (employee && HasWorkPermits(person))
            steps.Add(WorkPermitsStep());
        if (HasApplicationProfileInstances(person))
            steps.Add(ApplicationProfileInstancesStep());
        steps.Add(new PersonIntakeStep
        {
            Id = "review",
            Title = PersonIntakeText.Get("Step.Review"),
            Hint = PersonIntakeText.Get("Step.Review.Hint"),
            Kind = PersonIntakeStepKind.Review
        });
        return steps;
    }

    public const string FamilyMembersStepId = "family-members";
    public const string WorkPermitsStepId = "work-permits";
    public const string VisasStepId = "visas";
    public const string InvitationsStepId = "invitations";
    public const string MedicalRecordsStepId = "medical-records";
    public const string ApplicationProfileInstancesStepId = "application-profile-instances";

    private static bool HasFamilyMembers(Person person) =>
        person.FamilyMembers?.Any(member => member != null) == true;

    private static bool HasWorkPermits(Person person) =>
        person.WorkPermitItems?.Any(item => item != null) == true;

    private static bool HasVisas(Person person) =>
        person.Passports?.Any(passport => passport?.Visas?.Any(visa => visa != null) == true) == true;

    private static bool HasInvitations(Person person) =>
        person.InvitationItems?.Any(item => item != null) == true;

    private static bool HasMedicalRecords(Person person) =>
        person.MedicalRecords?.Any(record => record != null) == true;

    private static bool HasApplicationProfileInstances(Person person) =>
        person.ApplicationProfileInstances?.Any(instance => instance != null) == true;

    private static PersonIntakeStep ApplicationProfileInstancesStep() =>
        new()
        {
            Id = ApplicationProfileInstancesStepId,
            Title = PersonIntakeText.Get("Step.Applications"),
            Hint = PersonIntakeText.Get("Step.Applications.Hint"),
            Kind = PersonIntakeStepKind.ApplicationProfileInstances
        };

    private static PersonIntakeStep MedicalRecordsStep() =>
        new()
        {
            Id = MedicalRecordsStepId,
            Title = PersonIntakeFieldLabels.MemberLabel(typeof(Person), nameof(Person.MedicalRecords), "Medical records"),
            Hint = PersonIntakeText.Get("Step.Medical.Hint"),
            Kind = PersonIntakeStepKind.MedicalRecords
        };

    private static PersonIntakeStep InvitationsStep() =>
        new()
        {
            Id = InvitationsStepId,
            Title = PersonIntakeText.Get("Step.Invitations"),
            Hint = PersonIntakeText.Get("Step.Invitations.Hint"),
            Kind = PersonIntakeStepKind.Invitations
        };

    private static PersonIntakeStep VisasStep() =>
        new()
        {
            Id = VisasStepId,
            Title = PersonIntakeText.Get("Step.Visas"),
            Hint = PersonIntakeText.Get("Step.Visas.Hint"),
            Kind = PersonIntakeStepKind.Visas
        };

    private static PersonIntakeStep WorkPermitsStep() =>
        new()
        {
            Id = WorkPermitsStepId,
            Title = PersonIntakeFieldLabels.MemberLabel(typeof(Person), nameof(Person.WorkPermitItems), "Work permits"),
            Hint = PersonIntakeText.Get("Step.WorkPermits.Hint"),
            Kind = PersonIntakeStepKind.WorkPermits
        };

    private static PersonIntakeStep FamilyMembersStep() =>
        new()
        {
            Id = FamilyMembersStepId,
            Title = PersonIntakeFieldLabels.MemberLabel(typeof(Person), nameof(Person.FamilyMembers), "Family members"),
            Hint = PersonIntakeText.Get("Step.FamilyMembers.Hint"),
            Kind = PersonIntakeStepKind.FamilyMembers
        };

    private static PersonIntakeStep FormStep(
        string id,
        string title,
        string hint,
        string[] keys,
        string[] companionKeys = null,
        string companionTitle = null) =>
        new()
        {
            Id = id,
            Title = title,
            Hint = hint,
            Kind = PersonIntakeStepKind.Form,
            FieldKeys = keys,
            CompanionFieldKeys = companionKeys ?? Array.Empty<string>(),
            CompanionTitle = companionTitle
        };

    private static PersonIntakeStep RecordStep(string id, string title, string recordKey, string hint = null) =>
        new()
        {
            Id = id,
            Title = title,
            Hint = hint ?? PersonIntakeText.Get("Step.Record.Hint"),
            Kind = PersonIntakeStepKind.Record,
            RecordTargetKey = recordKey
        };

    private static PersonIntakeStep OthersStep(Person person)
    {
        var employee = person.PersonRole == PersonRecordRole.Employee;
        // Purpose of arrival is the Work duty record step. Employee Other records stay off.
        var options = employee
            ? Array.Empty<PersonIntakeOtherRecordOption>()
            : new[]
            {
                Other(PersonPendingTargets.PersonFamilyRelationDocument, PersonIntakeText.Get("Other.FamilyRelation"), PersonIntakeText.Get("Other.FamilyRelation.Help"))
            };
        return new PersonIntakeStep
        {
            Id = "others",
            Title = PersonIntakeText.Get("Step.Others"),
            Hint = PersonIntakeText.Get("Step.Others.Hint"),
            Kind = PersonIntakeStepKind.Others,
            OtherOptions = options
        };
    }

    private static PersonIntakeOtherRecordOption Other(string key, string label, string help) =>
        new() { TargetKey = key, Label = label, Help = help };
}