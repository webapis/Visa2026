using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeFamilyMembersStepTests
{
    [Fact]
    public void Employee_WithFamilyMembers_ShowsTheStep_AfterEditableSteps()
    {
        var employee = Employee();
        employee.FamilyMembers.Add(new Person { FirstName = "Aýna" });

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.FamilyMembersStepId, ids);
        Assert.True(ids.IndexOf("positions") < ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void Employee_WithoutFamilyMembers_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.FamilyMembersStepId, ids);
    }

    [Fact]
    public void FamilyMemberRole_DoesNotShowTheStep()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        person.FamilyMembers.Add(new Person { FirstName = "Aýna" });

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.FamilyMembersStepId, ids);
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }
}
