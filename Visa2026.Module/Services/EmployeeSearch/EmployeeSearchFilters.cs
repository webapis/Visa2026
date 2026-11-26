using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.EmployeeSearch;

public static partial class EmployeeSearchService
{
    private static IQueryable<Person> ApplySqlFilters(
        IQueryable<Person> query,
        IObjectSpace objectSpace,
        EmployeeSearchCriteria criteria,
        DateTime today)
    {
        query = ApplyIdentity(query, criteria);
        query = ApplyFamily(query, criteria, today);
        query = ApplyHome(query, criteria);
        query = ApplyEmployment(query, criteria);
        query = ApplyContract(query, criteria, today);
        query = ApplySalary(query, criteria);
        query = ApplyPassport(query, criteria, today);
        query = ApplyInvitation(query, criteria, today);
        query = ApplyPermit(query, criteria, today);
        query = ApplyAddress(query, criteria);
        query = ApplyBorderAndMedical(query, objectSpace, criteria, today);
        return query;
    }

    private static IQueryable<Person> ApplyIdentity(IQueryable<Person> query, EmployeeSearchCriteria criteria)
    {
        var personal = Lower(criteria.PersonalNumber);
        if (personal.Length > 0)
            query = query.Where(person => person.PersonalNumber != null && person.PersonalNumber.ToLower().Contains(personal));

        var email = Lower(criteria.Email);
        if (email.Length > 0)
            query = query.Where(person => person.Email != null && person.Email.ToLower().Contains(email));

        if (criteria.HasPhoto == true)
            query = query.Where(person => person.Photo != null && person.Photo.Length > 0);
        else if (criteria.HasPhoto == false)
            query = query.Where(person => person.Photo == null || person.Photo.Length == 0);

        if (criteria.Incomplete == true)
            query = query.Where(person => person.IsDataIncomplete);
        else if (criteria.Incomplete == false)
            query = query.Where(person => !person.IsDataIncomplete);

        var gaps = criteria.IncompleteGaps ?? new List<string>();
        if (gaps.Count > 0)
        {
            var personalGap = Gap(gaps, "personal");
            var passportGap = Gap(gaps, "passport");
            var cvGap = Gap(gaps, "cv");
            var photoGap = Gap(gaps, "photo");
            var educationGap = Gap(gaps, "education");
            var medicalGap = Gap(gaps, "medical");
            var addressGap = Gap(gaps, "address");
            var familyGap = Gap(gaps, "family");
            var otherGap = Gap(gaps, "other");
            query = query.Where(person =>
                (personalGap && person.IncompleteMissingPersonalData)
                || (passportGap && person.IncompleteMissingPassport)
                || (cvGap && person.IncompleteMissingCv)
                || (photoGap && person.IncompleteMissingPhoto)
                || (educationGap && person.IncompleteMissingEducation)
                || (medicalGap && person.IncompleteMissingMedical)
                || (addressGap && person.IncompleteMissingAddress)
                || (familyGap && person.IncompleteMissingFamilyDocs)
                || (otherGap && person.IncompleteMissingOther));
        }

        return query;
    }

    private static IQueryable<Person> ApplyFamily(IQueryable<Person> query, EmployeeSearchCriteria criteria, DateTime today)
    {
        if (criteria.BirthFrom is DateTime birthFrom)
            query = query.Where(person => person.DateOfBirth >= birthFrom && person.DateOfBirth.Year > 1900);
        if (criteria.BirthTo is DateTime birthTo)
            query = query.Where(person => person.DateOfBirth <= birthTo && person.DateOfBirth.Year > 1900);
        if (criteria.BirthYearFrom is int birthYearFrom)
            query = query.Where(person => person.DateOfBirth.Year >= birthYearFrom);
        if (criteria.BirthYearTo is int birthYearTo)
            query = query.Where(person => person.DateOfBirth.Year <= birthYearTo && person.DateOfBirth.Year > 1900);

        if (criteria.MaritalStatusIds.Count > 0)
        {
            var ids = criteria.MaritalStatusIds;
            query = query.Where(person => person.MaritalStatus != null && ids.Contains(person.MaritalStatus.ID));
        }

        if (criteria.RelationshipIds.Count > 0)
        {
            var ids = criteria.RelationshipIds;
            query = query.Where(person => person.FamilyMembers.Any(member => member.Relationship != null && ids.Contains(member.Relationship.ID)));
        }

        if (criteria.FamilyCountFrom is int familyFrom)
            query = query.Where(person => person.FamilyMembers.Count >= familyFrom);
        if (criteria.FamilyCountTo is int familyTo)
            query = query.Where(person => person.FamilyMembers.Count <= familyTo);

        if (criteria.FamilyVisa != null)
        {
            var year = criteria.FamilyVisaYear;
            var hasYear = year != null;
            var start = hasYear ? new DateTime(year!.Value, 1, 1) : today;
            var end = hasYear ? new DateTime(year!.Value, 12, 31) : today;
            if (criteria.FamilyVisa == true)
            {
                query = query.Where(person => person.FamilyMembers.Any(member => member.Passports.Any(passport => passport.Visas.Any(visa =>
                    !hasYear || (visa.StartDate <= end && (visa.ExpirationDate == null || visa.ExpirationDate >= start))))));
            }
            else
            {
                query = query.Where(person => !person.FamilyMembers.Any(member => member.Passports.Any(passport => passport.Visas.Any(visa =>
                    !hasYear || (visa.StartDate <= end && (visa.ExpirationDate == null || visa.ExpirationDate >= start))))));
            }
        }

        var familyText = Lower(criteria.FamilyText);
        if (familyText.Length > 0)
            query = query.Where(person => person.VisaApplicationFamilyMembersText != null && person.VisaApplicationFamilyMembersText.ToLower().Contains(familyText));
        if (criteria.FamilyTextEmpty == true)
            query = query.Where(person => person.VisaApplicationFamilyMembersText == null || person.VisaApplicationFamilyMembersText.Trim() == string.Empty || person.VisaApplicationFamilyMembersText.ToLower() == "ýok");
        else if (criteria.FamilyTextEmpty == false)
            query = query.Where(person => person.VisaApplicationFamilyMembersText != null && person.VisaApplicationFamilyMembersText.Trim() != string.Empty && person.VisaApplicationFamilyMembersText.ToLower() != "ýok");

        return query;
    }

    private static IQueryable<Person> ApplyHome(IQueryable<Person> query, EmployeeSearchCriteria criteria)
    {
        if (criteria.BirthCountryIds.Count > 0)
        {
            var ids = criteria.BirthCountryIds;
            query = query.Where(person => person.CountryOfBirth != null && ids.Contains(person.CountryOfBirth.ID));
        }

        var birthPlace = Lower(criteria.BirthPlace);
        if (birthPlace.Length > 0)
            query = query.Where(person => person.BirthPlace != null && person.BirthPlace.ToLower().Contains(birthPlace));

        if (criteria.CitizenshipDiffersFromBirth == true)
            query = query.Where(person => person.Nationality != null && person.CountryOfBirth != null && person.Nationality.ID != person.CountryOfBirth.ID);
        else if (criteria.CitizenshipDiffersFromBirth == false)
            query = query.Where(person => person.Nationality != null && person.CountryOfBirth != null && person.Nationality.ID == person.CountryOfBirth.ID);

        if (criteria.PassportFromOtherCountry == true)
            query = query.Where(person => person.Nationality != null && person.Passports.Any(passport => passport.IssuedCountry != null && passport.IssuedCountry.ID != person.Nationality.ID));
        else if (criteria.PassportFromOtherCountry == false)
            query = query.Where(person => person.Nationality == null || !person.Passports.Any(passport => passport.IssuedCountry != null && passport.IssuedCountry.ID != person.Nationality.ID));

        if (criteria.ForeignCountryIds.Count > 0)
        {
            var ids = criteria.ForeignCountryIds;
            query = query.Where(person => person.ForeignAddressCountry != null && ids.Contains(person.ForeignAddressCountry.ID));
        }

        var foreignAddress = Lower(criteria.ForeignAddress);
        if (foreignAddress.Length > 0)
            query = query.Where(person => person.ForeignAddress != null && person.ForeignAddress.ToLower().Contains(foreignAddress));

        return query;
    }

    private static IQueryable<Person> ApplyEmployment(IQueryable<Person> query, EmployeeSearchCriteria criteria)
    {
        if (criteria.HireFrom is DateTime hireFrom)
            query = query.Where(person => person.HireDate >= hireFrom && person.HireDate.Year >= 1901);
        if (criteria.HireTo is DateTime hireTo)
            query = query.Where(person => person.HireDate <= hireTo && person.HireDate.Year >= 1901);

        var previous = Lower(criteria.PreviousWorkplaces);
        if (previous.Length > 0)
            query = query.Where(person => person.PreviousWorkplacesInTurkmenistan != null && person.PreviousWorkplacesInTurkmenistan.ToLower().Contains(previous));

        return query;
    }

    private static IQueryable<Person> ApplyContract(IQueryable<Person> query, EmployeeSearchCriteria criteria, DateTime today)
    {
        if (criteria.ContractYearFrom != null || criteria.ContractYearTo != null)
        {
            var start = new DateTime(criteria.ContractYearFrom ?? criteria.ContractYearTo!.Value, 1, 1);
            var end = new DateTime(criteria.ContractYearTo ?? criteria.ContractYearFrom!.Value, 12, 31);
            query = query.Where(person =>
                (person.ProjectContract != null || person.ApplicationProfileInstances.Any(instance => instance.ProjectContract != null))
                && person.PositionHistory.Any(history => history.StartDate <= end && (history.EndDate == null || history.EndDate >= start)));
        }

        if (criteria.ContractActive == true)
        {
            query = query.Where(person =>
                (person.ProjectContract != null && person.ProjectContract.IsActive)
                || person.ApplicationProfileInstances.Any(instance => instance.ProjectContract != null && instance.ProjectContract.IsActive));
        }
        else if (criteria.ContractActive == false)
        {
            query = query.Where(person =>
                (person.ProjectContract == null || !person.ProjectContract.IsActive)
                && !person.ApplicationProfileInstances.Any(instance => instance.ProjectContract != null && instance.ProjectContract.IsActive));
        }

        var description = Lower(criteria.ContractDescription);
        if (description.Length > 0)
        {
            query = query.Where(person =>
                (person.ProjectContract != null && person.ProjectContract.Description != null && person.ProjectContract.Description.ToLower().Contains(description))
                || person.ApplicationProfileInstances.Any(instance => instance.ProjectContract != null && instance.ProjectContract.Description != null && instance.ProjectContract.Description.ToLower().Contains(description)));
        }

        if (criteria.SubcontractorIds.Count > 0)
        {
            var ids = criteria.SubcontractorIds;
            query = query.Where(person => person.Subcontractor != null && ids.Contains(person.Subcontractor.ID));
        }

        return query;
    }

    private static IQueryable<Person> ApplySalary(IQueryable<Person> query, EmployeeSearchCriteria criteria)
    {
        if (criteria.SalaryYearFrom == null && criteria.SalaryYearTo == null)
            return query;

        var start = new DateTime(criteria.SalaryYearFrom ?? criteria.SalaryYearTo!.Value, 1, 1);
        var end = new DateTime(criteria.SalaryYearTo ?? criteria.SalaryYearFrom!.Value, 12, 31);
        return query.Where(person => person.Salaries.Any(salary =>
            salary.StartDate <= end && (salary.EndDate == null || salary.EndDate >= start)));
    }

    private static IQueryable<Person> ApplyPassport(IQueryable<Person> query, EmployeeSearchCriteria criteria, DateTime today)
    {
        var number = Lower(criteria.PassportNumber);
        var types = ActiveOrDummy(criteria.PassportTypeIds, out var filterType);
        var countries = ActiveOrDummy(criteria.PassportCountryIds, out var filterCountry);
        var issuedFrom = criteria.PassportIssuedFrom;
        var issuedTo = criteria.PassportIssuedTo;
        var expiresFrom = criteria.PassportExpiresFrom;
        var expiresTo = criteria.PassportExpiresTo;
        var currentOnly = criteria.PassportCurrentOnly;
        var expired = criteria.PassportExpired == true;
        var notExpired = criteria.PassportExpired == false;
        var expiringDays = criteria.PassportExpiringDays ?? 0;
        var filterExpiring = criteria.PassportExpiringDays is > 0;
        var expiringUntil = today.AddDays(expiringDays);
        var cancelled = criteria.PassportCancelled == true;
        var notCancelled = criteria.PassportCancelled == false;
        var filterNumber = number.Length > 0;
        var active = filterNumber || filterType || filterCountry || issuedFrom != null || issuedTo != null
            || expiresFrom != null || expiresTo != null || currentOnly || expired || notExpired || filterExpiring
            || cancelled || notCancelled;
        if (active)
        {
            query = query.Where(person => person.Passports.Any(passport =>
                (!filterNumber || (passport.PassportNumber != null && passport.PassportNumber.ToLower().Contains(number)))
                && (!filterType || (passport.PassportType != null && types.Contains(passport.PassportType.ID)))
                && (!filterCountry || (passport.IssuedCountry != null && countries.Contains(passport.IssuedCountry.ID)))
                && (issuedFrom == null || (passport.IssueDate != null && passport.IssueDate >= issuedFrom))
                && (issuedTo == null || (passport.IssueDate != null && passport.IssueDate <= issuedTo))
                && (expiresFrom == null || (passport.ExpirationDate != null && passport.ExpirationDate >= expiresFrom))
                && (expiresTo == null || (passport.ExpirationDate != null && passport.ExpirationDate <= expiresTo))
                && (!currentOnly || ((passport.ExpirationDate == null || passport.ExpirationDate >= today) && !passport.IsCancelled))
                && (!expired || (passport.ExpirationDate != null && passport.ExpirationDate < today))
                && (!notExpired || passport.ExpirationDate == null || passport.ExpirationDate >= today)
                && (!filterExpiring || (passport.ExpirationDate != null && passport.ExpirationDate >= today && passport.ExpirationDate <= expiringUntil))
                && (!cancelled || passport.IsCancelled)
                && (!notCancelled || !passport.IsCancelled)));
        }

        if (criteria.SeveralPassports == true)
            query = query.Where(person => person.Passports.Count() > 1);
        else if (criteria.SeveralPassports == false)
            query = query.Where(person => person.Passports.Count() <= 1);

        return query;
    }

    private static IQueryable<Person> ApplyInvitation(IQueryable<Person> query, EmployeeSearchCriteria criteria, DateTime today)
    {
        var mode = criteria.InvitationMode ?? string.Empty;
        if (string.Equals(mode, "ever", StringComparison.OrdinalIgnoreCase))
            query = query.Where(person => person.InvitationItems.Any());
        else if (string.Equals(mode, "unused", StringComparison.OrdinalIgnoreCase))
            query = query.Where(person => person.InvitationItems.Any(item => item.IssuedVisa == null));
        else if (string.Equals(mode, "used", StringComparison.OrdinalIgnoreCase))
            query = query.Where(person => person.InvitationItems.Any(item => item.IssuedVisa != null));

        if (criteria.InvitationYearFrom != null || criteria.InvitationYearTo != null)
        {
            var start = new DateTime(criteria.InvitationYearFrom ?? criteria.InvitationYearTo!.Value, 1, 1);
            var end = new DateTime(criteria.InvitationYearTo ?? criteria.InvitationYearFrom!.Value, 12, 31);
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.Invitation != null && item.Invitation.IssuedDate >= start && item.Invitation.IssuedDate <= end));
        }

        if (criteria.InvitationCategoryIds.Count > 0)
        {
            var ids = criteria.InvitationCategoryIds;
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.Invitation != null && item.Invitation.VisaCategory != null && ids.Contains(item.Invitation.VisaCategory.ID)));
        }

        if (criteria.InvitationPeriodIds.Count > 0)
        {
            var ids = criteria.InvitationPeriodIds;
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.Invitation != null && item.Invitation.VisaPeriod != null && ids.Contains(item.Invitation.VisaPeriod.ID)));
        }

        if (criteria.InvitationValidOn is DateTime validOn)
        {
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.Invitation != null
                && item.Invitation.IssuedDate <= validOn
                && (item.Invitation.ExpirationDate == null || item.Invitation.ExpirationDate >= validOn)));
        }

        if (criteria.InvitationExpiredNoVisa == true)
        {
            query = query.Where(person => person.InvitationItems.Any(item =>
                item.IssuedVisa == null && item.Invitation != null && item.Invitation.ExpirationDate != null && item.Invitation.ExpirationDate < today));
        }
        else if (criteria.InvitationExpiredNoVisa == false)
        {
            query = query.Where(person => !person.InvitationItems.Any(item =>
                item.IssuedVisa == null && item.Invitation != null && item.Invitation.ExpirationDate != null && item.Invitation.ExpirationDate < today));
        }

        var family = ApplicationProfileActionFamily.Cancellation;
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        if (criteria.InvitationCancelled == true)
        {
            query = query.Where(person => person.InvitationItems.Any(item => item.ApplicationProfileInstances.Any(instance =>
                instance.ApplicationProfile != null
                && instance.ApplicationProfile.ActionFamily == family
                && instance.LatestPrimaryStateCode == issued)));
        }
        else if (criteria.InvitationCancelled == false)
        {
            query = query.Where(person => !person.InvitationItems.Any(item => item.ApplicationProfileInstances.Any(instance =>
                instance.ApplicationProfile != null
                && instance.ApplicationProfile.ActionFamily == family
                && instance.LatestPrimaryStateCode == issued)));
        }

        return query;
    }

    private static IQueryable<Person> ApplyPermit(IQueryable<Person> query, EmployeeSearchCriteria criteria, DateTime today)
    {
        var mode = criteria.PermitMode ?? string.Empty;
        if (string.Equals(mode, "ever", StringComparison.OrdinalIgnoreCase))
            query = query.Where(person => person.WorkPermitItems.Any());
        else if (string.Equals(mode, "current", StringComparison.OrdinalIgnoreCase))
            query = query.Where(person => person.WorkPermitItems.Any(item => item.StartDate <= today && item.ExpirationDate >= today));

        if (criteria.PermitYearFrom != null || criteria.PermitYearTo != null)
        {
            var start = new DateTime(criteria.PermitYearFrom ?? criteria.PermitYearTo!.Value, 1, 1);
            var end = new DateTime(criteria.PermitYearTo ?? criteria.PermitYearFrom!.Value, 12, 31);
            query = query.Where(person => person.WorkPermitItems.Any(item =>
                item.StartDate <= end && item.ExpirationDate >= start));
        }

        var asNumber = Lower(criteria.AsNumber);
        if (asNumber.Length > 0)
            query = query.Where(person => person.WorkPermitItems.Any(item => item.ASNumber != null && item.ASNumber.ToLower().Contains(asNumber)));

        if (criteria.PermitPositionIds.Count > 0)
        {
            var ids = criteria.PermitPositionIds;
            query = query.Where(person => person.WorkPermitItems.Any(item =>
                item.CurrentPositionHistory != null
                && item.CurrentPositionHistory.Position != null
                && ids.Contains(item.CurrentPositionHistory.Position.ID)));
        }

        return query;
    }

    private static IQueryable<Person> ApplyAddress(IQueryable<Person> query, EmployeeSearchCriteria criteria)
    {
        var types = criteria.ResidenceTypes ?? new List<string>();
        var lodging = types.Any(label => FoldedNeedle(label).Contains("lodg"));
        var hotel = types.Any(label => FoldedNeedle(label).Contains("hotel"));
        var house = types.Any(label => FoldedNeedle(label).Contains("house") || FoldedNeedle(label).Contains("private"));
        var hospital = types.Any(label => FoldedNeedle(label).Contains("hospit"));
        var other = types.Any(label => FoldedNeedle(label).Contains("other"));
        var filterType = lodging || hotel || house || hospital || other;
        var regions = ActiveOrDummy(criteria.RegionIds, out var filterRegion);
        var cities = ActiveOrDummy(criteria.CityIds, out var filterCity);
        var lodgings = ActiveOrDummy(criteria.LodgingIds, out var filterLodging);
        var hotels = ActiveOrDummy(criteria.HotelIds, out var filterHotel);
        var hospitals = ActiveOrDummy(criteria.HospitalIds, out var filterHospital);
        var sites = ActiveOrDummy(criteria.OtherSiteIds, out var filterSite);
        var text = Lower(criteria.AddressText);
        var filterText = text.Length > 0;
        if (filterType || filterRegion || filterCity || filterLodging || filterHotel || filterHospital || filterSite || filterText)
        {
            query = query.Where(person => person.AddressesOfResidence.Any(address =>
                (!filterType || ((lodging && address.Type == ResidenceType.Lodging)
                    || (hotel && address.Type == ResidenceType.Hotel)
                    || (house && address.Type == ResidenceType.PrivateHouse)
                    || (hospital && address.Type == ResidenceType.Hospital)
                    || (other && address.Type == ResidenceType.Other)))
                && (!filterRegion || (address.Region != null && regions.Contains(address.Region.ID)))
                && (!filterCity || (address.City != null && cities.Contains(address.City.ID)))
                && (!filterLodging || (address.Lodging != null && lodgings.Contains(address.Lodging.ID)))
                && (!filterHotel || (address.Hotel != null && hotels.Contains(address.Hotel.ID)))
                && (!filterHospital || (address.Hospital != null && hospitals.Contains(address.Hospital.ID)))
                && (!filterSite || (address.OtherSite != null && sites.Contains(address.OtherSite.ID)))
                && (!filterText || (
                    (address.FullAddress != null && address.FullAddress.ToLower().Contains(text))
                    || (address.Lodging != null && address.Lodging.FullAddress != null && address.Lodging.FullAddress.ToLower().Contains(text))
                    || (address.Hotel != null && address.Hotel.Name != null && address.Hotel.Name.ToLower().Contains(text))
                    || (address.Hospital != null && address.Hospital.Name != null && address.Hospital.Name.ToLower().Contains(text))
                    || (address.OtherSite != null && address.OtherSite.FullAddress != null && address.OtherSite.FullAddress.ToLower().Contains(text))))));
        }

        if (criteria.AddressExpiredDuringVisa == true)
        {
            query = query.Where(person => person.AddressesOfResidence.Any(address =>
                address.ExpirationDate != null
                && person.Passports.Any(passport => passport.Visas.Any(visa =>
                    visa.StartDate <= address.ExpirationDate
                    && (visa.ExpirationDate == null || visa.ExpirationDate >= address.ExpirationDate)))));
        }
        else if (criteria.AddressExpiredDuringVisa == false)
        {
            query = query.Where(person => !person.AddressesOfResidence.Any(address =>
                address.ExpirationDate != null
                && person.Passports.Any(passport => passport.Visas.Any(visa =>
                    visa.StartDate <= address.ExpirationDate
                    && (visa.ExpirationDate == null || visa.ExpirationDate >= address.ExpirationDate)))));
        }

        return query;
    }

    private static IQueryable<Person> ApplyBorderAndMedical(
        IQueryable<Person> query,
        IObjectSpace objectSpace,
        EmployeeSearchCriteria criteria,
        DateTime today)
    {
        var mode = criteria.BorderZoneMode ?? string.Empty;
        var hasMode = string.Equals(mode, "ever", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "current", StringComparison.OrdinalIgnoreCase);
        var hasYears = criteria.BorderZoneYearFrom != null || criteria.BorderZoneYearTo != null;
        var number = Lower(criteria.BorderZoneNumber);
        var place = Lower(criteria.BorderZonePlace);
        if (hasMode || hasYears || number.Length > 0 || place.Length > 0)
        {
            var current = string.Equals(mode, "current", StringComparison.OrdinalIgnoreCase);
            var start = hasYears ? new DateTime(criteria.BorderZoneYearFrom ?? criteria.BorderZoneYearTo!.Value, 1, 1) : today;
            var end = hasYears ? new DateTime(criteria.BorderZoneYearTo ?? criteria.BorderZoneYearFrom!.Value, 12, 31) : today;
            var zones = objectSpace.GetObjectsQuery<BorderZoneItem>();
            query = query.Where(person => zones.Any(item =>
                item.Person != null
                && item.Person.ID == person.ID
                && item.BorderZone != null
                && (!current || (item.BorderZone.StartDate <= today && (item.BorderZone.ExpirationDate == null || item.BorderZone.ExpirationDate >= today)))
                && (!hasYears || (item.BorderZone.StartDate <= end && (item.BorderZone.ExpirationDate == null || item.BorderZone.ExpirationDate >= start)))
                && (number.Length == 0 || (item.BorderZone.BorderZoneNumber != null && item.BorderZone.BorderZoneNumber.ToLower().Contains(number)))
                && (place.Length == 0 || (item.BorderZoneLocation != null && item.BorderZoneLocation.ToLower().Contains(place)))));
        }

        if (criteria.WasRejected == true || criteria.RejectedYear != null)
        {
            var year = criteria.RejectedYear;
            var hasYear = year != null;
            var items = objectSpace.GetObjectsQuery<RejectionItem>();
            if (criteria.WasRejected == false)
            {
                query = query.Where(person => !items.Any(item =>
                    item.Person != null && item.Person.ID == person.ID
                    && (!hasYear || (item.Rejection != null && item.Rejection.Date.Year == year))));
            }
            else
            {
                query = query.Where(person => items.Any(item =>
                    item.Person != null && item.Person.ID == person.ID
                    && item.Rejection != null
                    && (!hasYear || item.Rejection.Date.Year == year)));
            }
        }

        var medicalNumber = Lower(criteria.MedicalNumber);
        var medicalYears = criteria.MedicalYearFrom != null || criteria.MedicalYearTo != null;
        var medicalOn = criteria.MedicalValidOn;
        if (medicalNumber.Length > 0 || medicalYears || medicalOn != null)
        {
            var start = medicalYears ? new DateTime(criteria.MedicalYearFrom ?? criteria.MedicalYearTo!.Value, 1, 1) : today;
            var end = medicalYears ? new DateTime(criteria.MedicalYearTo ?? criteria.MedicalYearFrom!.Value, 12, 31) : today;
            query = query.Where(person => person.MedicalRecords.Any(record =>
                (medicalNumber.Length == 0 || (record.DocumentNumber != null && record.DocumentNumber.ToLower().Contains(medicalNumber)))
                && (!medicalYears || (record.IssueDate >= start && record.IssueDate <= end))
                && (medicalOn == null || (record.IssueDate <= medicalOn && (record.ExpirationDate == null || record.ExpirationDate >= medicalOn)))));
        }

        if (criteria.MedicalExpiredDuringVisa == true)
        {
            query = query.Where(person => person.MedicalRecords.Any(record =>
                record.ExpirationDate != null
                && person.Passports.Any(passport => passport.Visas.Any(visa =>
                    visa.StartDate <= record.ExpirationDate
                    && (visa.ExpirationDate == null || visa.ExpirationDate >= record.ExpirationDate)))));
        }
        else if (criteria.MedicalExpiredDuringVisa == false)
        {
            query = query.Where(person => !person.MedicalRecords.Any(record =>
                record.ExpirationDate != null
                && person.Passports.Any(passport => passport.Visas.Any(visa =>
                    visa.StartDate <= record.ExpirationDate
                    && (visa.ExpirationDate == null || visa.ExpirationDate >= record.ExpirationDate)))));
        }

        if (criteria.PermitExtended == true)
        {
            var rows = objectSpace.GetObjectsQuery<WorkPermitExtensionTracking>();
            query = query.Where(person => rows.Any(row => row.PersonID == person.ID));
        }
        else if (criteria.PermitExtended == false)
        {
            var rows = objectSpace.GetObjectsQuery<WorkPermitExtensionTracking>();
            query = query.Where(person => !rows.Any(row => row.PersonID == person.ID));
        }

        return query;
    }

    private static List<Guid> ApplyDetailFilters(
        IObjectSpace objectSpace,
        List<Guid> ids,
        EmployeeSearchCriteria criteria,
        DateTime today)
    {
        if (ids.Count == 0 || !NeedsDetail(criteria))
            return ids;

        var keep = new HashSet<Guid>(ids);
        if (criteria.YearsFrom != null || criteria.YearsTo != null)
            keep.IntersectWith(YearsWithCompany(objectSpace, ids, criteria, today));
        if (criteria.ChangedDepartmentYear != null || criteria.ChangedPositionYear != null || criteria.SeveralPositions != null)
            keep.IntersectWith(PositionFacts(objectSpace, ids, criteria));
        if (criteria.SeveralContracts != null)
            keep.IntersectWith(ContractFacts(objectSpace, ids, criteria));
        if (criteria.SalaryFrom != null || criteria.SalaryTo != null || criteria.SalaryChanged != null)
            keep.IntersectWith(SalaryFacts(objectSpace, ids, criteria));
        if (criteria.SpecialtyDiffers != null)
            keep.IntersectWith(SpecialtyFacts(objectSpace, ids, criteria, today));
        if (NeedsVisaFacts(criteria))
            keep.IntersectWith(VisaFacts(objectSpace, ids, criteria, today));
        if (criteria.PermitLocations is { Count: > 0 })
            keep.IntersectWith(PermitLocationFacts(objectSpace, ids, criteria));

        return ids.Where(keep.Contains).ToList();
    }

    private static bool NeedsDetail(EmployeeSearchCriteria criteria) =>
        criteria.YearsFrom != null || criteria.YearsTo != null
        || criteria.ChangedDepartmentYear != null || criteria.ChangedPositionYear != null
        || criteria.SeveralPositions != null || criteria.SeveralContracts != null
        || criteria.SalaryFrom != null || criteria.SalaryTo != null || criteria.SalaryChanged != null
        || criteria.SpecialtyDiffers != null || NeedsVisaFacts(criteria)
        || criteria.PermitLocations is { Count: > 0 };

    private static bool NeedsVisaFacts(EmployeeSearchCriteria criteria) =>
        criteria.FirstVisaYearFrom != null || criteria.FirstVisaYearTo != null
        || criteria.LastVisaYearFrom != null || criteria.LastVisaYearTo != null
        || criteria.CoveredYearFrom != null || criteria.CoveredYearTo != null
        || criteria.HadGap == true || criteria.StayDaysFrom != null || criteria.StayDaysTo != null;

    private static HashSet<Guid> YearsWithCompany(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria, DateTime today)
    {
        var hires = objectSpace.GetObjectsQuery<Person>()
            .Where(person => ids.Contains(person.ID))
            .Select(person => new { person.ID, person.HireDate })
            .ToList();
        var starts = objectSpace.GetObjectsQuery<EmployeePositionHistory>()
            .Where(history => history.Person != null && ids.Contains(history.Person.ID))
            .Select(history => new { PersonId = history.Person.ID, history.StartDate })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.Min(row => row.StartDate));
        var matched = new HashSet<Guid>();
        foreach (var hire in hires)
        {
            DateTime start;
            if (hire.HireDate.Year >= 1901)
                start = hire.HireDate;
            else if (!starts.TryGetValue(hire.ID, out start))
                continue;
            var years = today.Year - start.Year;
            if (start.Date > today.AddYears(-years))
                years--;
            if (years < 0)
                continue;
            if (criteria.YearsFrom is int from && years < from)
                continue;
            if (criteria.YearsTo is int to && years > to)
                continue;
            matched.Add(hire.ID);
        }

        return matched;
    }

    private static HashSet<Guid> PositionFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria)
    {
        var rows = objectSpace.GetObjectsQuery<EmployeePositionHistory>()
            .Where(history => history.Person != null && ids.Contains(history.Person.ID))
            .Select(history => new
            {
                PersonId = history.Person.ID,
                history.StartDate,
                history.EndDate,
                DepartmentId = history.Department != null ? (Guid?)history.Department.ID : null,
                PositionId = history.Position != null ? (Guid?)history.Position.ID : null,
            })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.StartDate).ToList());

        var matched = new HashSet<Guid>();
        foreach (var id in ids)
        {
            rows.TryGetValue(id, out var history);
            history ??= new();
            var changedDepartment = criteria.ChangedDepartmentYear == null || Changed(history.Select(row => (row.StartDate, row.DepartmentId)).ToList(), criteria.ChangedDepartmentYear.Value);
            var changedPosition = criteria.ChangedPositionYear == null || Changed(history.Select(row => (row.StartDate, row.PositionId)).ToList(), criteria.ChangedPositionYear.Value);
            var several = criteria.SeveralPositions == null || SeveralPositionsMatch(
                history.Select(row => (row.StartDate, row.EndDate, row.PositionId)).ToList(),
                criteria);
            if (changedDepartment && changedPosition && several)
                matched.Add(id);
        }

        return matched;
    }

    private static bool Changed(List<(DateTime Start, Guid? Id)> rows, int year)
    {
        for (var i = 1; i < rows.Count; i++)
        {
            if (rows[i].Start.Year == year && rows[i].Id != null && rows[i - 1].Id != null && rows[i].Id != rows[i - 1].Id)
                return true;
        }

        return false;
    }

    private static bool SeveralPositionsMatch(
        IReadOnlyList<(DateTime StartDate, DateTime? EndDate, Guid? PositionId)> rows,
        EmployeeSearchCriteria criteria)
    {
        IEnumerable<(DateTime StartDate, DateTime? EndDate, Guid? PositionId)> pool = rows;
        if (criteria.SeveralPositionsYear is int year)
        {
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);
            pool = pool.Where(row => row.StartDate <= end && (row.EndDate == null || row.EndDate >= start));
        }

        var count = pool.Select(row => row.PositionId).Where(id => id != null).Distinct().Count();
        return criteria.SeveralPositions == true ? count > 1 : count <= 1;
    }

    private static HashSet<Guid> ContractFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria)
    {
        var direct = objectSpace.GetObjectsQuery<Person>()
            .Where(person => ids.Contains(person.ID))
            .Select(person => new { person.ID, ContractId = person.ProjectContract != null ? (Guid?)person.ProjectContract.ID : null })
            .ToList();
        var linked = objectSpace.GetObjectsQuery<Person>()
            .Where(person => ids.Contains(person.ID))
            .SelectMany(person => person.ApplicationProfileInstances
                .Where(instance => instance.ProjectContract != null)
                .Select(instance => new { person.ID, ContractId = (Guid?)instance.ProjectContract.ID }))
            .ToList();
        var byPerson = direct.Select(row => (row.ID, row.ContractId))
            .Concat(linked.Select(row => (row.ID, row.ContractId)))
            .GroupBy(row => row.ID)
            .ToDictionary(group => group.Key, group => group.Select(row => row.ContractId).Where(id => id != null).Distinct().Count());
        var matched = new HashSet<Guid>();
        foreach (var id in ids)
        {
            var count = byPerson.TryGetValue(id, out var value) ? value : 0;
            if (criteria.SeveralContracts == true ? count > 1 : count <= 1)
                matched.Add(id);
        }

        return matched;
    }

    private static HashSet<Guid> SalaryFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria)
    {
        var rows = objectSpace.GetObjectsQuery<EmployeeSalary>()
            .Where(salary => salary.Person != null && ids.Contains(salary.Person.ID))
            .Select(salary => new { PersonId = salary.Person.ID, salary.StartDate, salary.EndDate, salary.Amount })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.StartDate).ToList());
        var matched = new HashSet<Guid>();
        foreach (var id in ids)
        {
            rows.TryGetValue(id, out var salaries);
            salaries ??= new();
            var amountOk = criteria.SalaryFrom == null && criteria.SalaryTo == null;
            if (!amountOk)
            {
                amountOk = salaries.Any(row =>
                {
                    if (!TryMoney(row.Amount, out var amount))
                        return false;
                    if (criteria.SalaryFrom is decimal from && amount < from)
                        return false;
                    if (criteria.SalaryTo is decimal to && amount > to)
                        return false;
                    if (criteria.SalaryYearFrom != null || criteria.SalaryYearTo != null)
                    {
                        var start = new DateTime(criteria.SalaryYearFrom ?? criteria.SalaryYearTo!.Value, 1, 1);
                        var end = new DateTime(criteria.SalaryYearTo ?? criteria.SalaryYearFrom!.Value, 12, 31);
                        if (row.StartDate > end || (row.EndDate != null && row.EndDate < start))
                            return false;
                    }

                    return true;
                });
            }

            var changedOk = criteria.SalaryChanged == null;
            if (!changedOk)
            {
                decimal? previous = null;
                var changed = false;
                foreach (var row in salaries)
                {
                    if (!TryMoney(row.Amount, out var amount))
                        continue;
                    if (previous != null && previous.Value != amount
                        && (criteria.SalaryChangedYear == null || row.StartDate.Year == criteria.SalaryChangedYear))
                        changed = true;
                    previous = amount;
                }

                changedOk = criteria.SalaryChanged == true ? changed : !changed;
            }

            if (amountOk && changedOk)
                matched.Add(id);
        }

        return matched;
    }

    private static HashSet<Guid> SpecialtyFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria, DateTime today)
    {
        var positions = objectSpace.GetObjectsQuery<EmployeePositionHistory>()
            .Where(history => history.Person != null && ids.Contains(history.Person.ID) && history.StartDate <= today && (history.EndDate == null || history.EndDate >= today))
            .Select(history => new
            {
                PersonId = history.Person.ID,
                Name = history.Position != null ? history.Position.Name : null,
                NameTm = history.Position != null ? history.Position.NameTm : null,
            })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.Select(row => FoldedNeedle((row.NameTm ?? string.Empty) + " " + (row.Name ?? string.Empty))).ToList());
        var specialties = objectSpace.GetObjectsQuery<Education>()
            .Where(education => education.Person != null && ids.Contains(education.Person.ID) && education.Specialty != null)
            .Select(education => new
            {
                PersonId = education.Person.ID,
                Name = education.Specialty.Name,
                NameTm = education.Specialty.NameTm,
            })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.Select(row => FoldedNeedle((row.NameTm ?? string.Empty) + " " + (row.Name ?? string.Empty))).Where(name => name.Length > 0).ToList());
        var matched = new HashSet<Guid>();
        foreach (var id in ids)
        {
            positions.TryGetValue(id, out var positionNames);
            specialties.TryGetValue(id, out var specialtyNames);
            positionNames ??= new List<string>();
            specialtyNames ??= new List<string>();
            var differs = positionNames.Any(position => specialtyNames.Any(specialty =>
                specialty.Length > 0 && position.Length > 0
                && !position.Contains(specialty, StringComparison.Ordinal)
                && !specialty.Contains(position, StringComparison.Ordinal)));
            if (criteria.SpecialtyDiffers == true ? differs : !differs)
                matched.Add(id);
        }

        return matched;
    }

    private static HashSet<Guid> VisaFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria, DateTime today)
    {
        var visas = objectSpace.GetObjectsQuery<Visa>()
            .Where(visa => visa.Passport != null && visa.Passport.Person != null && ids.Contains(visa.Passport.Person.ID))
            .Select(visa => new { PersonId = visa.Passport.Person.ID, visa.StartDate, visa.ExpirationDate })
            .ToList()
            .GroupBy(row => row.PersonId)
            .ToDictionary(group => group.Key, group => group.Select(row => (row.StartDate, row.ExpirationDate ?? today)).ToList());
        var matched = new HashSet<Guid>();
        foreach (var id in ids)
        {
            visas.TryGetValue(id, out var spans);
            spans ??= new List<(DateTime StartDate, DateTime)>();
            if (spans.Count == 0)
                continue;
            var first = spans.Min(span => span.StartDate.Year);
            var last = spans.Max(span => span.Item2.Year);
            if (!InRange(first, criteria.FirstVisaYearFrom, criteria.FirstVisaYearTo))
                continue;
            if (!InRange(last, criteria.LastVisaYearFrom, criteria.LastVisaYearTo))
                continue;
            if (criteria.CoveredYearFrom != null || criteria.CoveredYearTo != null)
            {
                var from = criteria.CoveredYearFrom ?? criteria.CoveredYearTo!.Value;
                var to = criteria.CoveredYearTo ?? criteria.CoveredYearFrom!.Value;
                var gap = MaxGap(spans, new DateTime(from, 1, 1), new DateTime(to, 12, 31));
                if (gap > (criteria.AllowedGapDays ?? 0))
                    continue;
            }

            if (criteria.HadGap == true)
            {
                var fromYear = criteria.VisaYearFrom ?? spans.Min(span => span.StartDate.Year);
                var toYear = criteria.VisaYearTo ?? spans.Max(span => span.Item2.Year);
                var gap = MaxGap(spans, new DateTime(fromYear, 1, 1), new DateTime(toYear, 12, 31));
                if (gap <= (criteria.GapLongerThanDays ?? 0))
                    continue;
            }

            if (criteria.StayDaysFrom != null || criteria.StayDaysTo != null)
            {
                var year = criteria.StayYear ?? criteria.VisaYearFrom ?? today.Year;
                var days = CoveredDays(spans, new DateTime(year, 1, 1), new DateTime(year, 12, 31));
                if (criteria.StayDaysFrom is int fromDays && days < fromDays)
                    continue;
                if (criteria.StayDaysTo is int toDays && days > toDays)
                    continue;
            }

            matched.Add(id);
        }

        return matched;
    }

    private static HashSet<Guid> PermitLocationFacts(IObjectSpace objectSpace, List<Guid> ids, EmployeeSearchCriteria criteria)
    {
        var labels = criteria.PermitLocations.Select(FoldedNeedle).Where(label => label.Length > 0).ToList();
        var rows = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(item => item.Person != null && ids.Contains(item.Person.ID))
            .Select(item => new { PersonId = item.Person.ID, item.WorkPermittedLocations })
            .ToList();
        var matched = new HashSet<Guid>();
        foreach (var row in rows)
        {
            var text = FoldedNeedle(row.WorkPermittedLocations);
            if (labels.Any(label => text.Contains(label, StringComparison.Ordinal)))
                matched.Add(row.PersonId);
        }

        return matched;
    }

    private static bool InRange(int year, int? from, int? to) =>
        (from == null || year >= from) && (to == null || year <= to);

    private static int MaxGap(List<(DateTime StartDate, DateTime End)> spans, DateTime windowStart, DateTime windowEnd)
    {
        var clipped = spans
            .Select(span => (Start: span.StartDate < windowStart ? windowStart : span.StartDate, End: span.End > windowEnd ? windowEnd : span.End))
            .Where(span => span.Start <= span.End)
            .OrderBy(span => span.Start)
            .ToList();
        if (clipped.Count == 0)
            return (windowEnd - windowStart).Days + 1;

        var merged = new List<(DateTime Start, DateTime End)> { clipped[0] };
        for (var i = 1; i < clipped.Count; i++)
        {
            var last = merged[^1];
            if (clipped[i].Start <= last.End.AddDays(1))
                merged[^1] = (last.Start, clipped[i].End > last.End ? clipped[i].End : last.End);
            else
                merged.Add(clipped[i]);
        }

        var gap = merged[0].Start > windowStart ? (merged[0].Start - windowStart).Days : 0;
        for (var i = 1; i < merged.Count; i++)
        {
            var between = (merged[i].Start - merged[i - 1].End).Days - 1;
            if (between > gap)
                gap = between;
        }

        var tail = merged[^1].End < windowEnd ? (windowEnd - merged[^1].End).Days : 0;
        return tail > gap ? tail : gap;
    }

    private static int CoveredDays(List<(DateTime StartDate, DateTime End)> spans, DateTime windowStart, DateTime windowEnd)
    {
        var clipped = spans
            .Select(span => (Start: span.StartDate < windowStart ? windowStart : span.StartDate, End: span.End > windowEnd ? windowEnd : span.End))
            .Where(span => span.Start <= span.End)
            .OrderBy(span => span.Start)
            .ToList();
        if (clipped.Count == 0)
            return 0;
        var days = 0;
        var current = clipped[0];
        for (var i = 1; i < clipped.Count; i++)
        {
            if (clipped[i].Start <= current.End.AddDays(1))
                current = (current.Start, clipped[i].End > current.End ? clipped[i].End : current.End);
            else
            {
                days += (current.End - current.Start).Days + 1;
                current = clipped[i];
            }
        }

        days += (current.End - current.Start).Days + 1;
        return days;
    }

    private static bool TryMoney(string value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var text = value.Trim().Replace(" ", string.Empty);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }

    private static bool Gap(IReadOnlyList<string> gaps, string token) =>
        gaps.Any(gap => FoldedNeedle(gap).Contains(token, StringComparison.Ordinal));

    private static string Lower(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
}
