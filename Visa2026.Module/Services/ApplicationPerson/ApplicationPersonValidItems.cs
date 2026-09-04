using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Valid/active resolve rules for ApplicationProfileInstance Person M2M (plan §10.2).
/// Officer link/create: Visa, WorkPermitItem, InvitationItem, BorderZoneItem, and MedicalRecord
/// must be valid/not-expired. Passport expiration is not checked (previous expired booklet is OK).
/// VISA2014 import (<see cref="MigrationImportContext.IsDataImport"/>) uses PersonCurrentItems
/// so historical expired rows still link when Last-N is 1.
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

    public static bool CanLinkPassport(Passport? passport, DateTime? asOf = null)
    {
        _ = asOf;
        return passport != null;
    }

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
        if (item == null || item.IsCancelled || item.IsChanged)
            return false;

        return !IsExpiredAsOf(item.ExpirationDate, asOf);
    }

    public static bool CanLinkBorderZoneItem(BorderZoneItem? item, DateTime? asOf = null)
    {
        if (item == null || item.IsCancelled || item.IsChanged || item.BorderZone == null)
            return false;

        return !IsExpiredAsOf(item.BorderZone.ExpirationDate, asOf);
    }

    public static Passport? ResolvePassport(Person? person, DateTime? asOf = null) =>
        ResolvePassports(person, 1, asOf).FirstOrDefault();

    public static IReadOnlyList<Passport> ResolvePassports(Person? person, int lastCount, DateTime? asOf = null)
    {
        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        if (person?.Passports == null)
            return [];

        if (!EnforceOfficerLinkValidity && lastCount == 1)
        {
            var current = PersonCurrentItems.GetCurrentPassport(person);
            return current == null ? [] : [current];
        }

        return person.Passports
            .Where(p => CanLinkPassport(p, asOf))
            .OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
            .ThenByDescending(p => p.ID)
            .Take(lastCount)
            .ToList();
    }

    public static Visa? ResolveVisa(Person? person, DateTime? asOf = null) =>
        ResolveVisas(person, 1, asOf).FirstOrDefault();

    public static IReadOnlyList<Visa> ResolveVisas(Person? person, int lastCount, DateTime? asOf = null)
    {
        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        if (person?.Passports == null)
            return [];

        if (!EnforceOfficerLinkValidity && lastCount == 1)
        {
            var current = PersonCurrentItems.GetCurrentVisa(person, asOf ?? DateTime.Today);
            return current == null ? [] : [current];
        }

        IEnumerable<Visa> query = person.Passports
            .Where(p => p != null)
            .SelectMany(p => p.Visas ?? Array.Empty<Visa>());
        if (EnforceOfficerLinkValidity)
            query = query.Where(v => CanLinkVisa(v, asOf));

        return query
            .OrderByDescending(v => v.StartDate)
            .ThenByDescending(v => v.IssueDate)
            .ThenByDescending(v => v.ID)
            .Take(lastCount)
            .ToList();
    }

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
        ResolveInvitationItems(person, 1, asOf).FirstOrDefault();

    public static IReadOnlyList<InvitationItem> ResolveInvitationItems(Person? person, int lastCount, DateTime? asOf = null)
    {
        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        if (person?.InvitationItems == null)
            return [];

        if (!EnforceOfficerLinkValidity && lastCount == 1)
        {
            var current = PersonCurrentItems.GetCurrentInvitationItem(person);
            return current == null ? [] : [current];
        }

        IEnumerable<InvitationItem> query = person.InvitationItems.Where(i => i != null);
        if (EnforceOfficerLinkValidity)
            query = query.Where(i => CanLinkInvitationItem(i, asOf));

        return query
            .OrderByDescending(i => i.Invitation?.IssuedDate ?? default)
            .ThenByDescending(i => i.ID)
            .Take(lastCount)
            .ToList();
    }

    public static WorkPermitItem? ResolveWorkPermitItem(Person? person, DateTime? asOf = null) =>
        ResolveWorkPermitItems(person, 1, asOf).FirstOrDefault();

    public static IReadOnlyList<WorkPermitItem> ResolveWorkPermitItems(Person? person, int lastCount, DateTime? asOf = null)
    {
        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        if (person?.WorkPermitItems == null)
            return [];

        if (!EnforceOfficerLinkValidity && lastCount == 1)
        {
            var current = PersonCurrentItems.GetCurrentWorkPermitItem(person);
            return current == null ? [] : [current];
        }

        IEnumerable<WorkPermitItem> query = person.WorkPermitItems.Where(w => w != null);
        if (EnforceOfficerLinkValidity)
            query = query.Where(w => CanLinkWorkPermitItem(w, asOf));

        return query
            .OrderByDescending(w => w.StartDate)
            .ThenByDescending(w => w.ID)
            .Take(lastCount)
            .ToList();
    }

    public static RejectionItem? ResolveRejectionItem(Person? person) =>
        PersonCurrentItems.GetCurrentRejectionItem(person);

    public static BorderZoneItem? ResolveBorderZoneItem(IObjectSpace objectSpace, Person? person, DateTime? asOf = null) =>
        ResolveBorderZoneItems(objectSpace, person, 1, asOf).FirstOrDefault();

    public static IReadOnlyList<BorderZoneItem> ResolveBorderZoneItems(
        IObjectSpace objectSpace,
        Person? person,
        int lastCount,
        DateTime? asOf = null)
    {
        lastCount = ApplicationProfilePersonLastCount.Clamp(lastCount);
        if (objectSpace == null || person == null || person.ID == Guid.Empty)
            return [];

        var rows = objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(b => b.Person != null && b.Person.ID == person.ID)
            .OrderByDescending(b => b.ID)
            .ToList();

        if (!EnforceOfficerLinkValidity)
        {
            return rows
                .Where(b => !b.IsCancelled)
                .Take(lastCount)
                .ToList();
        }

        return rows
            .Where(b => CanLinkBorderZoneItem(b, asOf))
            .Take(lastCount)
            .ToList();
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