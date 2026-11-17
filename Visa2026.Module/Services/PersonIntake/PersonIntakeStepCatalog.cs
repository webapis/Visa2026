using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public enum PersonIntakeStepKind
{
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
        var personFields = new List<string>();
        if (!employee)
            personFields.Add(PersonPendingTargets.SponsoringEmployee);

        personFields.Add(PersonPendingTargets.FirstName);
        personFields.Add(PersonPendingTargets.LastName);
        personFields.Add(PersonPendingTargets.DateOfBirth);
        personFields.Add(PersonPendingTargets.Age);
        personFields.Add(PersonPendingTargets.BirthPlace);
        personFields.Add(PersonPendingTargets.CountryOfBirth);
        personFields.Add(PersonPendingTargets.Gender);
        if (employee)
        {
            personFields.Add(PersonPendingTargets.MaritalStatus);
            personFields.Add(PersonPendingTargets.VisaApplicationFamilyMembersText);
        }

        personFields.Add(PersonPendingTargets.Nationality);
        personFields.Add(PersonPendingTargets.PersonalNumber);
        if (employee)
        {
            personFields.Add(PersonPendingTargets.ForeignAddress);
            personFields.Add(PersonPendingTargets.ForeignAddressCountry);
        }

        personFields.Add(PersonPendingTargets.ProjectContract);
        personFields.Add(PersonPendingTargets.Subcontractor);
        if (!employee)
            personFields.Add(PersonPendingTargets.Relationship);
        personFields.Add(PersonPendingTargets.Photo);

        var steps = new List<PersonIntakeStep>
        {
            FormStep("person", "Person", "Person", "Same fields as the person detail form.", personFields.ToArray())
        };

        steps.Add(RecordStep("passports", "Passports", PersonPendingTargets.Passport));
        steps.Add(RecordStep("education", "Education", PersonPendingTargets.Education));
        steps.Add(RecordStep("medical", "Medical", PersonPendingTargets.MedicalRecord));
        steps.Add(RecordStep("residence", "Residence", PersonPendingTargets.AddressOfResidence));
        steps.Add(OthersStep(person));
        steps.Add(new PersonIntakeStep
        {
            Id = "review",
            Title = "Review",
            Hint = "Check what is complete and what is still outstanding.",
            Kind = PersonIntakeStepKind.Review
        });
        return steps;
    }

    private static PersonIntakeStep FormStep(string id, string title, string subtitle, string hint, string[] keys) =>
        new()
        {
            Id = id,
            Title = title,
            Hint = hint,
            Kind = PersonIntakeStepKind.Form,
            FieldKeys = keys
        };

    private static PersonIntakeStep RecordStep(string id, string title, string recordKey) =>
        new()
        {
            Id = id,
            Title = title,
            Hint = "Add a record now or mark the whole record as not received.",
            Kind = PersonIntakeStepKind.Record,
            RecordTargetKey = recordKey
        };

    private static PersonIntakeStep OthersStep(Person person)
    {
        var employee = person.PersonRole == PersonRecordRole.Employee;
        var options = employee
            ? new[]
            {
                Other(PersonPendingTargets.PersonDocument, "CV & personal files", "CV and certificate scans."),
                Other(PersonPendingTargets.EmployeePositionHistory, "Position history", "Current and previous positions."),
                Other(PersonPendingTargets.TravelHistory, "Travel history", "Border crossings and trips."),
                Other(PersonPendingTargets.WorkDuty, "Purpose of arrival", "Business trip purpose and dates."),
                Other(PersonPendingTargets.EmployeeSalary, "Salaries", "Salary periods and amounts.")
            }
            : new[]
            {
                Other(PersonPendingTargets.PersonFamilyRelationDocument, "Family relation documents", "Marriage or birth certificate scans."),
                Other(PersonPendingTargets.TravelHistory, "Travel history", "Border crossings and trips.")
            };

        return new PersonIntakeStep
        {
            Id = "others",
            Title = "Other records",
            Hint = "Optional records. Mark only what you are still waiting for.",
            Kind = PersonIntakeStepKind.Others,
            OtherOptions = options
        };
    }

    private static PersonIntakeOtherRecordOption Other(string key, string label, string help) =>
        new() { TargetKey = key, Label = label, Help = help };
}