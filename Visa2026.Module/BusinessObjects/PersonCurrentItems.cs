using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

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
                .Where(p => p != null && !p.IsDeleted && p.IssueDate.HasValue)
                .OrderByDescending(p => p.IssueDate!.Value.Date)
                .ThenByDescending(p => p.ID)
                .FirstOrDefault();

        public static Visa GetCurrentVisa(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            return person.Passports
                .Where(p => p != null && !p.IsDeleted)
                .SelectMany(p => p.Visas ?? Array.Empty<Visa>())
                .Where(v => VisaIsEffectiveOn(v, asOfDate))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();
        }

        public static Visa GetCurrentVisa(Passport passport, DateTime? asOf = null)
        {
            if (passport == null || passport.IsDeleted)
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
                .Where(e => e != null && !e.IsDeleted)
                .OrderByDescending(e => ParseGraduationYear(e.GraduationYear))
                .ThenByDescending(e => e.ID)
                .FirstOrDefault();

        public static MedicalRecord GetCurrentMedicalRecord(Person person) =>
            person?.MedicalRecords?
                .Where(m => m != null && !m.IsDeleted && m.IssueDate != default)
                .OrderByDescending(m => m.IssueDate.Date)
                .ThenByDescending(m => m.ID)
                .FirstOrDefault();

        public static AddressOfResidence GetCurrentAddressOfResidence(Person person) =>
            person?.AddressesOfResidence?
                .Where(a => a != null && !a.IsDeleted && a.StartDate.HasValue)
                .OrderByDescending(a => a.StartDate!.Value.Date)
                .ThenByDescending(a => a.ID)
                .FirstOrDefault();

        public static InvitationItem GetCurrentInvitationItem(Person person) =>
            person?.InvitationItems?
                .Where(i => i != null && !i.IsDeleted)
                .OrderByDescending(i => i.Invitation?.StartDate ?? default)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault();

        public static RejectionItem GetCurrentRejectionItem(Person person) =>
            person?.RejectionItems?
                .Where(i => i != null)
                .OrderByDescending(i => i.Rejection?.Date ?? default)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault();

        public static TravelHistory GetCurrentTravelHistory(Person person) =>
            person?.TravelHistories?
                .Where(t => t != null && !t.IsDeleted)
                .OrderByDescending(t => t.TravelDate.Date)
                .ThenByDescending(t => t.ID)
                .FirstOrDefault();

        public static WorkPermitItem GetCurrentWorkPermitItem(Person person) =>
            person?.WorkPermitItems?
                .Where(w => w != null && !w.IsDeleted && w.StartDate != default)
                .OrderByDescending(w => w.StartDate.Date)
                .ThenByDescending(w => w.ID)
                .FirstOrDefault();

        public static EmployeePositionHistory GetCurrentPositionHistory(Person person) =>
            GetCurrentOpenPeriodItem(
                person?.PositionHistory,
                h => h.StartDate,
                h => h.EndDate);

        public static EmployeeContract GetCurrentEmployeeContract(Person person) =>
            person?.EmployeeContracts?
                .Where(c => c != null && !c.IsDeleted && c.ContractStartDate != default)
                .OrderByDescending(c => c.ContractStartDate.Date)
                .ThenByDescending(c => c.ID)
                .FirstOrDefault();

        public static EmployeeSalary GetCurrentSalary(Person person) =>
            GetCurrentOpenPeriodItem(
                person?.Salaries,
                s => s.StartDate,
                s => s.EndDate);

        public static WorkDuty GetCurrentWorkDuty(Person person) =>
            person?.WorkDuties?
                .Where(w => w != null && !w.IsDeleted)
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
                "CurrentEmployeeContract" => GetCurrentEmployeeContract(person),
                "CurrentInvitationItem" => GetCurrentInvitationItem(person),
                "CurrentWorkPermitItem" => GetCurrentWorkPermitItem(person),
                _ => null
            };
        }

        public static Person ExtractPerson(object source) =>
            source switch
            {
                ApplicationItem ai => ai.Person,
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
            where TItem : class, ISoftDelete
        {
            if (items == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var live = items.Where(x => x != null && !x.IsDeleted).ToList();
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
            v != null && !v.IsDeleted && !v.IsCancelled && v.StartDate != default;

        private static bool VisaIsEffectiveOn(Visa v, DateTime asOfDate) =>
            VisaIsSelectable(v) && v.StartDate.Date <= asOfDate.Date;
    }
}
