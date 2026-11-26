using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.EmployeeSearch;

public sealed class EmployeeSearchLookupOption
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public sealed class EmployeeSearchOptions
{
    public IReadOnlyList<EmployeeSearchLookupOption> Genders { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Countries { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Departments { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Positions { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Contracts { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Specialties { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> EducationLevels { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Cities { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> MaritalStatuses { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Relationships { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> ActualPositions { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Institutions { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> PassportTypes { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> VisaTypes { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> VisaCategories { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> VisaIssuedPlaces { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> VisaPeriods { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Subcontractors { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Regions { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Lodgings { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Hotels { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> Hospitals { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> OtherSites { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
    public IReadOnlyList<EmployeeSearchLookupOption> WorkLocations { get; set; } = Array.Empty<EmployeeSearchLookupOption>();
}

public sealed class EmployeeSearchCriteria
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<Guid> GenderIds { get; set; } = new();
    public int? AgeFrom { get; set; }
    public int? AgeTo { get; set; }
    public List<Guid> NationalityIds { get; set; } = new();
    public List<Guid> DepartmentIds { get; set; } = new();
    public List<Guid> PositionIds { get; set; } = new();
    public bool PositionAnyTime { get; set; } = true;
    public int? VisaYearFrom { get; set; }
    public int? VisaYearTo { get; set; }
    public bool StillValidToday { get; set; }
    public List<Guid> ProjectContractIds { get; set; } = new();
    public List<Guid> SpecialtyIds { get; set; } = new();
    public List<Guid> EducationCountryIds { get; set; } = new();
    public List<Guid> EducationLevelIds { get; set; } = new();
    public int? GraduationYearFrom { get; set; }
    public int? GraduationYearTo { get; set; }
    public string PassportNumber { get; set; } = string.Empty;
    public string InvitationNumber { get; set; } = string.Empty;
    public string WorkPermitNumber { get; set; } = string.Empty;
    public List<Guid> CityIds { get; set; } = new();
    public bool? RequireMedical { get; set; }
    public bool? RequireFamily { get; set; }
    public bool IncludeArchived { get; set; }
    public bool ArchivedOnly { get; set; }

    public string MiddleName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool? HasPhoto { get; set; }
    public bool? Incomplete { get; set; }
    public List<string> IncompleteGaps { get; set; } = new();

    public int? AgeAsOfYear { get; set; }
    public DateTime? BirthFrom { get; set; }
    public DateTime? BirthTo { get; set; }
    public int? BirthYearFrom { get; set; }
    public int? BirthYearTo { get; set; }
    public List<Guid> MaritalStatusIds { get; set; } = new();
    public List<Guid> RelationshipIds { get; set; } = new();
    public int? FamilyCountFrom { get; set; }
    public int? FamilyCountTo { get; set; }
    public bool? FamilyVisa { get; set; }
    public int? FamilyVisaYear { get; set; }
    public string FamilyText { get; set; } = string.Empty;
    public bool? FamilyTextEmpty { get; set; }

    public List<Guid> BirthCountryIds { get; set; } = new();
    public string BirthPlace { get; set; } = string.Empty;
    public bool? CitizenshipDiffersFromBirth { get; set; }
    public bool? PassportFromOtherCountry { get; set; }
    public List<Guid> ForeignCountryIds { get; set; } = new();
    public string ForeignAddress { get; set; } = string.Empty;

    public List<Guid> ActualPositionIds { get; set; } = new();
    public bool PositionLeft { get; set; }
    public int? PositionLeftYear { get; set; }
    public int? PositionYearFrom { get; set; }
    public int? PositionYearTo { get; set; }
    public DateTime? HireFrom { get; set; }
    public DateTime? HireTo { get; set; }
    public int? YearsFrom { get; set; }
    public int? YearsTo { get; set; }
    public int? ChangedDepartmentYear { get; set; }
    public int? ChangedPositionYear { get; set; }
    public bool? SeveralPositions { get; set; }
    public int? SeveralPositionsYear { get; set; }
    public string PreviousWorkplaces { get; set; } = string.Empty;

    public int? ContractYearFrom { get; set; }
    public int? ContractYearTo { get; set; }
    public bool? ContractActive { get; set; }
    public string ContractDescription { get; set; } = string.Empty;
    public bool? SeveralContracts { get; set; }
    public List<Guid> SubcontractorIds { get; set; } = new();

    public decimal? SalaryFrom { get; set; }
    public decimal? SalaryTo { get; set; }
    public int? SalaryYearFrom { get; set; }
    public int? SalaryYearTo { get; set; }
    public bool? SalaryChanged { get; set; }
    public int? SalaryChangedYear { get; set; }

    public List<Guid> InstitutionIds { get; set; } = new();
    public bool? HasEducation { get; set; }
    public bool? EducatedOutsideCitizenship { get; set; }
    public bool? SpecialtyDiffers { get; set; }
    public bool? DiplomaOnFile { get; set; }

    public DateTime? VisaFrom { get; set; }
    public DateTime? VisaTo { get; set; }
    public DateTime? VisaOn { get; set; }
    public int? ExpiringWithinDays { get; set; }
    public bool? VisaExpired { get; set; }
    public int? VisaExpiredYear { get; set; }
    public bool? NeverHadVisa { get; set; }
    public int? FirstVisaYearFrom { get; set; }
    public int? FirstVisaYearTo { get; set; }
    public int? LastVisaYearFrom { get; set; }
    public int? LastVisaYearTo { get; set; }
    public int? CoveredYearFrom { get; set; }
    public int? CoveredYearTo { get; set; }
    public int? AllowedGapDays { get; set; }
    public bool? HadGap { get; set; }
    public int? GapLongerThanDays { get; set; }
    public string VisaNumber { get; set; } = string.Empty;
    public List<Guid> VisaTypeIds { get; set; } = new();
    public List<Guid> VisaCategoryIds { get; set; } = new();
    public List<Guid> VisaIssuedPlaceIds { get; set; } = new();
    public bool? VisaExtended { get; set; }
    public bool? ExtensionRequired { get; set; }
    public string RegistrationState { get; set; } = string.Empty;
    public string VisaBorderZone { get; set; } = string.Empty;
    public int? StayDaysFrom { get; set; }
    public int? StayDaysTo { get; set; }
    public int? StayYear { get; set; }

    public bool PassportCurrentOnly { get; set; }
    public List<Guid> PassportTypeIds { get; set; } = new();
    public List<Guid> PassportCountryIds { get; set; } = new();
    public DateTime? PassportIssuedFrom { get; set; }
    public DateTime? PassportIssuedTo { get; set; }
    public DateTime? PassportExpiresFrom { get; set; }
    public DateTime? PassportExpiresTo { get; set; }
    public bool? PassportExpired { get; set; }
    public int? PassportExpiringDays { get; set; }
    public bool? PassportCancelled { get; set; }
    public bool? SeveralPassports { get; set; }

    public string InvitationMode { get; set; } = string.Empty;
    public int? InvitationYearFrom { get; set; }
    public int? InvitationYearTo { get; set; }
    public List<Guid> InvitationCategoryIds { get; set; } = new();
    public List<Guid> InvitationPeriodIds { get; set; } = new();
    public DateTime? InvitationValidOn { get; set; }
    public bool? InvitationExpiredNoVisa { get; set; }
    public bool? InvitationCancelled { get; set; }

    public string PermitMode { get; set; } = string.Empty;
    public int? PermitYearFrom { get; set; }
    public int? PermitYearTo { get; set; }
    public string AsNumber { get; set; } = string.Empty;
    public List<string> PermitLocations { get; set; } = new();
    public List<Guid> PermitPositionIds { get; set; } = new();
    public bool? PermitExtended { get; set; }

    public List<string> ResidenceTypes { get; set; } = new();
    public List<Guid> RegionIds { get; set; } = new();
    public List<Guid> LodgingIds { get; set; } = new();
    public List<Guid> HotelIds { get; set; } = new();
    public List<Guid> HospitalIds { get; set; } = new();
    public List<Guid> OtherSiteIds { get; set; } = new();
    public string AddressText { get; set; } = string.Empty;
    public bool? AddressExpiredDuringVisa { get; set; }

    public string BorderZoneMode { get; set; } = string.Empty;
    public int? BorderZoneYearFrom { get; set; }
    public int? BorderZoneYearTo { get; set; }
    public string BorderZoneNumber { get; set; } = string.Empty;
    public string BorderZonePlace { get; set; } = string.Empty;
    public bool? WasRejected { get; set; }
    public int? RejectedYear { get; set; }
    public string MedicalNumber { get; set; } = string.Empty;
    public int? MedicalYearFrom { get; set; }
    public int? MedicalYearTo { get; set; }
    public DateTime? MedicalValidOn { get; set; }
    public bool? MedicalExpiredDuringVisa { get; set; }
}

public sealed class EmployeeSearchMatch
{
    public string VisaLine { get; set; } = string.Empty;
    public string PositionLine { get; set; } = string.Empty;
    public string EarlierPositionLine { get; set; } = string.Empty;
    public string ContractLine { get; set; } = string.Empty;
    public string EducationLine { get; set; } = string.Empty;
    public string EducationNote { get; set; } = string.Empty;
    public string MoreLine { get; set; } = string.Empty;
}

public sealed class EmployeeSearchRow
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AgeText { get; set; } = string.Empty;
    public string Citizenship { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string VisaStatus { get; set; } = "None";
    public string Contract { get; set; } = string.Empty;
    public IReadOnlyList<string> Pills { get; set; } = Array.Empty<string>();
    public EmployeeSearchMatch Detail { get; set; } = new();
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class EmployeeSearchResult
{
    public int Total { get; set; }
    public bool Truncated { get; set; }
    public IReadOnlyList<EmployeeSearchRow> Rows { get; set; } = Array.Empty<EmployeeSearchRow>();
}
