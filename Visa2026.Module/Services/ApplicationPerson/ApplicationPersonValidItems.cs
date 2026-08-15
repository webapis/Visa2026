using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Valid/active resolve rules for ApplicationProfileInstance Person M2M (plan §10.2).
/// Officer link/create: only valid, not-expired Passport, Visa, WorkPermitItem,
/// InvitationItem, BorderZoneItem, and MedicalRecord rows may be auto-linked.
/// VISA2014 import (<see cref="MigrationImportContext.IsDataImport"/>) uses PersonCurrentItems
/// so historical expired rows still link.
/// </summary>
public static class ApplicationProfileInstancePersonValidItems
{
    /// <summary>
    /// Officer UI and start-application enforce §10.2 validity.
    /// VISA2014 / DataImporter scopes skip the gate (past related data).
    /// </summary>
    public static bool EnforceOfficerLinkValidity => !MigrationImportContext.IsDataImport;

    public static bool CanLinkEntity(object? entity, DateTime? asOf = null) => entity switch
    {
        Passport passport => CanLinkPassport(passport, asOf),
        Visa visa => CanLinkVisa(visa, asOf),
        MedicalRecord medical => CanLinkMedicalRecord(medical, asOf),
        InvitationItem invitationItem => CanLinkInvitationItem(invitationItem, asOf),
        WorkPermitItem workPermitItem => CanLinkWorkPermitItem(workPermitItem, asOf),
        BorderZoneItem borderZoneItem => CanLinkBorderZoneItem(borderZoneItem, asOf),
        _ => true,
    };

    public static bool CanLinkPassport(Passport? passport, DateTime? asOf = null) =>
        passport != null && !IsExpiredAsOf(passport.ExpirationDate, asOf);

    public static bool CanLinkVisa(Visa? visa, DateTime? asOf = null)
    {
        if (visa == null || visa.IsCancelled || visa.IsChanged)
            return false;

        var asOfDate = AsOfDate(asOf);
        if (visa.StartDate == default || visa.StartDate.Date > asOfDate)
            return false;

        return !IsExpiredAsOf(visa.ExpirationDate, asOfDate);
    }

    public static bool CanLinkMedicalRecord(MedicalRecord? record, DateTime? asOf = null) =>
        record != null && !IsExpiredAsOf(record.ExpirationDate, asOf);

    public static bool CanLinkInvitationItem(InvitationItem? item, DateTime? asOf = null)
    {
        if (item == null || item.Invitation == null)
            return false;
        if (item.IsCancelled || item.IsChanged || item.IsUsed)
            return false;

        return !IsExpiredAsOf(item.Invitation.ExpirationDate, asOf);
    }

    public static bool CanLinkWorkPermitItem(WorkPermitItem? item, DateTime? asOf = null)
    {
        if (item == null || item.IsCancelled)
            return false;

        return !IsExpiredAsOf(item.ExpirationDate, asOf);
    }

    public static bool CanLinkBorderZoneItem(BorderZoneItem? item, DateTime? asOf = null)
    {
        if (item == null || item.IsCancelled || item.BorderZone == null)
            return false;

        return !IsExpiredAsOf(item.BorderZone.ExpirationDate, asOf);
    }

    public static Passport? ResolvePassport(Person? person, DateTime? asOf = null) =>
        EnforceOfficerLinkValidity
            ? person?.Passports?
                .Where(p => CanLinkPassport(p, asOf))
                .OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(p => p.ID)
                .FirstOrDefault()
            : PersonCurrentItems.GetCurrentPassport(person);

    public static Visa? ResolveVisa(Person? person, DateTime? asOf = null) =>
        EnforceOfficerLinkValidity
            ? person?.Passports?
                .Where(p => p != null)
                .SelectMany(p => p.Visas ?? Array.Empty<Visa>())
                .Where(v => CanLinkVisa(v, asOf))
                .OrderByDescending(v => v.StartDate)
                .ThenByDescending(v => v.IssueDate)
                .ThenByDescending(v => v.ID)
                .FirstOrDefault()
            : PersonCurrentItems.GetCurrentVisa(person, asOf ?? DateTime.Today);

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

    public static MedicalRecord? ResolveMedical(Person? person, DateTime? asOf = null) =>
        EnforceOfficerLinkValidity
            ? person?.MedicalRecords?
                .Where(m => CanLinkMedicalRecord(m, asOf))
                .OrderByDescending(m => m.IssueDate)
                .ThenByDescending(m => m.ID)
                .FirstOrDefault()
            : PersonCurrentItems.GetCurrentMedicalRecord(person);

    public static InvitationItem? ResolveInvitationItem(Person? person, DateTime? asOf = null) =>
        EnforceOfficerLinkValidity
            ? person?.InvitationItems?
                .Where(i => CanLinkInvitationItem(i, asOf))
                .OrderByDescending(i => i.Invitation?.IssuedDate ?? default)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault()
            : PersonCurrentItems.GetCurrentInvitationItem(person);

    public static WorkPermitItem? ResolveWorkPermitItem(Person? person, DateTime? asOf = null) =>
        EnforceOfficerLinkValidity
            ? person?.WorkPermitItems?
                .Where(w => CanLinkWorkPermitItem(w, asOf))
                .OrderByDescending(w => w.StartDate)
                .ThenByDescending(w => w.ID)
                .FirstOrDefault()
            : PersonCurrentItems.GetCurrentWorkPermitItem(person);

    public static RejectionItem? ResolveRejectionItem(Person? person) =>
        PersonCurrentItems.GetCurrentRejectionItem(person);

    public static BorderZoneItem? ResolveBorderZoneItem(IObjectSpace objectSpace, Person? person, DateTime? asOf = null)
    {
        if (objectSpace == null || person == null || person.ID == Guid.Empty)
            return null;

        if (!EnforceOfficerLinkValidity)
        {
            return objectSpace.GetObjectsQuery<BorderZoneItem>()
                .Where(b => b.Person != null && b.Person.ID == person.ID && !b.IsCancelled)
                .OrderByDescending(b => b.ID)
                .FirstOrDefault();
        }

        return objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && b.Person.ID == person.ID)
            .OrderByDescending(b => b.ID)
            .ToList()
            .FirstOrDefault(b => CanLinkBorderZoneItem(b, asOf));
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

    private static DateTime AsOfDate(DateTime? asOf) => (asOf ?? DateTime.Today).Date;

    private static bool IsExpiredAsOf(DateTime? expirationDate, DateTime? asOf) =>
        expirationDate.HasValue && expirationDate.Value.Date < AsOfDate(asOf);
}