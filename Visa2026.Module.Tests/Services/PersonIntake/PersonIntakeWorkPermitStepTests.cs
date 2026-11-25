using System;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeWorkPermitStepTests
{
    [Fact]
    public void Employee_WithWorkPermits_ShowsTheStep_AfterPositionHistory()
    {
        var employee = Employee();
        employee.WorkPermitItems.Add(Permit(employee, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1)));

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
        Assert.True(ids.IndexOf("positions") < ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void ReadOnlySteps_FollowEditableSteps_FamilyMembersThenWorkPermits()
    {
        var employee = Employee();
        employee.FamilyMembers.Add(new Person { FirstName = "Aýna" });
        employee.WorkPermitItems.Add(Permit(employee, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1)));

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf("positions") < ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId) < ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void Employee_WithoutWorkPermits_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
    }

    [Fact]
    public void FamilyMemberRole_DoesNotShowTheStep()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        person.WorkPermitItems.Add(Permit(person, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1)));

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
    }

    [Fact]
    public void CurrentPermit_IsActive_AndOlderPermit_IsPrevious()
    {
        var employee = Employee();
        var today = DateTime.Today;
        var current = Permit(employee, today.AddMonths(-1), today.AddYears(1));
        var older = Permit(employee, today.AddYears(-3), today.AddYears(-2));
        employee.WorkPermitItems.Add(current);
        employee.WorkPermitItems.Add(older);

        Assert.Equal(PersonIntakeWorkPermitState.Active, PersonIntakeWorkPermitState.Code(current, null));
        Assert.Equal(PersonIntakeWorkPermitState.Previous, PersonIntakeWorkPermitState.Code(older, null));
        Assert.True(PersonIntakeWorkPermitState.ShowsDays(PersonIntakeWorkPermitState.Active));
        Assert.False(PersonIntakeWorkPermitState.ShowsDays(PersonIntakeWorkPermitState.Previous));
    }

    [Fact]
    public void CancelledPermit_IsCancelled_EvenWhenItsDatesStillRun()
    {
        var employee = Employee();
        var today = DateTime.Today;
        var item = Permit(employee, today.AddMonths(-1), today.AddYears(1));
        item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued
        });
        employee.WorkPermitItems.Add(item);

        Assert.Equal(PersonIntakeWorkPermitState.Cancelled, PersonIntakeWorkPermitState.Code(item, null));
        Assert.False(PersonIntakeWorkPermitState.ShowsDays(PersonIntakeWorkPermitState.Cancelled));
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static WorkPermitItem Permit(Person person, DateTime start, DateTime expiration) =>
        new()
        {
            Person = person,
            StartDate = start,
            ExpirationDate = expiration,
            WorkPermitNumber = "WP"
        };
}
