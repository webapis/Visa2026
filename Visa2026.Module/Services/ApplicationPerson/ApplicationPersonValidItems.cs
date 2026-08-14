using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>Valid/active resolve rules for ApplicationProfileInstance Person M2M (plan §10.2).</summary>
public static class ApplicationProfileInstancePersonValidItems
{
    public static Passport? ResolvePassport(Person? person)
    {
        var passport = PersonCurrentItems.GetCurrentPassport(person);
        if (passport?.ExpirationDate is DateTime exp && exp.Date < DateTime.Today)
            return null;

        return passport;
    }

    public static Visa? ResolveVisa(Person? person) =>
        PersonCurrentItems.GetCurrentVisa(person, DateTime.Today);

    public static Education? ResolveEducation(Person? person) =>
        PersonCurrentItems.GetCurrentEducation(person);

    public static AddressOfResidence? ResolveAddress(Person? person) =>
        PersonCurrentItems.GetCurrentAddressOfResidence(person);

    public static EmployeePositionHistory? ResolvePosition(Person? person) =>
        PersonCurrentItems.GetCurrentPositionHistory(person);

    public static WorkDuty? ResolveWorkDuty(Person? person) =>
        PersonCurrentItems.GetCurrentWorkDuty(person);

    public static EmployeeSalary? ResolveSalary(Person? person) =>
        PersonCurrentItems.GetCurrentSalary(person);

    public static MedicalRecord? ResolveMedical(Person? person) =>
        PersonCurrentItems.GetCurrentMedicalRecord(person);

    public static InvitationItem? ResolveInvitationItem(Person? person) =>
        PersonCurrentItems.GetCurrentInvitationItem(person);

    public static WorkPermitItem? ResolveWorkPermitItem(Person? person) =>
        PersonCurrentItems.GetCurrentWorkPermitItem(person);

    public static RejectionItem? ResolveRejectionItem(Person? person) =>
        PersonCurrentItems.GetCurrentRejectionItem(person);

    public static BorderZoneItem? ResolveBorderZoneItem(IObjectSpace objectSpace, Person? person)
    {
        if (objectSpace == null || person == null || person.ID == Guid.Empty)
            return null;

        return objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && b.Person.ID == person.ID && !b.IsCancelled)
            .OrderByDescending(b => b.ID)
            .FirstOrDefault();
    }

    public static TravelHistory? ResolveTravelHistory(Person? person)
    {
        if (person?.TravelHistories == null)
            return null;

        return person.TravelHistories
            .Where(t => t != null)
            .OrderByDescending(t => t.TravelDate.Date)
            .ThenByDescending(t => t.ID)
            .FirstOrDefault();
    }
}
