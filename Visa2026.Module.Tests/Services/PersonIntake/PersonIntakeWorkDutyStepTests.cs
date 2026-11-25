using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeWorkDutyStepTests
{
    [Fact]
    public void Employee_ShowsTheStep_AfterPositionHistory_AndBeforeReadOnlySteps()
    {
        var employee = new Person();
        PersonRoleHelper.ApplyRole(employee, PersonRecordRole.Employee);
        employee.FamilyMembers.Add(new Person { FirstName = "Child" });

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf("positions") < ids.IndexOf("work-duties"));
        Assert.True(ids.IndexOf("work-duties") < ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId));
        Assert.Equal(PersonIntakeStepKind.Record, PersonIntakeStepCatalog.Steps(employee).First(step => step.Id == "work-duties").Kind);
    }

    [Fact]
    public void FamilyMember_HidesTheStep()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id);

        Assert.DoesNotContain("work-duties", ids);
    }

    [Fact]
    public void EmptyPurpose_DoesNotStayUnresolved()
    {
        var employee = new Person();
        PersonRoleHelper.ApplyRole(employee, PersonRecordRole.Employee);

        var finding = PersonIntakeCompleteness.Evaluate(employee)
            .First(item => item.TargetKey == PersonPendingTargets.WorkDuty);

        Assert.Equal(PersonIntakeItemState.Filled, finding.State);
    }

    [Fact]
    public void DraftWithADescription_CountsAsFilled()
    {
        var state = new PersonIntakeWizardState();
        state.AddWorkDutyDraft().Description = "Montaj";

        Assert.Contains(PersonPendingTargets.WorkDuty, state.DraftSatisfiedKeys());
    }
}
