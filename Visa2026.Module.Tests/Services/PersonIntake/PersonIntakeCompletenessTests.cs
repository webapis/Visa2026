using System.Linq;
using System.Reflection;
using DevExpress.Persistent.Validation;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeCompletenessTests
{
    [Fact]
    public void Rollup_MapsPassportPending_OntoThePassportFlag()
    {
        var person = Employee();
        person.PendingItems.Add(Pending(PersonPendingTargets.Passport, PersonIncompleteDataLabels.Passport));

        PersonPendingRollup.Apply(person, replaceLegacy: true, markedBy: "officer");

        Assert.True(person.IsDataIncomplete);
        Assert.True(person.IncompleteMissingPassport);
        Assert.False(person.IncompleteMissingEducation);
        Assert.Equal("officer", person.IncompleteMarkedBy);
        Assert.NotNull(person.IncompleteMarkedOn);
    }

    [Fact]
    public void Rollup_ClearsFlagsAndNotes_WhenTheLastPendingItemIsGone()
    {
        var person = Employee();
        person.IsDataIncomplete = true;
        person.IncompleteMissingPassport = true;
        person.IncompleteNotes = "waiting on the scan";
        person.IncompleteMarkedBy = "officer";

        PersonPendingRollup.Apply(person, replaceLegacy: true, markedBy: "officer");

        Assert.False(person.IsDataIncomplete);
        Assert.False(person.IncompleteMissingPassport);
        Assert.Null(person.IncompleteNotes);
        Assert.Null(person.IncompleteMarkedBy);
        Assert.Null(person.IncompleteMarkedOn);
    }

    [Fact]
    public void EmployeeForeignAddress_IsUnresolvedUntilMarkedPending()
    {
        var person = Employee();

        Assert.Equal(
            PersonIntakeItemState.Unresolved,
            State(person, PersonPendingTargets.ForeignAddress));

        person.PendingItems.Add(Pending(PersonPendingTargets.ForeignAddress, PersonIncompleteDataLabels.PersonalData));

        Assert.Equal(
            PersonIntakeItemState.Pending,
            State(person, PersonPendingTargets.ForeignAddress));
    }

    [Fact]
    public void FamilyMember_DoesNotRequireForeignAddressOrMaritalStatus()
    {
        var person = Family();

        var findings = PersonIntakeCompleteness.Evaluate(person);

        Assert.DoesNotContain(findings, finding => finding.TargetKey == PersonPendingTargets.ForeignAddress);
        Assert.DoesNotContain(findings, finding => finding.TargetKey == PersonPendingTargets.MaritalStatus);
        Assert.Equal(
            PersonIntakeItemState.Unresolved,
            State(person, PersonPendingTargets.SponsoringEmployee));
    }

    [Fact]
    public void AcceptMissing_MarksEmptyRequiredFields_AndLeavesOptionalEmpty()
    {
        var person = Employee();
        person.FirstName = "Demo";
        person.LastName = "Name";
        var state = new PersonIntakeWizardState();

        PersonIntakeCompleteness.AcceptMissingAsNotReceived(person, state);

        Assert.Contains(PersonPendingTargets.Gender, state.PendingKeys);
        Assert.Contains(PersonPendingTargets.BirthPlace, state.PendingKeys);
        Assert.Contains(PersonPendingTargets.Passport, state.PendingKeys);
        Assert.Contains(PersonPendingTargets.Education, state.PendingKeys);
        Assert.Contains(PersonPendingTargets.ForeignAddress, state.PendingKeys);
        Assert.DoesNotContain(PersonPendingTargets.Photo, state.PendingKeys);
        Assert.DoesNotContain(PersonPendingTargets.PersonDocument, state.PendingKeys);
        Assert.DoesNotContain(PersonPendingTargets.EmployeeSalary, state.PendingKeys);
        Assert.DoesNotContain(PersonPendingTargets.FirstName, state.PendingKeys);
    }

    [Fact]
    public void AcceptMissing_DropsUnfinishedPassport_AndMarksTheStepNotReceived()
    {
        var person = Employee();
        person.FirstName = "Demo";
        person.LastName = "Name";
        var state = new PersonIntakeWizardState();
        state.PassportDrafts.Add(new PersonIntakePassportDraft { PassportNumber = "AB123" });

        PersonIntakeCompleteness.AcceptMissingAsNotReceived(person, state);

        Assert.Empty(state.PassportDrafts);
        Assert.Contains(PersonPendingTargets.Passport, state.PendingKeys);
    }

    [Fact]
    public void AcceptMissing_KeepsACompletePassport()
    {
        var person = Employee();
        person.FirstName = "Demo";
        person.LastName = "Name";
        var state = new PersonIntakeWizardState();
        state.PassportDrafts.Add(new PersonIntakePassportDraft
        {
            PassportNumber = "AB123",
            PassportTypeId = Guid.NewGuid(),
            IssuedCountryId = Guid.NewGuid(),
            Authority = "Ashgabat",
            IssueDate = new DateTime(2020, 1, 1),
            ExpirationDate = new DateTime(2030, 1, 1)
        });

        PersonIntakeCompleteness.AcceptMissingAsNotReceived(person, state);

        Assert.Single(state.PassportDrafts);
        Assert.DoesNotContain(PersonPendingTargets.Passport, state.PendingKeys);
    }

    [Fact]
    public void AcceptMissing_DoesNotDeferFamilySponsorOrRelationship()
    {
        var person = Family();
        person.FirstName = "Demo";
        person.LastName = "Name";
        var state = new PersonIntakeWizardState();

        PersonIntakeCompleteness.AcceptMissingAsNotReceived(person, state);

        Assert.DoesNotContain(PersonPendingTargets.SponsoringEmployee, state.PendingKeys);
        Assert.DoesNotContain(PersonPendingTargets.Relationship, state.PendingKeys);
        Assert.Equal(
            PersonIntakeItemState.Unresolved,
            PersonIntakeCompleteness.Evaluate(person, state.PendingKeys, state.DraftSatisfiedKeys())
                .Single(finding => finding.TargetKey == PersonPendingTargets.SponsoringEmployee).State);
    }

    [Fact]
    public void OptionalCv_DoesNotBlock_UntilTheOfficerSwitchesItOn()
    {
        var person = Employee();

        Assert.Equal(PersonIntakeItemState.Filled, State(person, PersonPendingTargets.PersonDocument));

        person.PendingItems.Add(Pending(PersonPendingTargets.PersonDocument, PersonIncompleteDataLabels.Cv));

        Assert.Equal(PersonIntakeItemState.Pending, State(person, PersonPendingTargets.PersonDocument));
    }

    [Fact]
    public void RequiredRule_RunsOnlyWhenThatTargetIsNotPending()
    {
        var foreignAddress = Criteria(nameof(Person.ForeignAddress));
        Assert.Contains("Person.ForeignAddress", foreignAddress);
        Assert.Contains(PersonRoleHelper.EmployeeCriteria, foreignAddress);

        var firstName = Criteria(nameof(Person.FirstName));
        Assert.Equal(string.Empty, firstName);

        var sponsor = Criteria(nameof(Person.SponsoringEmployee));
        Assert.DoesNotContain("PendingItems", sponsor);
    }

    [Fact]
    public void PersonalNumberPending_IsClearedToNull_NotTheSentinelZero()
    {
        var person = Employee();
        person.PersonalNumber = "0";
        person.PendingItems.Add(Pending(PersonPendingTargets.PersonalNumber, PersonIncompleteDataLabels.PersonalData));

        PersonIntakeCompleteness.ClearPendingScalars(person);

        Assert.Null(person.PersonalNumber);
        Assert.True(person.IsPersonalNumberUniqueAmongActive);
    }

    [Fact]
    public void PendingEducation_SkipsTheAdultFamilyDefault_UntilTheMarkIsRemoved()
    {
        var person = Family();
        person.DateOfBirth = new System.DateTime(1990, 5, 1);
        person.PendingItems.Add(Pending(PersonPendingTargets.Education, PersonIncompleteDataLabels.Education));

        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(person));

        person.PendingItems.Clear();

        Assert.True(FamilyMemberDefaultEducation.ShouldEnsure(person));
    }

    [Fact]
    public void LegacyFlag_IsLeftAlone_UntilMaterializeAndSave()
    {
        var person = Employee();
        person.IsDataIncomplete = true;
        person.IncompleteMissingPassport = true;
        person.IncompleteNotes = "from the old popup";

        Assert.True(PersonIntakeCompleteness.IsLegacyOnly(person));
        Assert.Contains(PersonIncompleteDataLabels.Passport, PersonIntakeCompleteness.LegacyAreaKeys(person));
        Assert.Empty(person.PendingItems);

        PersonPendingRollup.Apply(person, replaceLegacy: false, markedBy: "officer");

        Assert.True(person.IsDataIncomplete);
        Assert.True(person.IncompleteMissingPassport);
        Assert.Empty(person.PendingItems);

        PersonIntakeCompleteness.MaterializeLegacy(person);
        PersonPendingRollup.Apply(person, replaceLegacy: true, markedBy: "officer");

        Assert.True(person.IsDataIncomplete);
        Assert.True(person.IncompleteMissingPassport);
        Assert.False(person.IncompleteMissingEducation);
        Assert.Contains(person.PendingItems, item => item.TargetKey == "Legacy.Passport");
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static Person Family()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        return person;
    }

    private static PersonPendingItem Pending(string targetKey, string areaKey) =>
        new() { TargetKey = targetKey, AreaKey = areaKey };

    private static PersonIntakeItemState State(Person person, string targetKey) =>
        PersonIntakeCompleteness.Evaluate(person).Single(finding => finding.TargetKey == targetKey).State;

    private static string Criteria(string propertyName)
    {
        var property = typeof(Person).GetProperty(propertyName);
        var attribute = property.GetCustomAttribute<RuleRequiredFieldAttribute>();
        return attribute?.TargetCriteria ?? string.Empty;
    }
}
