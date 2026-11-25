using System;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonIntake;

public class PersonIntakeInvitationStepTests
{
    [Fact]
    public void Employee_WithAnInvitation_ShowsTheStep_BetweenVisasAndWorkPermits()
    {
        var employee = Employee();
        AddVisa(employee);
        AddInvitation(employee, DateTime.Today.AddYears(1));
        employee.WorkPermitItems.Add(new WorkPermitItem
        {
            Person = employee,
            StartDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddYears(1),
            WorkPermitNumber = "WP"
        });

        var ids = PersonIntakeStepCatalog.Steps(employee).Select(step => step.Id).ToList();

        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.VisasStepId) < ids.IndexOf(PersonIntakeStepCatalog.InvitationsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.InvitationsStepId) < ids.IndexOf(PersonIntakeStepCatalog.WorkPermitsStepId));
    }

    [Fact]
    public void FamilyMember_WithAnInvitation_ShowsTheStep_AfterEditableSteps()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        AddInvitation(person, DateTime.Today.AddYears(1));

        var ids = PersonIntakeStepCatalog.Steps(person).Select(step => step.Id).ToList();

        Assert.Contains(PersonIntakeStepCatalog.InvitationsStepId, ids);
        Assert.True(ids.IndexOf("education") < ids.IndexOf(PersonIntakeStepCatalog.InvitationsStepId));
        Assert.True(ids.IndexOf(PersonIntakeStepCatalog.InvitationsStepId) < ids.IndexOf("review"));
        Assert.DoesNotContain(PersonIntakeStepCatalog.WorkPermitsStepId, ids);
    }

    [Fact]
    public void Person_WithoutInvitations_HidesTheStep()
    {
        var ids = PersonIntakeStepCatalog.Steps(Employee()).Select(step => step.Id);

        Assert.DoesNotContain(PersonIntakeStepCatalog.InvitationsStepId, ids);
    }

    [Fact]
    public void InForceInvitation_IsReady_AndPastUnusedInvitation_IsExpired()
    {
        var employee = Employee();
        var ready = AddInvitation(employee, DateTime.Today.AddYears(1));
        var expired = AddInvitation(employee, DateTime.Today.AddDays(-1));

        Assert.Equal(PersonIntakeInvitationState.Ready, PersonIntakeInvitationState.Code(ready));
        Assert.Equal(PersonIntakeInvitationState.Expired, PersonIntakeInvitationState.Code(expired));
    }

    [Fact]
    public void InvitationWithIssuedVisa_IsUsed()
    {
        var employee = Employee();
        var item = AddInvitation(employee, DateTime.Today.AddDays(-1));
        item.IssuedVisa = new Visa { VisaNumber = "V-USED" };

        Assert.Equal(PersonIntakeInvitationState.Used, PersonIntakeInvitationState.Code(item));
    }

    private static Person Employee()
    {
        var person = new Person();
        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
        return person;
    }

    private static InvitationItem AddInvitation(Person person, DateTime expiration)
    {
        var invitation = new Invitation
        {
            InvitationNumber = "INV-" + person.InvitationItems.Count,
            IssuedDate = expiration.AddYears(-1),
            ExpirationDate = expiration
        };
        var item = new InvitationItem { Person = person, Invitation = invitation };
        person.InvitationItems.Add(item);
        return item;
    }

    private static void AddVisa(Person person)
    {
        var passport = new Passport { Person = person, PassportNumber = "P1" };
        person.Passports.Add(passport);
        passport.Visas.Add(new Visa
        {
            Passport = passport,
            VisaNumber = "V1",
            StartDate = DateTime.Today.AddMonths(-1),
            IssueDate = DateTime.Today.AddMonths(-1),
            ExpirationDate = DateTime.Today.AddYears(1)
        });
    }
}
