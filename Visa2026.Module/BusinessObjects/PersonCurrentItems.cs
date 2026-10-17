using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Resolves a person's "current" child records from collections by date/effectivity rules,
    /// replacing persisted <c>Person.Current*</c> FK columns.
    /// </summary>
    public static class PersonCurrentItems
    {
        public static Passport GetCurrentPassport(Person person) =>
            person?.Passports?
                .Where(p => p != null && p.IssueDate.HasValue)
                .OrderByDescending(p => p.IssueDate!.Value.Date)
                .ThenByDescending(p => p.ID)
                .FirstOrDefault();

        public static Visa GetCurrentVisa(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            return person.Passports
                .Where(p => p != null)
                .SelectMany(p => p.Visas ?? Array.Empty<Visa>())
                .Where(v => VisaIsEffectiveOn(v, asOfDate))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();
        }

        public static Visa GetCurrentVisa(Passport passport, DateTime? asOf = null)
        {
            if (passport == null )
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            return passport.Visas?
                .Where(VisaIsSelectable)
                .Where(v => VisaIsEffectiveOn(v, asOfDate))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();
        }

        public static Education GetCurrentEducation(Person person) =>
            person?.Educations?
                .Where(e => e != null)
                .OrderByDescending(e => ParseGraduationYear(e.GraduationYear))
                .ThenByDescending(e => e.ID)
                .FirstOrDefault();

        public static MedicalRecord GetCurrentMedicalRecord(Person person) =>
            person?.MedicalRecords?
                .Where(m => m != null && m.IssueDate != default)
                .OrderByDescending(m => m.IssueDate.Date)
                .ThenByDescending(m => m.ID)
                .FirstOrDefault();

        /// <summary>
        /// Prefer the address still valid on <paramref name="asOf"/> (no expiration or expiration on/after that date);
        /// among those, latest expiration then newest ID. If none are valid, fall back to latest expiration.
        /// </summary>
        public static AddressOfResidence GetCurrentAddressOfResidence(Person person, DateTime? asOf = null)
        {
            if (person?.AddressesOfResidence == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var live = person.AddressesOfResidence
                .Where(a => a != null)
                .ToList();
            if (live.Count == 0)
                return null;

            var stillValid = live
                .Where(a => !a.ExpirationDate.HasValue || a.ExpirationDate.Value.Date >= asOfDate)
                .ToList();

            if (stillValid.Count > 0)
            {
                return stillValid
                    .OrderByDescending(a => a.ExpirationDate?.Date ?? DateTime.MaxValue)
                    .ThenByDescending(a => a.ID)
                    .FirstOrDefault();
            }

            return live
                .OrderByDescending(a => a.ExpirationDate?.Date ?? DateTime.MinValue)
                .ThenByDescending(a => a.ID)
                .FirstOrDefault();
        }

        public static InvitationItem GetCurrentInvitationItem(Person person) =>
            person?.InvitationItems?
                .Where(i => i != null)
                .OrderByDescending(i => i.Invitation?.IssuedDate ?? default)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault();

        public static RejectionItem GetCurrentRejectionItem(Person person) =>
            person?.RejectionItems?
                .Where(i => i != null)
                .OrderByDescending(i => i.Rejection?.Date ?? default)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault();

        public static WorkPermitItem GetCurrentWorkPermitItem(Person person) =>
            person?.WorkPermitItems?
                .Where(w => w != null && w.StartDate != default)
                .OrderByDescending(w => w.StartDate.Date)
                .ThenByDescending(w => w.ID)
                .FirstOrDefault();

        public static EmployeePositionHistory GetCurrentPositionHistory(Person person) =>
            GetCurrentOpenPeriodItem(
                person?.PositionHistory,
                h => h.StartDate,
                h => h.EndDate);

        /// <summary>Visa-report title on <see cref="GetCurrentPositionHistory"/>.</summary>
        public static string FormatEmployeeListPositionName(Person person) =>
            LookupLocalization.GetDisplayName(GetCurrentPositionHistory(person)?.Position);

        /// <summary>Company title on <see cref="GetCurrentPositionHistory"/>.</summary>
        public static string FormatEmployeeListActualPositionName(Person person) =>
            GetCurrentPositionHistory(person)?.ActualPosition?.Name?.Trim() ?? string.Empty;

        /// <summary>Department on <see cref="GetCurrentPositionHistory"/>.</summary>
        public static string FormatEmployeeListDepartmentName(Person person) =>
            LookupLocalization.GetDisplayName(GetCurrentPositionHistory(person)?.Department);

        /// <summary>
        /// Visas in force on <paramref name="asOf"/>: not cancelled, start on or before that day,
        /// expiration empty or on or after that day. Numbers, latest start first.
        /// </summary>
        public static string FormatActiveVisaNumbers(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return string.Empty;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var numbers = person.Passports
                .Where(p => p != null)
                .SelectMany(p => p.Visas ?? Array.Empty<Visa>())
                .Where(v => VisaIsInForceOn(v, asOfDate))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .ThenByDescending(v => v.ID)
                .Select(v => v.VisaNumber?.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", numbers);
        }

        /// <summary>
        /// Work-permit lines in force on <paramref name="asOf"/>: not cancelled, start on or before that day,
        /// expiration on or after that day. Numbers, latest start first.
        /// </summary>
        public static string FormatActiveWorkPermitNumbers(Person person, DateTime? asOf = null)
        {
            if (person?.WorkPermitItems == null)
                return string.Empty;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var numbers = person.WorkPermitItems
                .Where(w => WorkPermitIsInForceOn(w, asOfDate))
                .OrderByDescending(w => w.StartDate.Date)
                .ThenByDescending(w => w.ID)
                .Select(w => w.WorkPermitNumber?.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", numbers);
        }

        /// <summary>Current residence as report text: region, city, and street.</summary>
        public static string FormatEmployeeListResidenceAddress(Person person) =>
            AddressOfResidenceReportText.CityAndStreet(GetCurrentAddressOfResidence(person));

        public static EmployeeSalary GetCurrentSalary(Person person) =>
            GetCurrentOpenPeriodItem(
                person?.Salaries,
                s => s.StartDate,
                s => s.EndDate);

        public static WorkDuty GetCurrentWorkDuty(Person person) =>
            person?.WorkDuties?
                .Where(w => w != null)
                .OrderByDescending(w => w.ID)
                .FirstOrDefault();

        public static object ResolveFromSource(object source, string propertyName)
        {
            var person = ExtractPerson(source);
            if (person == null)
                return null;

            return propertyName switch
            {
                "CurrentPassport" => GetCurrentPassport(person),
                "CurrentVisa" => GetCurrentVisa(person),
                "CurrentAddressOfResidence" => GetCurrentAddressOfResidence(person),
                "CurrentPositionHistory" => GetCurrentPositionHistory(person),
                "CurrentMedicalRecord" => GetCurrentMedicalRecord(person),
                "CurrentEducation" => GetCurrentEducation(person),
                "CurrentInvitationItem" => GetCurrentInvitationItem(person),
                "CurrentRejectionItem" => GetCurrentRejectionItem(person),
                "CurrentWorkPermitItem" => GetCurrentWorkPermitItem(person),
                "CurrentSalary" => GetCurrentSalary(person),
                _ => null
            };
        }

        public static Person ExtractPerson(object source) =>
            source switch
            {
                ApplicationRosterMergeLine ai => ai.Person,
                WorkPermitItem wpi => wpi.Person,
                Passport passport => passport.Person,
                Visa visa => visa.Passport?.Person,
                _ => null
            };

        private static int ParseGraduationYear(string year) =>
            int.TryParse(year?.Trim(), out var parsed) ? parsed : int.MinValue;

        private static TItem GetCurrentOpenPeriodItem<TItem>(
            IEnumerable<TItem> items,
            Func<TItem, DateTime> getStartDate,
            Func<TItem, DateTime?> getEndDate,
            DateTime? asOf = null)
            where TItem : BaseObject
        {
            if (items == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var live = items.Where(x => x != null).ToList();
            if (live.Count == 0)
                return null;

            var open = live
                .Where(x =>
                {
                    var end = getEndDate(x);
                    return !end.HasValue || end.Value.Date >= asOfDate;
                })
                .ToList();

            if (open.Count > 0)
            {
                return open
                    .OrderByDescending(getStartDate)
                    .ThenByDescending(x => ((BaseObject)(object)x).ID)
                    .FirstOrDefault();
            }

            return live
                .OrderByDescending(getStartDate)
                .ThenByDescending(x => ((BaseObject)(object)x).ID)
                .FirstOrDefault();
        }

        private static bool VisaIsSelectable(Visa v) =>
            v != null && !IssuedDocumentLifecycle.IsCancelled(v) && v.StartDate != default;

        private static bool VisaIsEffectiveOn(Visa v, DateTime asOfDate) =>
            VisaIsSelectable(v) && v.StartDate.Date <= asOfDate.Date;

        private static bool VisaIsInForceOn(Visa v, DateTime asOfDate) =>
            VisaIsSelectable(v)
            && v.StartDate.Date <= asOfDate
            && (!v.ExpirationDate.HasValue || v.ExpirationDate.Value.Date >= asOfDate);

        private static bool WorkPermitIsInForceOn(WorkPermitItem w, DateTime asOfDate) =>
            w != null
            && !IssuedDocumentLifecycle.IsCancelled(w)
            && w.StartDate != default
            && w.StartDate.Date <= asOfDate
            && w.ExpirationDate != default
            && w.ExpirationDate.Date >= asOfDate;
    }
}
