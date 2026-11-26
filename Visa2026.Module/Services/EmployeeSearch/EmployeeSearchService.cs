using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services.EmployeeSearch;

public static partial class EmployeeSearchService
{
    public const int ResultLimit = 200;

    public static EmployeeSearchOptions LoadOptions(IObjectSpace objectSpace)
    {
        return new EmployeeSearchOptions
        {
            Genders = Load<Gender>(objectSpace),
            Countries = Load<Country>(objectSpace),
            Departments = Load<Department>(objectSpace),
            Positions = Load<Position>(objectSpace),
            Contracts = Load<ProjectContract>(objectSpace),
            Specialties = Load<Specialty>(objectSpace),
            EducationLevels = Load<EducationLevel>(objectSpace),
            Cities = Load<City>(objectSpace),
            MaritalStatuses = Load<MaritalStatus>(objectSpace),
            Relationships = Load<Relationship>(objectSpace),
            Institutions = Load<EducationInstitution>(objectSpace),
            PassportTypes = Load<PassportType>(objectSpace),
            VisaTypes = Load<VisaType>(objectSpace),
            VisaCategories = Load<VisaCategory>(objectSpace),
            VisaIssuedPlaces = Load<VisaIssuedPlace>(objectSpace),
            VisaPeriods = Load<VisaPeriod>(objectSpace),
            Subcontractors = Load<Subcontractor>(objectSpace),
            Regions = Load<Region>(objectSpace),
            WorkLocations = Load<WorkPermittedLocationName>(objectSpace),
            ActualPositions = LoadCaption(objectSpace.GetObjectsQuery<ActualPosition>(), row => row.Name),
            Lodgings = LoadCaption(objectSpace.GetObjectsQuery<Lodging>(), row => row.FullAddress),
            Hotels = LoadCaption(objectSpace.GetObjectsQuery<Hotel>(), row => row.Name),
            Hospitals = LoadCaption(objectSpace.GetObjectsQuery<Hospital>(), row => row.Name),
            OtherSites = LoadCaption(objectSpace.GetObjectsQuery<OtherSite>(), row => row.FullAddress),
        };
    }

    public static EmployeeSearchResult Search(IObjectSpace objectSpace, EmployeeSearchCriteria criteria)
    {
        criteria ??= new EmployeeSearchCriteria();
        var today = DateTime.Today;
        var ageOn = criteria.AgeAsOfYear is int asOf && asOf is >= 1900 and <= 2100
            ? new DateTime(asOf, 12, 31)
            : today;
        Normalize(criteria);

        var query = objectSpace.GetObjectsQuery<Person>()
            .Where(person => person.PersonRole == PersonRecordRole.Employee);

        if (criteria.ArchivedOnly)
            query = query.Where(person => person.IsArchived);
        else if (!criteria.IncludeArchived)
            query = query.Where(person => !person.IsArchived);

        if (criteria.GenderIds.Count > 0)
        {
            var genderIds = criteria.GenderIds;
            query = query.Where(person => person.Gender != null && genderIds.Contains(person.Gender.ID));
        }

        if (criteria.AgeFrom is int ageFrom)
        {
            var latestBirth = ageOn.AddYears(-ageFrom);
            query = query.Where(person => person.DateOfBirth > new DateTime(1900, 1, 1) && person.DateOfBirth <= latestBirth);
        }

        if (criteria.AgeTo is int ageTo)
        {
            var earliestBirth = ageOn.AddYears(-(ageTo + 1)).AddDays(1);
            query = query.Where(person => person.DateOfBirth >= earliestBirth);
        }

        if (criteria.NationalityIds is { Count: > 0 } nationalityIds)
            query = query.Where(person => person.Nationality != null && nationalityIds.Contains(person.Nationality.ID));

        if (criteria.DepartmentIds.Count > 0 || criteria.PositionIds.Count > 0 || criteria.ActualPositionIds.Count > 0
            || criteria.PositionYearFrom != null || criteria.PositionYearTo != null
            || criteria.PositionLeft || !criteria.PositionAnyTime)
        {
            var departmentIds = ActiveOrDummy(criteria.DepartmentIds, out var filterDepartment);
            var positionIds = ActiveOrDummy(criteria.PositionIds, out var filterPosition);
            var actualIds = ActiveOrDummy(criteria.ActualPositionIds, out var filterActual);
            var left = criteria.PositionLeft;
            var currentOnly = !criteria.PositionAnyTime && !left;
            var leftYear = criteria.PositionLeftYear ?? 0;
            var filterLeftYear = criteria.PositionLeftYear != null;
            var hasYears = criteria.PositionYearFrom != null || criteria.PositionYearTo != null;
            var rangeStart = hasYears
                ? new DateTime(criteria.PositionYearFrom ?? criteria.PositionYearTo!.Value, 1, 1)
                : today;
            var rangeEnd = hasYears
                ? new DateTime(criteria.PositionYearTo ?? criteria.PositionYearFrom!.Value, 12, 31)
                : today;
            query = query.Where(person => person.PositionHistory.Any(history =>
                (!filterDepartment || (history.Department != null && departmentIds.Contains(history.Department.ID)))
                && (!filterPosition || (history.Position != null && positionIds.Contains(history.Position.ID)))
                && (!filterActual || (history.ActualPosition != null && actualIds.Contains(history.ActualPosition.ID)))
                && (!hasYears || (history.StartDate <= rangeEnd && (history.EndDate == null || history.EndDate >= rangeStart)))
                && (!currentOnly || (history.StartDate <= today && (history.EndDate == null || history.EndDate >= today)))
                && (!left || (history.EndDate != null && (!filterLeftYear || history.EndDate.Value.Year == leftYear)))));
        }

        if (criteria.NeverHadVisa == true)
            query = query.Where(person => !person.Passports.Any(passport => passport.Visas.Any()));

        var visaNumber = criteria.VisaNumber?.Trim().ToLower() ?? string.Empty;
        var registration = criteria.RegistrationState?.Trim().ToLower() ?? string.Empty;
        var visaBorder = criteria.VisaBorderZone?.Trim().ToLower() ?? string.Empty;
        var visaTypes = ActiveOrDummy(criteria.VisaTypeIds, out var filterVisaType);
        var visaCategories = ActiveOrDummy(criteria.VisaCategoryIds, out var filterVisaCategory);
        var visaPlaces = ActiveOrDummy(criteria.VisaIssuedPlaceIds, out var filterVisaPlace);
        var hasVisaYears = criteria.VisaYearFrom.HasValue || criteria.VisaYearTo.HasValue;
        var visaYearStart = hasVisaYears
            ? new DateTime(criteria.VisaYearFrom ?? criteria.VisaYearTo!.Value, 1, 1)
            : today;
        var visaYearEnd = hasVisaYears
            ? new DateTime(criteria.VisaYearTo ?? criteria.VisaYearFrom!.Value, 12, 31)
            : today;
        var hasVisaDates = criteria.VisaFrom.HasValue || criteria.VisaTo.HasValue;
        var visaDateStart = criteria.VisaFrom ?? criteria.VisaTo ?? today;
        var visaDateEnd = criteria.VisaTo ?? criteria.VisaFrom ?? today;
        var hasVisaOn = criteria.VisaOn.HasValue;
        var visaOn = criteria.VisaOn ?? today;
        var stillValid = criteria.StillValidToday;
        var expiringDays = criteria.ExpiringWithinDays ?? 0;
        var filterExpiring = criteria.ExpiringWithinDays is > 0;
        var expiringUntil = today.AddDays(expiringDays);
        var visaExpired = criteria.VisaExpired == true;
        var visaNotExpired = criteria.VisaExpired == false;
        var expiredYear = criteria.VisaExpiredYear ?? 0;
        var filterExpiredYear = criteria.VisaExpiredYear != null;
        var filterVisaNumber = visaNumber.Length > 0;
        var filterRegistration = registration.Length > 0;
        var filterVisaBorder = visaBorder.Length > 0;
        var wantExtended = criteria.VisaExtended == true;
        var filterExtended = criteria.VisaExtended != null;
        var wantExtensionRequired = criteria.ExtensionRequired == true;
        var filterExtensionRequired = criteria.ExtensionRequired != null;
        var filterVisa = hasVisaYears || hasVisaDates || hasVisaOn || stillValid || filterExpiring || visaExpired || visaNotExpired
            || filterVisaNumber || filterVisaType || filterVisaCategory || filterVisaPlace || filterExtended
            || filterExtensionRequired || filterRegistration || filterVisaBorder;
        if (filterVisa && criteria.NeverHadVisa != true)
        {
            query = query.Where(person => person.Passports.Any(passport => passport.Visas.Any(visa =>
                (!hasVisaYears || (visa.StartDate <= visaYearEnd && (visa.ExpirationDate == null || visa.ExpirationDate >= visaYearStart)))
                && (!hasVisaDates || (visa.StartDate <= visaDateEnd && (visa.ExpirationDate == null || visa.ExpirationDate >= visaDateStart)))
                && (!hasVisaOn || (visa.StartDate <= visaOn && (visa.ExpirationDate == null || visa.ExpirationDate >= visaOn)))
                && (!stillValid || (visa.StartDate <= today && (visa.ExpirationDate == null || visa.ExpirationDate >= today)))
                && (!filterExpiring || (visa.ExpirationDate != null && visa.ExpirationDate >= today && visa.ExpirationDate <= expiringUntil))
                && (!visaExpired || (visa.ExpirationDate != null && visa.ExpirationDate < today && (!filterExpiredYear || visa.ExpirationDate.Value.Year == expiredYear)))
                && (!visaNotExpired || visa.ExpirationDate == null || visa.ExpirationDate >= today)
                && (!filterVisaNumber || (visa.VisaNumber != null && visa.VisaNumber.ToLower().Contains(visaNumber)))
                && (!filterVisaType || (visa.VisaType != null && visaTypes.Contains(visa.VisaType.ID)))
                && (!filterVisaCategory || (visa.VisaCategory != null && visaCategories.Contains(visa.VisaCategory.ID)))
                && (!filterVisaPlace || (visa.VisaIssuedPlace != null && visaPlaces.Contains(visa.VisaIssuedPlace.ID)))
                && (!filterExtended || visa.IsExtended == wantExtended)
                && (!filterExtensionRequired || visa.ExtensionRequired == wantExtensionRequired)
                && (!filterRegistration || (visa.RegistrationState != null && visa.RegistrationState.ToLower().Contains(registration)))
                && (!filterVisaBorder || (visa.BorderZoneLocation != null && visa.BorderZoneLocation.ToLower().Contains(visaBorder))))));
        }

        if (criteria.ProjectContractIds.Count > 0)
        {
            var contractIds = criteria.ProjectContractIds;
            query = query.Where(person => person.ApplicationProfileInstances.Any(instance =>
                instance.ProjectContract != null && contractIds.Contains(instance.ProjectContract.ID)));
        }

        if (criteria.SpecialtyIds.Count > 0
            || criteria.EducationCountryIds.Count > 0
            || criteria.EducationLevelIds.Count > 0
            || criteria.InstitutionIds.Count > 0
            || criteria.GraduationYearFrom is int
            || criteria.GraduationYearTo is int
            || criteria.DiplomaOnFile != null
            || criteria.EducatedOutsideCitizenship != null)
        {
            var specialtyIds = ActiveOrDummy(criteria.SpecialtyIds, out var filterSpecialty);
            var countryIds = ActiveOrDummy(criteria.EducationCountryIds, out var filterCountry);
            var levelIds = ActiveOrDummy(criteria.EducationLevelIds, out var filterLevel);
            var institutionIds = ActiveOrDummy(criteria.InstitutionIds, out var filterInstitution);
            var yearFrom = criteria.GraduationYearFrom?.ToString() ?? "0000";
            var yearTo = criteria.GraduationYearTo?.ToString() ?? "0000";
            var filterYearFrom = criteria.GraduationYearFrom != null;
            var filterYearTo = criteria.GraduationYearTo != null;
            var wantDiploma = criteria.DiplomaOnFile == true;
            var diplomaNo = criteria.DiplomaOnFile == false;
            var wantOutside = criteria.EducatedOutsideCitizenship == true;
            var outsideNo = criteria.EducatedOutsideCitizenship == false;
            query = query.Where(person => person.Educations.Any(education =>
                (!filterSpecialty || (education.Specialty != null && specialtyIds.Contains(education.Specialty.ID)))
                && (!filterCountry || (education.EducationCountry != null && countryIds.Contains(education.EducationCountry.ID)))
                && (!filterLevel || (education.EducationLevel != null && levelIds.Contains(education.EducationLevel.ID)))
                && (!filterInstitution || (education.EducationInstitution != null && institutionIds.Contains(education.EducationInstitution.ID)))
                && (!filterYearFrom || (education.GraduationYear != null && education.GraduationYear.CompareTo(yearFrom) >= 0))
                && (!filterYearTo || (education.GraduationYear != null && education.GraduationYear.CompareTo(yearTo) <= 0))
                && (!wantDiploma || education.Documents.Any() || education.Images.Any())
                && (!diplomaNo || (!education.Documents.Any() && !education.Images.Any()))
                && (!wantOutside || (person.Nationality != null && education.EducationCountry != null && education.EducationCountry.ID != person.Nationality.ID))
                && (!outsideNo || person.Nationality == null || education.EducationCountry == null || education.EducationCountry.ID == person.Nationality.ID)));
        }

        if (criteria.HasEducation == true)
            query = query.Where(person => person.Educations.Any());
        else if (criteria.HasEducation == false)
            query = query.Where(person => !person.Educations.Any());

        var invitationNeedle = criteria.InvitationNumber?.Trim();
        if (!string.IsNullOrEmpty(invitationNeedle))
        {
            var raw = invitationNeedle.ToLower();
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.Invitation != null
                && item.Invitation.InvitationNumber != null
                && item.Invitation.InvitationNumber.ToLower().Contains(raw)));
        }

        var permitNeedle = criteria.WorkPermitNumber?.Trim();
        if (!string.IsNullOrEmpty(permitNeedle))
        {
            var raw = permitNeedle.ToLower();
            query = query.Where(person => person.WorkPermitItems.Any(item =>
                item.WorkPermitNumber != null && item.WorkPermitNumber.ToLower().Contains(raw)));
        }

        if (criteria.RequireMedical == true)
            query = query.Where(person => person.MedicalRecords.Any());
        else if (criteria.RequireMedical == false)
            query = query.Where(person => !person.MedicalRecords.Any());

        if (criteria.RequireFamily == true)
            query = query.Where(person => person.FamilyMembers.Any());
        else if (criteria.RequireFamily == false)
            query = query.Where(person => !person.FamilyMembers.Any());

        query = ApplySqlFilters(query, objectSpace, criteria, today);

        var firstNeedle = FoldedNeedle(criteria.FirstName);
        var middleNeedle = FoldedNeedle(criteria.MiddleName);
        var lastNeedle = FoldedNeedle(criteria.LastName);
        var fullTokens = FoldedNeedle(criteria.FullName).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<Guid> orderedIds;
        if (firstNeedle.Length > 0 || middleNeedle.Length > 0 || lastNeedle.Length > 0 || fullTokens.Length > 0)
        {
            var names = query.Select(person => new
            {
                person.ID,
                person.FirstName,
                person.MiddleName,
                person.LastName,
            }).ToList();

            orderedIds = names
                .Where(person =>
                {
                    var first = PersonSearchTextNormalizer.Fold(person.FirstName);
                    var middle = PersonSearchTextNormalizer.Fold(person.MiddleName);
                    var last = PersonSearchTextNormalizer.Fold(person.LastName);
                    if (firstNeedle.Length > 0 && !first.Contains(firstNeedle, StringComparison.Ordinal))
                        return false;
                    if (middleNeedle.Length > 0 && !middle.Contains(middleNeedle, StringComparison.Ordinal))
                        return false;
                    if (lastNeedle.Length > 0 && !last.Contains(lastNeedle, StringComparison.Ordinal))
                        return false;
                    foreach (var token in fullTokens)
                    {
                        if (!first.Contains(token, StringComparison.Ordinal)
                            && !middle.Contains(token, StringComparison.Ordinal)
                            && !last.Contains(token, StringComparison.Ordinal))
                            return false;
                    }

                    return true;
                })
                .OrderBy(person => person.LastName)
                .ThenBy(person => person.FirstName)
                .Select(person => person.ID)
                .ToList();
        }
        else
        {
            orderedIds = query
                .OrderBy(person => person.LastName)
                .ThenBy(person => person.FirstName)
                .Select(person => person.ID)
                .ToList();
        }

        orderedIds = ApplyDetailFilters(objectSpace, orderedIds, criteria, today);
        var total = orderedIds.Count;
        var pageIds = orderedIds.Take(ResultLimit).ToList();
        return new EmployeeSearchResult
        {
            Total = total,
            Truncated = total > pageIds.Count,
            Rows = pageIds.Count == 0
                ? Array.Empty<EmployeeSearchRow>()
                : Hydrate(objectSpace, pageIds, criteria, today),
        };
    }

    private static List<EmployeeSearchRow> Hydrate(
        IObjectSpace objectSpace,
        List<Guid> pageIds,
        EmployeeSearchCriteria criteria,
        DateTime today)
    {
        var people = objectSpace.GetObjectsQuery<Person>()
            .Where(person => pageIds.Contains(person.ID))
            .Select(person => new
            {
                person.ID,
                person.FirstName,
                person.MiddleName,
                person.LastName,
                person.DateOfBirth,
                CitizenshipTm = person.Nationality != null ? person.Nationality.NameTm : null,
                Citizenship = person.Nationality != null ? person.Nationality.Name : null,
                GenderTm = person.Gender != null ? person.Gender.NameTm : null,
                Gender = person.Gender != null ? person.Gender.Name : null,
                person.PersonalNumber,
                person.Email,
                person.BirthPlace,
                person.ForeignAddress,
                person.PreviousWorkplacesInTurkmenistan,
                person.HireDate,
                person.IsArchived,
                MaritalTm = person.MaritalStatus != null ? person.MaritalStatus.NameTm : null,
                Marital = person.MaritalStatus != null ? person.MaritalStatus.Name : null,
                BirthCountryTm = person.CountryOfBirth != null ? person.CountryOfBirth.NameTm : null,
                BirthCountry = person.CountryOfBirth != null ? person.CountryOfBirth.Name : null,
                ForeignCountryTm = person.ForeignAddressCountry != null ? person.ForeignAddressCountry.NameTm : null,
                ForeignCountry = person.ForeignAddressCountry != null ? person.ForeignAddressCountry.Name : null,
                SubcontractorTm = person.Subcontractor != null ? person.Subcontractor.NameTm : null,
                Subcontractor = person.Subcontractor != null ? person.Subcontractor.Name : null,
            })
            .ToList()
            .ToDictionary(person => person.ID);

        var histories = objectSpace.GetObjectsQuery<EmployeePositionHistory>()
            .Where(history => history.Person != null && pageIds.Contains(history.Person.ID))
            .Select(history => new HistoryRow
            {
                PersonId = history.Person.ID,
                StartDate = history.StartDate,
                EndDate = history.EndDate,
                PositionId = history.Position != null ? (Guid?)history.Position.ID : null,
                PositionTm = history.Position != null ? history.Position.NameTm : null,
                Position = history.Position != null ? history.Position.Name : null,
                DepartmentId = history.Department != null ? (Guid?)history.Department.ID : null,
                DepartmentTm = history.Department != null ? history.Department.NameTm : null,
                Department = history.Department != null ? history.Department.Name : null,
                ActualPosition = history.ActualPosition != null ? history.ActualPosition.Name : null,
            })
            .ToList()
            .GroupBy(history => history.PersonId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var visas = objectSpace.GetObjectsQuery<Visa>()
            .Where(visa => visa.Passport != null && visa.Passport.Person != null && pageIds.Contains(visa.Passport.Person.ID))
            .Select(visa => new VisaRow
            {
                PersonId = visa.Passport.Person.ID,
                VisaNumber = visa.VisaNumber,
                StartDate = visa.StartDate,
                ExpirationDate = visa.ExpirationDate,
                TypeTm = visa.VisaType != null ? visa.VisaType.NameTm : null,
                Type = visa.VisaType != null ? visa.VisaType.Name : null,
                CategoryTm = visa.VisaCategory != null ? visa.VisaCategory.NameTm : null,
                Category = visa.VisaCategory != null ? visa.VisaCategory.Name : null,
            })
            .ToList()
            .GroupBy(visa => visa.PersonId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var educations = objectSpace.GetObjectsQuery<Education>()
            .Where(education => education.Person != null && pageIds.Contains(education.Person.ID))
            .Select(education => new EducationRow
            {
                PersonId = education.Person.ID,
                SpecialtyId = education.Specialty != null ? (Guid?)education.Specialty.ID : null,
                SpecialtyTm = education.Specialty != null ? education.Specialty.NameTm : null,
                Specialty = education.Specialty != null ? education.Specialty.Name : null,
                CountryId = education.EducationCountry != null ? (Guid?)education.EducationCountry.ID : null,
                CountryTm = education.EducationCountry != null ? education.EducationCountry.NameTm : null,
                Country = education.EducationCountry != null ? education.EducationCountry.Name : null,
                LevelId = education.EducationLevel != null ? (Guid?)education.EducationLevel.ID : null,
                LevelTm = education.EducationLevel != null ? education.EducationLevel.NameTm : null,
                Level = education.EducationLevel != null ? education.EducationLevel.Name : null,
                GraduationYear = education.GraduationYear,
            })
            .ToList()
            .GroupBy(education => education.PersonId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var contracts = (
                from instance in objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                where instance.ProjectContract != null
                from person in instance.People
                where pageIds.Contains(person.ID)
                select new ContractRow
                {
                    PersonId = person.ID,
                    ContractId = instance.ProjectContract.ID,
                    NameTm = instance.ProjectContract.NameTm,
                    Name = instance.ProjectContract.Name,
                })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var passports = objectSpace.GetObjectsQuery<Passport>()
            .Where(passport => passport.Person != null && pageIds.Contains(passport.Person.ID))
            .Select(passport => new PassportRow
            {
                PersonId = passport.Person.ID,
                Number = passport.PassportNumber,
                TypeTm = passport.PassportType != null ? passport.PassportType.NameTm : null,
                Type = passport.PassportType != null ? passport.PassportType.Name : null,
                CountryTm = passport.IssuedCountry != null ? passport.IssuedCountry.NameTm : null,
                Country = passport.IssuedCountry != null ? passport.IssuedCountry.Name : null,
                IssueDate = passport.IssueDate,
                ExpirationDate = passport.ExpirationDate,
                IsCancelled = passport.IsCancelled,
            })
            .ToList()
            .GroupBy(passport => passport.PersonId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var hasRange = criteria.VisaYearFrom.HasValue || criteria.VisaYearTo.HasValue;
        var rangeStart = hasRange
            ? new DateTime(criteria.VisaYearFrom ?? criteria.VisaYearTo!.Value, 1, 1)
            : today;
        var rangeEnd = hasRange
            ? new DateTime(criteria.VisaYearTo ?? criteria.VisaYearFrom!.Value, 12, 31)
            : today;

        var rows = new List<EmployeeSearchRow>(pageIds.Count);
        foreach (var personId in pageIds)
        {
            if (!people.TryGetValue(personId, out var person))
                continue;

            histories.TryGetValue(personId, out var personHistories);
            visas.TryGetValue(personId, out var personVisas);
            educations.TryGetValue(personId, out var personEducations);
            contracts.TryGetValue(personId, out var personContracts);
            passports.TryGetValue(personId, out var personPassports);

            var position = PickHistory(personHistories, criteria, today);
            var earlier = PickEarlier(personHistories, position?.StartDate);
            var visa = PickVisa(personVisas, criteria, hasRange, rangeStart, rangeEnd, today);
            var education = PickEducation(personEducations, criteria);
            var contract = PickContract(personContracts, criteria.ProjectContractIds);
            var passport = PickPassport(personPassports, today);

            var positionName = Label(position?.PositionTm, position?.Position);
            var departmentName = Label(position?.DepartmentTm, position?.Department);
            var contractName = Label(contract?.NameTm, contract?.Name);
            var specialtyName = Label(education?.SpecialtyTm, education?.Specialty);
            var visaStatus = VisaStatus(visa?.ExpirationDate, visa != null, today);
            var age = Age(person.DateOfBirth, today);
            var citizenship = Label(person.CitizenshipTm, person.Citizenship);
            var gender = Label(person.GenderTm, person.Gender);

            var pills = BuildPills(criteria, citizenship, gender, departmentName, positionName, contractName, specialtyName, education);
            var detail = new EmployeeSearchMatch
            {
                VisaLine = visa == null
                    ? string.Empty
                    : FormatVisa(visa.VisaNumber, visa.StartDate, visa.ExpirationDate, criteria),
                PositionLine = position == null
                    ? string.Empty
                    : FormatPosition(positionName, departmentName, position.StartDate, position.EndDate),
                EarlierPositionLine = earlier == null
                    ? string.Empty
                    : EmployeeSearchText.Format("Line.Earlier", FormatPosition(Label(earlier.PositionTm, earlier.Position), Label(earlier.DepartmentTm, earlier.Department), earlier.StartDate, earlier.EndDate)),
                ContractLine = string.IsNullOrEmpty(contractName) ? string.Empty : contractName,
                EducationLine = FormatEducation(education),
                EducationNote = SpecialtyDiffers(criteria, specialtyName, positionName)
                    ? EmployeeSearchText.Get("Line.SpecialtyDiffers")
                    : string.Empty,
                MoreLine = MoreLine(criteria),
            };

            var fullName = string.Join(" ", new[] { person.FirstName, person.MiddleName, person.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
            rows.Add(new EmployeeSearchRow
            {
                PersonId = personId,
                FullName = fullName,
                AgeText = age?.ToString() ?? string.Empty,
                Citizenship = citizenship,
                Position = positionName,
                VisaStatus = visaStatus,
                Contract = contractName,
                Pills = pills,
                Detail = detail,
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FullName"] = fullName,
                    ["Age"] = age?.ToString() ?? string.Empty,
                    ["Nationality"] = citizenship,
                    ["WhyMatched"] = string.Join("; ", pills),
                    ["Position"] = positionName,
                    ["ProjectContract"] = contractName,
                    ["FirstName"] = person.FirstName?.Trim() ?? string.Empty,
                    ["MiddleName"] = person.MiddleName?.Trim() ?? string.Empty,
                    ["LastName"] = person.LastName?.Trim() ?? string.Empty,
                    ["PersonalNumber"] = person.PersonalNumber?.Trim() ?? string.Empty,
                    ["DateOfBirth"] = DateText(person.DateOfBirth),
                    ["Gender"] = gender,
                    ["MaritalStatus"] = Label(person.MaritalTm, person.Marital),
                    ["CountryOfBirth"] = Label(person.BirthCountryTm, person.BirthCountry),
                    ["BirthPlace"] = person.BirthPlace?.Trim() ?? string.Empty,
                    ["Email"] = person.Email?.Trim() ?? string.Empty,
                    ["HireDate"] = DateText(person.HireDate),
                    ["IsArchived"] = EmployeeSearchText.Get(person.IsArchived ? "Yes" : "No"),
                    ["Subcontractor"] = Label(person.SubcontractorTm, person.Subcontractor),
                    ["ForeignAddress"] = person.ForeignAddress?.Trim() ?? string.Empty,
                    ["ForeignAddressCountry"] = Label(person.ForeignCountryTm, person.ForeignCountry),
                    ["PreviousWorkplacesInTurkmenistan"] = person.PreviousWorkplacesInTurkmenistan?.Trim() ?? string.Empty,
                    ["Department"] = departmentName,
                    ["ActualPosition"] = position?.ActualPosition?.Trim() ?? string.Empty,
                    ["VisaNumber"] = visa?.VisaNumber?.Trim() ?? string.Empty,
                    ["VisaType"] = Label(visa?.TypeTm, visa?.Type),
                    ["VisaCategory"] = Label(visa?.CategoryTm, visa?.Category),
                    ["VisaStartDate"] = visa == null ? string.Empty : DateText(visa.StartDate),
                    ["VisaExpirationDate"] = DateText(visa?.ExpirationDate),
                    ["PassportNumber"] = passport?.Number?.Trim() ?? string.Empty,
                    ["PassportType"] = Label(passport?.TypeTm, passport?.Type),
                    ["PassportIssuedCountry"] = Label(passport?.CountryTm, passport?.Country),
                    ["PassportExpirationDate"] = DateText(passport?.ExpirationDate),
                },
            });
        }

        return rows;
    }

    private static void Normalize(EmployeeSearchCriteria criteria)
    {
        criteria.GenderIds ??= new List<Guid>();
        criteria.NationalityIds ??= new List<Guid>();
        criteria.DepartmentIds ??= new List<Guid>();
        criteria.PositionIds ??= new List<Guid>();
        criteria.ProjectContractIds ??= new List<Guid>();
        criteria.SpecialtyIds ??= new List<Guid>();
        criteria.EducationCountryIds ??= new List<Guid>();
        criteria.EducationLevelIds ??= new List<Guid>();
        criteria.CityIds ??= new List<Guid>();
        criteria.IncompleteGaps ??= new List<string>();
        criteria.MaritalStatusIds ??= new List<Guid>();
        criteria.RelationshipIds ??= new List<Guid>();
        criteria.BirthCountryIds ??= new List<Guid>();
        criteria.ForeignCountryIds ??= new List<Guid>();
        criteria.ActualPositionIds ??= new List<Guid>();
        criteria.SubcontractorIds ??= new List<Guid>();
        criteria.InstitutionIds ??= new List<Guid>();
        criteria.VisaTypeIds ??= new List<Guid>();
        criteria.VisaCategoryIds ??= new List<Guid>();
        criteria.VisaIssuedPlaceIds ??= new List<Guid>();
        criteria.PassportTypeIds ??= new List<Guid>();
        criteria.PassportCountryIds ??= new List<Guid>();
        criteria.InvitationCategoryIds ??= new List<Guid>();
        criteria.InvitationPeriodIds ??= new List<Guid>();
        criteria.PermitLocations ??= new List<string>();
        criteria.PermitPositionIds ??= new List<Guid>();
        criteria.ResidenceTypes ??= new List<string>();
        criteria.RegionIds ??= new List<Guid>();
        criteria.LodgingIds ??= new List<Guid>();
        criteria.HotelIds ??= new List<Guid>();
        criteria.HospitalIds ??= new List<Guid>();
        criteria.OtherSiteIds ??= new List<Guid>();
        criteria.MiddleName ??= string.Empty;
        criteria.FullName ??= string.Empty;
        criteria.PersonalNumber ??= string.Empty;
        criteria.Email ??= string.Empty;
        criteria.FamilyText ??= string.Empty;
        criteria.BirthPlace ??= string.Empty;
        criteria.ForeignAddress ??= string.Empty;
        criteria.PreviousWorkplaces ??= string.Empty;
        criteria.ContractDescription ??= string.Empty;
        criteria.VisaNumber ??= string.Empty;
        criteria.RegistrationState ??= string.Empty;
        criteria.VisaBorderZone ??= string.Empty;
        criteria.AsNumber ??= string.Empty;
        criteria.AddressText ??= string.Empty;
        criteria.BorderZoneNumber ??= string.Empty;
        criteria.BorderZonePlace ??= string.Empty;
        criteria.MedicalNumber ??= string.Empty;
        criteria.InvitationMode ??= string.Empty;
        criteria.PermitMode ??= string.Empty;
        criteria.BorderZoneMode ??= string.Empty;
        criteria.FirstName ??= string.Empty;
        criteria.LastName ??= string.Empty;
        criteria.PassportNumber ??= string.Empty;
        criteria.InvitationNumber ??= string.Empty;
        criteria.WorkPermitNumber ??= string.Empty;
        Swap(ref criteria, c => c.AgeFrom, c => c.AgeTo, (c, a, b) => { c.AgeFrom = a; c.AgeTo = b; });
        Swap(ref criteria, c => c.VisaYearFrom, c => c.VisaYearTo, (c, a, b) => { c.VisaYearFrom = a; c.VisaYearTo = b; });
        Swap(ref criteria, c => c.BirthYearFrom, c => c.BirthYearTo, (c, a, b) => { c.BirthYearFrom = a; c.BirthYearTo = b; });
        Swap(ref criteria, c => c.PositionYearFrom, c => c.PositionYearTo, (c, a, b) => { c.PositionYearFrom = a; c.PositionYearTo = b; });
        Swap(ref criteria, c => c.ContractYearFrom, c => c.ContractYearTo, (c, a, b) => { c.ContractYearFrom = a; c.ContractYearTo = b; });
        Swap(ref criteria, c => c.InvitationYearFrom, c => c.InvitationYearTo, (c, a, b) => { c.InvitationYearFrom = a; c.InvitationYearTo = b; });
        Swap(ref criteria, c => c.PermitYearFrom, c => c.PermitYearTo, (c, a, b) => { c.PermitYearFrom = a; c.PermitYearTo = b; });
        Swap(ref criteria, c => c.MedicalYearFrom, c => c.MedicalYearTo, (c, a, b) => { c.MedicalYearFrom = a; c.MedicalYearTo = b; });
        Swap(ref criteria, c => c.BorderZoneYearFrom, c => c.BorderZoneYearTo, (c, a, b) => { c.BorderZoneYearFrom = a; c.BorderZoneYearTo = b; });
        Swap(ref criteria, c => c.SalaryYearFrom, c => c.SalaryYearTo, (c, a, b) => { c.SalaryYearFrom = a; c.SalaryYearTo = b; });
        criteria.VisaYearFrom = YearOrNull(criteria.VisaYearFrom);
        criteria.VisaYearTo = YearOrNull(criteria.VisaYearTo);
        criteria.GraduationYearFrom = YearOrNull(criteria.GraduationYearFrom);
        criteria.GraduationYearTo = YearOrNull(criteria.GraduationYearTo);
        criteria.AgeAsOfYear = YearOrNull(criteria.AgeAsOfYear);
        criteria.BirthYearFrom = YearOrNull(criteria.BirthYearFrom);
        criteria.BirthYearTo = YearOrNull(criteria.BirthYearTo);
        criteria.FamilyVisaYear = YearOrNull(criteria.FamilyVisaYear);
        criteria.PositionLeftYear = YearOrNull(criteria.PositionLeftYear);
        criteria.PositionYearFrom = YearOrNull(criteria.PositionYearFrom);
        criteria.PositionYearTo = YearOrNull(criteria.PositionYearTo);
        criteria.ChangedDepartmentYear = YearOrNull(criteria.ChangedDepartmentYear);
        criteria.ChangedPositionYear = YearOrNull(criteria.ChangedPositionYear);
        criteria.SeveralPositionsYear = YearOrNull(criteria.SeveralPositionsYear);
        criteria.ContractYearFrom = YearOrNull(criteria.ContractYearFrom);
        criteria.ContractYearTo = YearOrNull(criteria.ContractYearTo);
        criteria.SalaryYearFrom = YearOrNull(criteria.SalaryYearFrom);
        criteria.SalaryYearTo = YearOrNull(criteria.SalaryYearTo);
        criteria.SalaryChangedYear = YearOrNull(criteria.SalaryChangedYear);
        criteria.VisaExpiredYear = YearOrNull(criteria.VisaExpiredYear);
        criteria.FirstVisaYearFrom = YearOrNull(criteria.FirstVisaYearFrom);
        criteria.FirstVisaYearTo = YearOrNull(criteria.FirstVisaYearTo);
        criteria.LastVisaYearFrom = YearOrNull(criteria.LastVisaYearFrom);
        criteria.LastVisaYearTo = YearOrNull(criteria.LastVisaYearTo);
        criteria.CoveredYearFrom = YearOrNull(criteria.CoveredYearFrom);
        criteria.CoveredYearTo = YearOrNull(criteria.CoveredYearTo);
        criteria.StayYear = YearOrNull(criteria.StayYear);
        criteria.InvitationYearFrom = YearOrNull(criteria.InvitationYearFrom);
        criteria.InvitationYearTo = YearOrNull(criteria.InvitationYearTo);
        criteria.PermitYearFrom = YearOrNull(criteria.PermitYearFrom);
        criteria.PermitYearTo = YearOrNull(criteria.PermitYearTo);
        criteria.BorderZoneYearFrom = YearOrNull(criteria.BorderZoneYearFrom);
        criteria.BorderZoneYearTo = YearOrNull(criteria.BorderZoneYearTo);
        criteria.RejectedYear = YearOrNull(criteria.RejectedYear);
        criteria.MedicalYearFrom = YearOrNull(criteria.MedicalYearFrom);
        criteria.MedicalYearTo = YearOrNull(criteria.MedicalYearTo);
        if (criteria.ExpiringWithinDays is < 0) criteria.ExpiringWithinDays = null;
        if (criteria.PassportExpiringDays is < 0) criteria.PassportExpiringDays = null;
        if (criteria.AllowedGapDays is < 0) criteria.AllowedGapDays = null;
        if (criteria.GapLongerThanDays is < 0) criteria.GapLongerThanDays = null;
        if (criteria.StayDaysFrom is < 0) criteria.StayDaysFrom = null;
        if (criteria.StayDaysTo is < 0) criteria.StayDaysTo = null;
        if (criteria.FamilyCountFrom is < 0) criteria.FamilyCountFrom = null;
        if (criteria.FamilyCountTo is < 0) criteria.FamilyCountTo = null;
        if (criteria.YearsFrom is < 0 or > 80) criteria.YearsFrom = null;
        if (criteria.YearsTo is < 0 or > 80) criteria.YearsTo = null;
        if (criteria.AgeFrom is < 0 or > 120)
            criteria.AgeFrom = null;
        if (criteria.AgeTo is < 0 or > 120)
            criteria.AgeTo = null;
    }

    private static void Swap(
        ref EmployeeSearchCriteria criteria,
        Func<EmployeeSearchCriteria, int?> from,
        Func<EmployeeSearchCriteria, int?> to,
        Action<EmployeeSearchCriteria, int?, int?> write)
    {
        var start = from(criteria);
        var end = to(criteria);
        if (start is int a && end is int b && a > b)
            write(criteria, b, a);
    }

    private static int? YearOrNull(int? year) =>
        year is >= 1900 and <= 2100 ? year : null;

    private static string FoldedNeedle(string value) =>
        PersonSearchTextNormalizer.Fold(value ?? string.Empty).Trim();

    /// <summary>
    /// EF translates <c>Contains</c> only for a non-empty list. An inactive filter keeps a dummy id
    /// and a false flag so the SQL predicate stays valid and matches every row for that part.
    /// </summary>
    private static List<Guid> ActiveOrDummy(List<Guid> ids, out bool active)
    {
        active = ids is { Count: > 0 };
        return active ? ids : new List<Guid> { Guid.Empty };
    }

    private static bool IdChosen(IReadOnlyList<Guid> ids, Guid? value) =>
        ids == null || ids.Count == 0 || (value != null && ids.Contains(value.Value));

    private static List<EmployeeSearchLookupOption> LoadCaption<T>(IQueryable<T> query, Func<T, string> caption)
        where T : BaseObject =>
        query.ToList()
            .Select(row => new EmployeeSearchLookupOption { Id = row.ID, Caption = caption(row)?.Trim() ?? string.Empty })
            .Where(option => !string.IsNullOrWhiteSpace(option.Caption))
            .OrderBy(option => option.Caption, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static List<EmployeeSearchLookupOption> Load<T>(IObjectSpace objectSpace) where T : LookupBase =>
        objectSpace.GetObjectsQuery<T>()
            .ToList()
            .Select(lookup => new EmployeeSearchLookupOption
            {
                Id = lookup.ID,
                Caption = LookupLocalization.GetDisplayName(lookup),
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Caption))
            .OrderBy(option => option.Caption, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static int? Age(DateTime dateOfBirth, DateTime today)
    {
        if (dateOfBirth.Year < 1901)
            return null;

        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
        return age < 0 ? null : age;
    }

    private static string Label(string turkmen, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(turkmen))
            return turkmen.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static HistoryRow PickHistory(IEnumerable<HistoryRow> rows, EmployeeSearchCriteria criteria, DateTime today)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return null;

        bool Current(HistoryRow row) => row.StartDate <= today && (row.EndDate == null || row.EndDate >= today);
        bool Matches(HistoryRow row) =>
            IdChosen(criteria.DepartmentIds, row.DepartmentId)
            && IdChosen(criteria.PositionIds, row.PositionId);

        IEnumerable<HistoryRow> pool = list;
        if (criteria.DepartmentIds.Count > 0 || criteria.PositionIds.Count > 0)
        {
            pool = list.Where(Matches);
            if (!criteria.PositionAnyTime)
                pool = pool.Where(Current);
        }
        else
        {
            var open = list.Where(Current).ToList();
            if (open.Count > 0)
                pool = open;
        }

        return pool.OrderByDescending(row => row.StartDate).FirstOrDefault();
    }

    private static HistoryRow PickEarlier(IEnumerable<HistoryRow> rows, DateTime? currentStart)
    {
        if (rows == null || currentStart == null)
            return null;

        return rows
            .Where(row => row.StartDate < currentStart.Value)
            .OrderByDescending(row => row.StartDate)
            .FirstOrDefault();
    }

    private static VisaRow PickVisa(
        IEnumerable<VisaRow> rows,
        EmployeeSearchCriteria criteria,
        bool hasRange,
        DateTime rangeStart,
        DateTime rangeEnd,
        DateTime today)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return null;

        bool Overlaps(VisaRow visa, DateTime start, DateTime end) =>
            visa.StartDate <= end && (visa.ExpirationDate == null || visa.ExpirationDate >= start);

        IEnumerable<VisaRow> pool = list;
        if (hasRange)
            pool = pool.Where(visa => Overlaps(visa, rangeStart, rangeEnd));
        if (criteria.StillValidToday)
            pool = pool.Where(visa => Overlaps(visa, today, today));

        var chosen = pool
            .OrderByDescending(visa => visa.ExpirationDate ?? DateTime.MaxValue)
            .ThenByDescending(visa => visa.StartDate)
            .FirstOrDefault();
        return chosen ?? list.OrderByDescending(visa => visa.StartDate).FirstOrDefault();
    }

    private static PassportRow PickPassport(IEnumerable<PassportRow> rows, DateTime today)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return null;

        var current = list
            .Where(passport => !passport.IsCancelled && (passport.ExpirationDate == null || passport.ExpirationDate.Value.Date >= today))
            .OrderByDescending(passport => passport.ExpirationDate ?? DateTime.MaxValue)
            .FirstOrDefault();
        return current ?? list.OrderByDescending(passport => passport.IssueDate ?? DateTime.MinValue).FirstOrDefault();
    }

    private static string DateText(DateTime value) => value.Year < 1901 ? string.Empty : value.ToString("dd.MM.yyyy");

    private static string DateText(DateTime? value) => value == null ? string.Empty : DateText(value.Value);

    private static EducationRow PickEducation(IEnumerable<EducationRow> rows, EmployeeSearchCriteria criteria)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return null;

        bool Matches(EducationRow education) =>
            IdChosen(criteria.SpecialtyIds, education.SpecialtyId)
            && IdChosen(criteria.EducationCountryIds, education.CountryId)
            && IdChosen(criteria.EducationLevelIds, education.LevelId)
            && (criteria.GraduationYearFrom == null || (education.GraduationYear != null && string.Compare(education.GraduationYear, criteria.GraduationYearFrom.Value.ToString(), StringComparison.Ordinal) >= 0))
            && (criteria.GraduationYearTo == null || (education.GraduationYear != null && string.Compare(education.GraduationYear, criteria.GraduationYearTo.Value.ToString(), StringComparison.Ordinal) <= 0));

        if (criteria.SpecialtyIds.Count > 0 || criteria.EducationCountryIds.Count > 0 || criteria.EducationLevelIds.Count > 0
            || criteria.GraduationYearFrom != null || criteria.GraduationYearTo != null)
        {
            var match = list.Where(Matches).FirstOrDefault();
            if (match != null)
                return match;
        }

        return list.FirstOrDefault();
    }

    private static ContractRow PickContract(IEnumerable<ContractRow> rows, IReadOnlyList<Guid> contractIds)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return null;

        if (contractIds is { Count: > 0 })
        {
            var match = list.FirstOrDefault(row => row.ContractId is Guid id && contractIds.Contains(id));
            if (match != null)
                return match;
        }

        return list.FirstOrDefault();
    }

    private static string VisaStatus(DateTime? expiration, bool hasVisa, DateTime today)
    {
        if (!hasVisa)
            return "None";
        if (expiration == null || expiration.Value.Date >= today.AddDays(30))
            return "Valid";
        if (expiration.Value.Date >= today)
            return "Expiring";
        return "Expired";
    }

    private static string FormatVisa(string number, DateTime start, DateTime? end, EmployeeSearchCriteria criteria)
    {
        var span = start.ToString("dd.MM.yyyy") + " – " + (end?.ToString("dd.MM.yyyy") ?? EmployeeSearchText.Get("Line.Open"));
        var who = string.IsNullOrWhiteSpace(number)
            ? EmployeeSearchText.Get("Line.Visa")
            : EmployeeSearchText.Format("Line.VisaNumber", number.Trim());
        if (criteria.VisaYearFrom is int from)
        {
            var to = criteria.VisaYearTo ?? from;
            var years = from == to ? from.ToString() : from + "–" + to;
            return EmployeeSearchText.Format("Line.VisaOverlap", who, years, span);
        }

        return EmployeeSearchText.Format("Line.VisaSpan", who, span);
    }

    private static string FormatPosition(string position, string department, DateTime start, DateTime? end)
    {
        var title = string.Join(", ", new[] { position, department }.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (string.IsNullOrEmpty(title))
            title = EmployeeSearchText.Get("Line.Position");
        var until = end?.ToString("dd.MM.yyyy") ?? EmployeeSearchText.Get("Line.StillOpen");
        return title + ", " + start.ToString("dd.MM.yyyy") + " – " + until;
    }

    private static string FormatEducation(EducationRow education)
    {
        if (education == null)
            return string.Empty;

        var parts = new[]
        {
            Label(education.LevelTm, education.Level),
            Label(education.SpecialtyTm, education.Specialty),
            Label(education.CountryTm, education.Country),
            string.IsNullOrWhiteSpace(education.GraduationYear) ? null : EmployeeSearchText.Format("Line.Graduation", education.GraduationYear.Trim()),
        };
        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string MoreLine(EmployeeSearchCriteria criteria)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(criteria.PassportNumber))
            parts.Add(EmployeeSearchText.Format("Line.PassportContains", criteria.PassportNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(criteria.InvitationNumber))
            parts.Add(EmployeeSearchText.Format("Line.InvitationContains", criteria.InvitationNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(criteria.WorkPermitNumber))
            parts.Add(EmployeeSearchText.Format("Line.PermitContains", criteria.WorkPermitNumber.Trim()));
        if (criteria.RequireMedical == true)
            parts.Add(EmployeeSearchText.Get("Line.MedicalYes"));
        else if (criteria.RequireMedical == false)
            parts.Add(EmployeeSearchText.Get("Line.MedicalNo"));
        if (criteria.RequireFamily == true)
            parts.Add(EmployeeSearchText.Get("Line.FamilyYes"));
        else if (criteria.RequireFamily == false)
            parts.Add(EmployeeSearchText.Get("Line.FamilyNo"));
        if (criteria.ArchivedOnly)
            parts.Add(EmployeeSearchText.Get("Line.ArchivedOnly"));
        else if (criteria.IncludeArchived)
            parts.Add(EmployeeSearchText.Get("Line.ArchivedIncluded"));
        return string.Join(". ", parts);
    }

    private static bool SpecialtyDiffers(EmployeeSearchCriteria criteria, string specialty, string position)
    {
        if (criteria.SpecialtyIds.Count == 0 || string.IsNullOrWhiteSpace(specialty) || string.IsNullOrWhiteSpace(position))
            return false;

        var foldedSpecialty = PersonSearchTextNormalizer.Fold(specialty);
        var foldedPosition = PersonSearchTextNormalizer.Fold(position);
        return !foldedPosition.Contains(foldedSpecialty, StringComparison.Ordinal)
            && !foldedSpecialty.Contains(foldedPosition, StringComparison.Ordinal);
    }

    private static List<string> BuildPills(
        EmployeeSearchCriteria criteria,
        string citizenship,
        string gender,
        string department,
        string position,
        string contract,
        string specialty,
        EducationRow education)
    {
        var pills = new List<string>();
        if (!string.IsNullOrWhiteSpace(gender) && criteria.GenderIds.Count > 0)
            pills.Add(gender);
        if (criteria.AgeFrom != null || criteria.AgeTo != null)
        {
            if (criteria.AgeFrom is int from && criteria.AgeTo is int to)
                pills.Add(EmployeeSearchText.Format("Pill.AgeRange", from, to));
            else if (criteria.AgeFrom is int onlyFrom)
                pills.Add(EmployeeSearchText.Format("Pill.AgeFrom", onlyFrom));
            else
                pills.Add(EmployeeSearchText.Format("Pill.AgeTo", criteria.AgeTo));
        }

        if (criteria.NationalityIds is { Count: > 0 } && !string.IsNullOrWhiteSpace(citizenship))
            pills.Add(citizenship);
        if (!string.IsNullOrWhiteSpace(department) && criteria.DepartmentIds.Count > 0)
            pills.Add(department);
        if (!string.IsNullOrWhiteSpace(position) && criteria.PositionIds.Count > 0)
            pills.Add(position);
        if (criteria.VisaYearFrom is int yearFrom)
        {
            var yearTo = criteria.VisaYearTo ?? yearFrom;
            pills.Add(yearFrom == yearTo
                ? EmployeeSearchText.Format("Pill.VisaYear", yearFrom)
                : EmployeeSearchText.Format("Pill.VisaYears", yearFrom, yearTo));
        }

        if (criteria.StillValidToday)
            pills.Add(EmployeeSearchText.Get("ValidToday"));
        if (!string.IsNullOrWhiteSpace(contract) && criteria.ProjectContractIds.Count > 0)
            pills.Add(contract);
        if (!string.IsNullOrWhiteSpace(specialty) && criteria.SpecialtyIds.Count > 0)
            pills.Add(specialty);
        else if (criteria.EducationCountryIds.Count > 0)
        {
            var country = Label(education?.CountryTm, education?.Country);
            if (!string.IsNullOrWhiteSpace(country))
                pills.Add(country);
        }

        if (!string.IsNullOrWhiteSpace(criteria.PassportNumber))
            pills.Add(EmployeeSearchText.Get("Pill.Passport"));
        if (!string.IsNullOrWhiteSpace(criteria.InvitationNumber))
            pills.Add(EmployeeSearchText.Get("Pill.Invitation"));
        if (!string.IsNullOrWhiteSpace(criteria.WorkPermitNumber))
            pills.Add(EmployeeSearchText.Get("Pill.Permit"));
        if (criteria.CityIds.Count > 0)
            pills.Add(EmployeeSearchText.Get("Pill.Address"));
        if (criteria.RequireMedical == true)
            pills.Add(EmployeeSearchText.Get("Pill.Medical"));
        else if (criteria.RequireMedical == false)
            pills.Add(EmployeeSearchText.Get("Pill.NoMedical"));
        if (criteria.RequireFamily == true)
            pills.Add(EmployeeSearchText.Get("Pill.Family"));
        else if (criteria.RequireFamily == false)
            pills.Add(EmployeeSearchText.Get("Pill.NoFamily"));
        return pills;
    }

    private sealed class HistoryRow
    {
        public Guid PersonId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? PositionId { get; set; }
        public string PositionTm { get; set; }
        public string Position { get; set; }
        public Guid? DepartmentId { get; set; }
        public string DepartmentTm { get; set; }
        public string Department { get; set; }
        public string ActualPosition { get; set; }
    }

    private sealed class VisaRow
    {
        public Guid PersonId { get; set; }
        public string VisaNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string TypeTm { get; set; }
        public string Type { get; set; }
        public string CategoryTm { get; set; }
        public string Category { get; set; }
    }

    private sealed class PassportRow
    {
        public Guid PersonId { get; set; }
        public string Number { get; set; }
        public string TypeTm { get; set; }
        public string Type { get; set; }
        public string CountryTm { get; set; }
        public string Country { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsCancelled { get; set; }
    }

    private sealed class EducationRow
    {
        public Guid PersonId { get; set; }
        public Guid? SpecialtyId { get; set; }
        public string SpecialtyTm { get; set; }
        public string Specialty { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryTm { get; set; }
        public string Country { get; set; }
        public Guid? LevelId { get; set; }
        public string LevelTm { get; set; }
        public string Level { get; set; }
        public string GraduationYear { get; set; }
    }

    private sealed class ContractRow
    {
        public Guid PersonId { get; set; }
        public Guid ContractId { get; set; }
        public string NameTm { get; set; }
        public string Name { get; set; }
    }
}
