using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Resolves a person's "current" child records from collections and <see cref="ICurrentPersonItem.IsActive"/>,
    /// replacing persisted <c>Person.Current*</c> FK columns.
    /// </summary>
    public static class PersonCurrentItems
    {
        public static Passport GetCurrentPassport(Person person) =>
            GetActiveItem(person?.Passports);

        public static Visa GetCurrentVisa(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            return person.Passports
                .Where(p => p != null && !p.IsDeleted)
                .SelectMany(p => p.Visas ?? Array.Empty<Visa>())
                .Where(v => v != null && !v.IsDeleted && !v.IsCancelled && v.StartDate != default && v.StartDate.Date <= asOfDate)
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();
        }

        public static Education GetCurrentEducation(Person person) =>
            GetActiveItem(person?.Educations);

        public static MedicalRecord GetCurrentMedicalRecord(Person person) =>
            GetActiveItem(person?.MedicalRecords);

        public static AddressOfResidence GetCurrentAddressOfResidence(Person person) =>
            GetActiveItem(person?.AddressesOfResidence);

        public static InvitationItem GetCurrentInvitationItem(Person person) =>
            GetActiveItem(person?.InvitationItems);

        public static RejectionItem GetCurrentRejectionItem(Person person) =>
            GetActiveItem(person?.RejectionItems);

        public static TravelHistory GetCurrentTravelHistory(Person person) =>
            GetActiveItem(person?.TravelHistories);

        public static WorkPermitItem GetCurrentWorkPermitItem(Person person) =>
            GetActiveItem(person?.WorkPermitItems);

        public static EmployeePositionHistory GetCurrentPositionHistory(Person person) =>
            GetActiveItem(person?.PositionHistory);

        public static EmployeeContract GetCurrentEmployeeContract(Person person) =>
            GetActiveItem(person?.EmployeeContracts);

        public static EmployeeSalary GetCurrentSalary(Person person) =>
            GetActiveItem(person?.Salaries);

        public static WorkDuty GetCurrentWorkDuty(Person person) =>
            GetActiveItem(person?.WorkDuties);

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

        private static TItem GetActiveItem<TItem>(IEnumerable<TItem> items)
            where TItem : class, ICurrentPersonItem
        {
            if (items == null)
                return null;

            return items.FirstOrDefault(x =>
                x != null &&
                x.IsActive &&
                (x is not ISoftDelete sd || !sd.IsDeleted));
        }
    }
}
