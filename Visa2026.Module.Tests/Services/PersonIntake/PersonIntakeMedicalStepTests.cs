using System;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeMedicalStepTests
{
    [Fact]
    public void Employee_WithAMedicalRecord_ShowsTheStep_BetweenInvitationsAndWorkPermits()
    {
        var employee = Employee();
        employee.InvitationItems.Add(new InvitationItem
        {
            Person = employee,
            Invitation = new Invitation
            {
                InvitationNumber = "INV",
                IssuedDate = DateTime.Today.AddYears(-1),
                ExpirationDate = DateTime.Today.AddYears(1)
            }
        });
        AddRecord(employee, DateTime.Today.AddMonths(-1), 365);
        employee.WorkPermitItems.Add(new WorkPermitItem
        {
            Person = employee,
            StartDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddYears(1),
            WorkPermitNumber = "WP"
        });

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.InvitationsStepId) < ids.IndexOf(PersonIntakeStepCatalog.MedicalRecordsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.MedicalRecordsStepId) < ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId));
    }

    [Fact]
    public void FamilyMember_WithAMedicalRecord_ShowsTheStep_AfterEditableSteps()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        AddRecord(person, DateTime.Today.AddMonths(-1), 365);

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.MedicalRecordsStepId, ids);
        Assert.True(ids.IndexOf("education") < ids.IndexOf(PersonIntakeStepCatalog.MedicalRecordsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.MedicalRecordsStepId) < ids.IndexOf("review"));
    }

    [Fact]
    public void Person_WithoutMedicalRecords_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.MedicalRecordsStepId, ids);
    }

    [Fact]
    public void LatestRecord_IsActive_AndOlderRecord_IsPrevious()
    {
        var employee = Employee();
        var older = AddRecord(employee, DateTime.Today.AddYears(-2), 30);
        var latest = AddRecord(employee, DateTime.Today.AddMonths(-1), 365);

        Assert.Equal(PersonIntakeMedicalState.Active, PersonIntakeMedicalState.Code(latest, null));
        Assert.Equal(PersonIntakeMedicalState.Previous, PersonIntakeMedicalState.Code(older, null));
        Assert.True(PersonIntakeMedicalState.ShowsDays(PersonIntakeMedicalState.Active));
        Assert.False(PersonIntakeMedicalState.ShowsDays(PersonIntakeMedicalState.Previous));
    }

    [Fact]
    public void OnlyRecord_PastItsDate_IsExpired()
    {
        var employee = Employee();
        var record = AddRecord(employee, DateTime.Today.AddDays(-40), 10);

        Assert.Equal(PersonIntakeMedicalState.Expired, PersonIntakeMedicalState.Code(record, null));
    }

    [Fact]
    public void UnrestrictedAccess_CannotChangeMedicalRecords_OnThePersonWizard()
    {
        var access = PersonIntakeWizardAccess.Unrestricted;

        Assert.False(access.CanAddRecord(PersonPendingTargets.MedicalRecord));
        Assert.False(access.CanEditRecord(PersonPendingTargets.MedicalRecord, null));
        Assert.False(access.CanEditRecord(PersonPendingTargets.MedicalRecord, Guid.NewGuid()));
        Assert.False(access.CanRemoveRecord(PersonPendingTargets.MedicalRecord, null));
        Assert.False(access.CanRemoveRecord(PersonPendingTargets.MedicalRecord, Guid.NewGuid()));
        Assert.True(access.CanAddRecord(PersonPendingTargets.Passport));
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static MedicalRecord AddRecord(Person person, DateTime issueDate, int days)
    {
        var record = new MedicalRecord
        {
            Person = person,
            DocumentNumber = "MED-" + person.MedicalRecords.Count,
            IssueDate = issueDate,
            ValidityDuration = new ValidityDuration { NumberOfDays = days }
        };
        person.MedicalRecords.Add(record);
        return record;
    }
}
