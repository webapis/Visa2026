using System;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeApplicationProfileInstanceStepTests
{
    [Fact]
    public void Employee_WithAnInstance_ShowsTheStep_AfterWorkPermits()
    {
        var employee = Employee();
        employee.WorkPermitItems.Add(new WorkPermitItem
        {
            Person = employee,
            StartDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddYears(1),
            WorkPermitNumber = "WP"
        });
        employee.ApplicationProfileInstances.Add(Instance("12/7010"));

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId) < ids.IndexOf(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void FamilyMember_WithAnInstance_ShowsTheStep_AfterEditableSteps()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        person.ApplicationProfileInstances.Add(Instance("FM-1"));

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId, ids);
        Assert.True(ids.IndexOf("education") < ids.IndexOf(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId) < ids.IndexOf("review"));
        Assert.DoesNotContain(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
    }

    [Fact]
    public void Person_WithoutInstances_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.ApplicationProfileInstancesStepId, ids);
    }

    [Fact]
    public void NewInstance_UsesTheOfficeProgressChip()
    {
        var instance = Instance("1/1");

        Assert.False(string.IsNullOrWhiteSpace(PersonIntakeApplicationProfileInstanceState.Label(instance)));
        Assert.Equal("is-info", PersonIntakeApplicationProfileInstanceState.Css(instance));
        Assert.Equal("is-valid", PersonIntakeApplicationProfileInstanceState.Css(ApplicationProfileInstanceProgressColorTab.Issued));
        Assert.Equal("is-expired", PersonIntakeApplicationProfileInstanceState.Css(ApplicationProfileInstanceProgressColorTab.Rejected));
        Assert.Equal("is-cancelled", PersonIntakeApplicationProfileInstanceState.Css(ApplicationProfileInstanceProgressColorTab.Cancelled));
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static ApplicationProfileInstance Instance(string number) =>
        new()
        {
            FullApplicationNumber = number,
            ApplicationDate = DateTime.Today,
            ApplicationProfile = new ApplicationProfile { Name = "Invitation", Code = "INV" }
        };
}
