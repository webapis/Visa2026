using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public enum PersonIntakeStepKind
{
    Overview,
    Form,
    Record,
    Others,
    Review
}

public sealed class PersonIntakeStep
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Hint { get; init; }
    public PersonIntakeStepKind Kind { get; init; }
    public IReadOnlyList<string> FieldKeys { get; init; } = Array.Empty<string>();
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
            FormStep("identity", PersonIntakeText.Get("Step.Identity"), PersonIntakeText.Get("Step.Identity.Hint"), identityFields.ToArray()),
            FormStep("work", PersonIntakeText.Get("Step.Work"), employee
                ? PersonIntakeText.Get("Step.Work.HintEmployee")
                : PersonIntakeText.Get("Step.Work.HintFamilyMember"), workFields.ToArray())
        };

        steps.Add(RecordStep("passports", PersonIntakeText.Get("Step.Passports"), PersonPendingTargets.Passport));
        steps.Add(RecordStep("education", PersonIntakeText.Get("Step.Education"), PersonPendingTargets.Education));
        // Medical records and travel history are entered later from their own navigation, not this wizard.
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
        var others = OthersStep(person);
        if (others.OtherOptions.Count > 0)
            steps.Add(others);
        steps.Add(new PersonIntakeStep
        {
            Id = "review",
            Title = PersonIntakeText.Get("Step.Review"),
            Hint = PersonIntakeText.Get("Step.Review.Hint"),
            Kind = PersonIntakeStepKind.Review
        });
        return steps;
    }

    private static PersonIntakeStep FormStep(string id, string title, string hint, string[] keys) =>
        new()
        {
            Id = id,
            Title = title,
            Hint = hint,
            Kind = PersonIntakeStepKind.Form,
            FieldKeys = keys
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
        // Purpose of arrival / employee Other records deferred — not required for now.
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