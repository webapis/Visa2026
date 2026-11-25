using System;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeVisaStepTests
{
    [Fact]
    public void Employee_WithAVisa_ShowsTheStep_BetweenFamilyMembersAndWorkPermits()
    {
        var employee = Employee();
        employee.FamilyMembers.Add(new Person { FirstName = "Aýna" });
        AddVisa(employee, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1));
        employee.WorkPermitItems.Add(new WorkPermitItem
        {
            Person = employee,
            StartDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddYears(1),
            WorkPermitNumber = "WP"
        });

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.FamilyMembersStepId) < ids.IndexOf(PersonIntakeStepCatalog.VisasStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.VisasStepId) < ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void FamilyMember_WithAVisa_ShowsTheStep_AfterEditableSteps()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        AddVisa(person, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1));

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.VisasStepId, ids);
        Assert.True(ids.IndexOf("education") < ids.IndexOf(PersonIntakeStepCatalog.VisasStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.VisasStepId) < ids.IndexOf("review"));
        Assert.DoesNotContain(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
    }

    [Fact]
    public void Person_WithoutVisas_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.VisasStepId, ids);
    }

    [Fact]
    public void CurrentVisa_IsActive_AndOlderVisa_IsPrevious()
    {
        var employee = Employee();
        var today = DateTime.Today;
        var current = AddVisa(employee, today.AddMonths(-1), today.AddYears(1));
        var older = AddVisa(employee, today.AddYears(-3), today.AddYears(-2));

        Assert.Equal(PersonIntakeVisaState.Active, PersonIntakeVisaState.Code(current, null));
        Assert.Equal(PersonIntakeVisaState.Previous, PersonIntakeVisaState.Code(older, null));
        Assert.True(PersonIntakeVisaState.ShowsDays(PersonIntakeVisaState.Active));
        Assert.False(PersonIntakeVisaState.ShowsDays(PersonIntakeVisaState.Previous));
    }

    [Fact]
    public void CancelledVisa_IsCancelled()
    {
        var employee = Employee();
        var visa = AddVisa(employee, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(1));
        visa.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued
        });

        Assert.Equal(PersonIntakeVisaState.Cancelled, PersonIntakeVisaState.Code(visa, null));
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static Visa AddVisa(Person person, DateTime start, DateTime expiration)
    {
        var passport = new Passport { Person = person, PassportNumber = "P" + person.Passports.Count };
        person.Passports.Add(passport);
        var visa = new Visa
        {
            Passport = passport,
            VisaNumber = "V" + passport.Visas.Count,
            StartDate = start,
            IssueDate = start,
            ExpirationDate = expiration
        };
        passport.Visas.Add(visa);
        return visa;
    }
}
