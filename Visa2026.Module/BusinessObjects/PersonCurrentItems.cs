using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>
        /// Latest passport still in force: not cancelled, issued on or before the day,
        /// expiration empty or on or after that day. Newest issue date wins.
        /// </summary>
        public static Passport GetLastActivePassport(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return null;

            var asOfDate = (asOf ?? DateTime.Today).Date;
            return person.Passports
                .Where(passport => PassportIsInForceOn(passport, asOfDate))
                .OrderByDescending(passport => passport.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(passport => passport.ID)
                .FirstOrDefault();
        }

        /// <summary>Passport number, then expiration date, for the last in-force passport.</summary>
        public static IReadOnlyList<string> ListActivePassportLines(Person person, DateTime? asOf = null)
        {
            var passport = GetLastActivePassport(person, asOf);
            if (passport == null)
                return Array.Empty<string>();

            var expiration = passport.ExpirationDate is DateTime date && date != default
                ? date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                : string.Empty;
            return Lines(passport.PassportNumber, expiration);
        }

        public static string FormatEmployeeListActivePassport(Person person, DateTime? asOf = null) =>
            string.Join(", ", ListActivePassportLines(person, asOf));

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
                .Where(v => VisaIsSelectable(v))
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

        public static InvitationItem GetCurrentInvitationItem(Person person)
        {
            var asOf = DateTime.Today;
            var items = person?.InvitationItems?.Where(i => i != null).ToList();
            if (items == null || items.Count == 0)
                return null;

            var (usedIds, closedIds) = LoadInvitationLifecycleSets(person, items);
            return items
                .Where(i => InvitationIsInForceOn(i, asOf, usedIds, closedIds))
                .OrderByDescending(i => i.Invitation!.IssuedDate)
                .ThenByDescending(i => i.ID)
                .FirstOrDefault();
        }

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

        /// <summary>Education level, institution, and country, one record after another.</summary>
        public static string FormatEmployeeListEducationPlace(Person person) =>
            string.Join(" | ", ListEducationPlaces(person).Select(lines => string.Join(", ", lines)));

        /// <summary>Specialty for each education record.</summary>
        public static string FormatEmployeeListEducationProfession(Person person) =>
            string.Join(" | ", ListEducationProfessions(person).Select(lines => string.Join(", ", lines)));

        public static IReadOnlyList<IReadOnlyList<string>> ListEducationPlaces(Person person) =>
            OrderedEducations(person)
                .Select(education => Lines(
                    LookupLocalization.GetDisplayName(education.EducationLevel),
                    LookupLocalization.GetDisplayName(education.EducationInstitution),
                    LookupLocalization.GetDisplayName(education.EducationCountry)))
                .Where(lines => lines.Count > 0)
                .ToList();

        public static IReadOnlyList<IReadOnlyList<string>> ListEducationProfessions(Person person) =>
            OrderedEducations(person)
                .Select(education => Lines(LookupLocalization.GetDisplayName(education.Specialty)))
                .Where(lines => lines.Count > 0)
                .ToList();

        private static IEnumerable<Education> OrderedEducations(Person person) =>
            person?.Educations?
                .Where(education => education != null)
                .OrderByDescending(education => ParseGraduationYear(education.GraduationYear))
                .ThenByDescending(education => education.ID)
            ?? Enumerable.Empty<Education>();

        private static IReadOnlyList<string> Lines(params string[] values) =>
            values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();

        private static IReadOnlyList<PersonListCopyCell> ListShownEducations(Person person, bool places)
        {
            var cells = new List<PersonListCopyCell>();
            foreach (var education in OrderedEducations(person))
            {
                var lines = places
                    ? Lines(
                        LookupLocalization.GetDisplayName(education.EducationLevel),
                        LookupLocalization.GetDisplayName(education.EducationInstitution),
                        LookupLocalization.GetDisplayName(education.EducationCountry))
                    : Lines(LookupLocalization.GetDisplayName(education.Specialty));
                if (lines.Count == 0)
                    continue;

                var key = HasAnyFile(education.Documents) ? $"Education:{education.ID:N}" : string.Empty;
                var caption = string.Join(", ", Lines(
                    LookupLocalization.GetDisplayName(education.EducationInstitution),
                    LookupLocalization.GetDisplayName(education.Specialty)));
                cells.Add(new PersonListCopyCell
                {
                    Lines = lines,
                    RecordKey = key,
                    DisplayName = key.Length == 0
                        ? string.Empty
                        : PersonDocumentCopiesLocalization.FormatEducationRecord(caption),
                });
            }

            return cells;
        }

        private static string AddressCopyKey(AddressOfResidence address)
        {
            if (address == null)
                return string.Empty;

            if (HasAnyFile(address.Documents))
                return $"AddressOfResidence:{address.ID:N}";

            if (address.Type == ResidenceType.Lodging
                && address.Lodging != null
                && HasAnyFile(address.Lodging.Documents))
            {
                return $"AddressOfResidence:{address.ID:N}/Lodging:{address.Lodging.ID:N}";
            }

            return string.Empty;
        }

        private static IReadOnlyList<PersonListCopyCell> ListFileCopies<T>(
            IEnumerable<T> documents,
            Func<T, string> recordKey,
            Func<string, string> displayName)
            where T : DocumentBase
        {
            if (documents == null)
                return Array.Empty<PersonListCopyCell>();

            return documents
                .Where(document => document?.File != null && document.File.Size > 0 && !string.IsNullOrWhiteSpace(document.File.FileName))
                .OrderBy(document => document.File.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(document => document.ID)
                .Select(document =>
                {
                    var name = document.File.FileName.Trim();
                    return new PersonListCopyCell
                    {
                        Lines = new[] { name },
                        RecordKey = recordKey(document),
                        DisplayName = displayName(name),
                    };
                })
                .ToList();
        }

        private static bool HasAnyFile<T>(IEnumerable<T> documents)
            where T : DocumentBase =>
            documents?.Any(document => document?.File != null && document.File.Size > 0) == true;

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

        /// <summary>
        /// Invitation lines in force on <paramref name="asOf"/>: not cancelled, issued on or before that day,
        /// expiration empty or on or after that day. Numbers, latest issued date first.
        /// </summary>
        public static string FormatActiveInvitationNumbers(Person person, DateTime? asOf = null)
        {
            return string.Join(
                ", ",
                ListActiveInvitations(person, asOf)
                    .Select(item => item.Number)
                    .Where(number => !string.IsNullOrEmpty(number))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>In-force visas, latest start first. Each line is number, visa type, and start–expiration.</summary>
        public static IReadOnlyList<PersonListActiveDocument> ListActiveVisas(Person person, DateTime? asOf = null)
        {
            if (person?.Passports == null)
                return Array.Empty<PersonListActiveDocument>();

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var visas = person.Passports
                .Where(passport => passport?.Visas != null)
                .SelectMany(passport => passport.Visas.Where(visa => visa != null))
                .ToList();
            var closedIds = IssuedDocumentLifecycle.LoadClosedVisaIds(
                ResolveObjectSpace(person, visas),
                visas.Select(visa => visa.ID).Where(id => id != Guid.Empty).ToList());

            var rows = new List<(DateTime Start, DateTime Issued, Guid Id, PersonListActiveDocument Document)>();
            foreach (var passport in person.Passports.Where(passport => passport != null))
            {
                if (passport.Visas == null)
                    continue;

                foreach (var visa in passport.Visas.Where(visa => VisaIsInForceOn(visa, asOfDate, closedIds)))
                {
                    var number = visa.VisaNumber?.Trim() ?? string.Empty;
                    rows.Add((
                        visa.StartDate.Date,
                        visa.IssueDate.Date,
                        visa.ID,
                        new PersonListActiveDocument
                        {
                            Number = number,
                            Detail = LookupLocalization.GetDisplayName(visa.VisaType)?.Trim() ?? string.Empty,
                            Dates = FormatListDateRange(visa.StartDate, visa.ExpirationDate),
                            RecordKey = $"Passport:{passport.ID:N}/Visa:{visa.ID:N}",
                            DisplayName = PersonDocumentCopiesLocalization.FormatVisaRecord(number),
                        }));
                }
            }

            return rows
                .OrderByDescending(row => row.Start)
                .ThenByDescending(row => row.Issued)
                .ThenByDescending(row => row.Id)
                .Select(row => row.Document)
                .ToList();
        }

        /// <summary>In-force work-permit lines, latest start first. Each line is number and start–expiration.</summary>
        public static IReadOnlyList<PersonListActiveDocument> ListActiveWorkPermits(Person person, DateTime? asOf = null)
        {
            if (person?.WorkPermitItems == null)
                return Array.Empty<PersonListActiveDocument>();

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var items = person.WorkPermitItems.Where(item => item != null).ToList();
            var closedIds = IssuedDocumentLifecycle.LoadClosedWorkPermitItemIds(
                ResolveObjectSpace(person, items),
                items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());

            return items
                .Where(item => WorkPermitIsInForceOn(item, asOfDate, closedIds))
                .OrderByDescending(item => item.StartDate.Date)
                .ThenByDescending(item => item.ID)
                .Select(item =>
                {
                    var number = item.WorkPermitNumber?.Trim();
                    if (string.IsNullOrEmpty(number))
                        number = item.WorkPermit?.WorkPermitNumber?.Trim() ?? string.Empty;
                    return new PersonListActiveDocument
                    {
                        Number = number,
                        Dates = FormatListDateRange(item.StartDate, item.ExpirationDate),
                        RecordKey = $"WorkPermitItem:{item.ID:N}",
                        DisplayName = PersonDocumentCopiesLocalization.FormatWorkPermitRecord(number),
                    };
                })
                .ToList();
        }

        /// <summary>
        /// In-force invitation lines, latest issued date first. Each line is number and issued–expiration.
        /// Excludes cancelled, changed, used, and expired (same as dossier Valid status).
        /// </summary>
        public static IReadOnlyList<PersonListActiveDocument> ListActiveInvitations(Person person, DateTime? asOf = null)
        {
            if (person?.InvitationItems == null)
                return Array.Empty<PersonListActiveDocument>();

            var asOfDate = (asOf ?? DateTime.Today).Date;
            var items = person.InvitationItems.Where(item => item != null).ToList();
            var (usedIds, closedIds) = LoadInvitationLifecycleSets(person, items);

            return items
                .Where(item => InvitationIsInForceOn(item, asOfDate, usedIds, closedIds))
                .OrderByDescending(item => item.Invitation!.IssuedDate.Date)
                .ThenByDescending(item => item.ID)
                .Select(item =>
                {
                    var invitation = item.Invitation!;
                    var number = invitation.InvitationNumber?.Trim() ?? string.Empty;
                    return new PersonListActiveDocument
                    {
                        Number = number,
                        Dates = FormatListDateRange(invitation.IssuedDate, invitation.ExpirationDate),
                        RecordKey = $"InvitationItem:{item.ID:N}",
                        DisplayName = PersonDocumentCopiesLocalization.FormatInvitationRecord(number),
                    };
                })
                .ToList();
        }

        /// <summary>Current residence as report text: region, city, and street.</summary>
        public static string FormatEmployeeListResidenceAddress(Person person) =>
            AddressOfResidenceReportText.CityAndStreet(GetCurrentAddressOfResidence(person));

        /// <summary>The passport already shown on the person list. The key is set only when that passport has a scan.</summary>
        public static PersonListCopyCell ShownPassportCopy(Person person, DateTime? asOf = null)
        {
            var passport = GetLastActivePassport(person, asOf);
            var lines = ListActivePassportLines(person, asOf);
            if (passport == null || lines.Count == 0)
                return PersonListCopyCell.Empty;

            var key = HasAnyFile(passport.Documents) ? $"Passport:{passport.ID:N}" : string.Empty;
            return new PersonListCopyCell
            {
                Lines = lines,
                RecordKey = key,
                DisplayName = key.Length == 0
                    ? string.Empty
                    : PersonDocumentCopiesLocalization.FormatPassportRecord(passport.PassportNumber),
            };
        }

        /// <summary>Education rows already shown, in the same order as the place column.</summary>
        public static IReadOnlyList<PersonListCopyCell> ListShownEducationPlaces(Person person) =>
            ListShownEducations(person, places: true);

        /// <summary>Specialty rows for the same education records as <see cref="ListShownEducationPlaces"/>.</summary>
        public static IReadOnlyList<PersonListCopyCell> ListShownEducationProfessions(Person person) =>
            ListShownEducations(person, places: false);

        /// <summary>The address already shown. The key is that address's scan, or the lodging scan when the stay is a lodging.</summary>
        public static PersonListCopyCell ShownAddressCopy(Person person)
        {
            var text = FormatEmployeeListResidenceAddress(person);
            if (string.IsNullOrWhiteSpace(text))
                return PersonListCopyCell.Empty;

            var address = GetCurrentAddressOfResidence(person);
            var key = AddressCopyKey(address);
            var display = key.Contains("/Lodging:", StringComparison.Ordinal)
                ? PersonDocumentCopiesLocalization.FormatLodgingRecord(text)
                : PersonDocumentCopiesLocalization.FormatAddressRecord(text);
            return new PersonListCopyCell
            {
                Lines = new[] { text.Trim() },
                RecordKey = key,
                DisplayName = key.Length == 0 ? string.Empty : display,
            };
        }

        /// <summary>Employee CV and other person files. Each line is one file.</summary>
        public static IReadOnlyList<PersonListCopyCell> ListCvCopies(Person person) =>
            ListFileCopies(person?.Documents, static document => $"PersonDocument:{document.ID:N}", PersonDocumentCopiesLocalization.FormatPersonDocumentRecord);

        /// <summary>Family-member proof files. Each line is one file.</summary>
        public static IReadOnlyList<PersonListCopyCell> ListFamilyProofCopies(Person person) =>
            ListFileCopies(person?.FamilyRelationDocuments, static document => $"FamilyRelationDocument:{document.ID:N}", PersonDocumentCopiesLocalization.FormatFamilyRelationDocumentRecord);

        public static string FormatCvCopies(Person person) =>
            string.Join(" | ", ListCvCopies(person).Select(cell => cell.Lines[0]));

        public static string FormatFamilyProofCopies(Person person) =>
            string.Join(" | ", ListFamilyProofCopies(person).Select(cell => cell.Lines[0]));

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

        private static bool PassportIsInForceOn(Passport passport, DateTime asOfDate) =>
            passport != null
            && !passport.IsCancelled
            && (!passport.IssueDate.HasValue || passport.IssueDate.Value.Date <= asOfDate)
            && (!passport.ExpirationDate.HasValue || passport.ExpirationDate.Value.Date >= asOfDate);

        private static (HashSet<Guid> UsedIds, HashSet<Guid> ClosedIds) LoadInvitationLifecycleSets(
            Person person,
            IReadOnlyList<InvitationItem> items)
        {
            var objectSpace = ResolveObjectSpace(person, items);
            var ids = items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList();
            return (
                IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, ids),
                IssuedDocumentLifecycle.LoadClosedInvitationItemIds(objectSpace, ids));
        }

        private static DevExpress.ExpressApp.IObjectSpace ResolveObjectSpace(
            Person person,
            IEnumerable<BaseObject> related)
        {
            var objectSpace = ObjectSpaceHelper.Get(person);
            if (objectSpace != null)
                return objectSpace;

            foreach (var entity in related)
            {
                objectSpace = ObjectSpaceHelper.Get(entity);
                if (objectSpace != null)
                    return objectSpace;
            }

            return null;
        }

        private static bool VisaIsSelectable(Visa v, IReadOnlySet<Guid> closedIds = null) =>
            v != null
            && v.StartDate != default
            && !((closedIds != null && v.ID != Guid.Empty && closedIds.Contains(v.ID))
                || IssuedDocumentLifecycle.IsCancelled(v)
                || IssuedDocumentLifecycle.IsChanged(v));

        private static bool VisaIsEffectiveOn(Visa v, DateTime asOfDate) =>
            VisaIsSelectable(v) && v.StartDate.Date <= asOfDate.Date;

        private static bool VisaIsInForceOn(Visa v, DateTime asOfDate, IReadOnlySet<Guid> closedIds = null) =>
            VisaIsSelectable(v, closedIds)
            && v.StartDate.Date <= asOfDate
            && (!v.ExpirationDate.HasValue || v.ExpirationDate.Value.Date >= asOfDate);

        private static bool WorkPermitIsInForceOn(
            WorkPermitItem w,
            DateTime asOfDate,
            IReadOnlySet<Guid> closedIds = null) =>
            w != null
            && !((closedIds != null && w.ID != Guid.Empty && closedIds.Contains(w.ID))
                || IssuedDocumentLifecycle.IsCancelled(w)
                || IssuedDocumentLifecycle.IsChanged(w))
            && w.StartDate != default
            && w.StartDate.Date <= asOfDate
            && w.ExpirationDate != default
            && w.ExpirationDate.Date >= asOfDate;

        /// <summary>
        /// Active for Person ListView / current-item: valid dates, not cancelled, not changed, not used.
        /// <paramref name="usedIds"/> / <paramref name="closedIds"/> cover ListView when skip-navs are unloaded.
        /// </summary>
        private static bool InvitationIsInForceOn(
            InvitationItem item,
            DateTime asOfDate,
            IReadOnlySet<Guid> usedIds = null,
            IReadOnlySet<Guid> closedIds = null)
        {
            if (item?.Invitation == null)
                return false;

            if ((closedIds != null && item.ID != Guid.Empty && closedIds.Contains(item.ID))
                || IssuedDocumentLifecycle.IsCancelled(item)
                || IssuedDocumentLifecycle.IsChanged(item))
                return false;

            if (usedIds != null)
            {
                if (item.ID != Guid.Empty && usedIds.Contains(item.ID))
                    return false;
                if (item.IssuedVisa != null)
                    return false;
            }
            else if (IssuedDocumentLifecycle.IsUsed(item))
            {
                return false;
            }

            var issued = item.Invitation.IssuedDate;
            if (issued == default || issued.Date > asOfDate)
                return false;

            var expiration = item.Invitation.ExpirationDate;
            return !expiration.HasValue || expiration.Value.Date >= asOfDate;
        }

        private static string FormatListDateRange(DateTime start, DateTime? end)
        {
            var startText = start == default
                ? string.Empty
                : start.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            var endText = end is DateTime value && value != default
                ? value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                : string.Empty;
            if (startText.Length == 0)
                return endText;
            if (endText.Length == 0)
                return startText;
            return startText + " – " + endText;
        }
    }
}
