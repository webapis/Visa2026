using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

// -----------------------------------------------------------------------
// Mirror of Visa2026.Module.BusinessObjects.VisaType
// Only include the properties you need — OData ignores unknown ones.
// -----------------------------------------------------------------------
public class VisaType
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }

    public override string ToString() =>
        $"[{Id}] Name={Name}, Code={Code}, IsDefault={IsDefault}";
}

// -----------------------------------------------------------------------
// Add more entity models here as you expose them in Startup.cs, e.g.:
// public class VisaApplication { ... }
// -----------------------------------------------------------------------

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationLifecycleStage
{
    Entry,
    Stay,
    Exit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationTypeCategory
{
    Employee,
    FamilyMember,
    Both
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Office = 0,
    ToMinistry = 1,
    Processed = 2
}

public class ApplicationType
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }

    [JsonPropertyName("LifecycleStage")]
    public ApplicationLifecycleStage LifecycleStage { get; set; }

    [JsonPropertyName("Category")]
    public ApplicationTypeCategory Category { get; set; }

    [JsonPropertyName("DurationInDays")]
    public int DurationInDays { get; set; }

    [JsonPropertyName("ApplicationTypeFilter")]
    public ApplicationTypeFilter? ApplicationTypeFilter { get; set; }
}

public class ApplicationTypeFilter
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("Category")]
    public ApplicationTypeCategory Category { get; set; }
}

public class Application
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("ApplicationNumber")]
    public string ApplicationNumber { get; set; } = "";

    [JsonPropertyName("AppNumberPrefix")]
    public string AppNumberPrefix { get; set; } = "";

    [JsonPropertyName("FullApplicationNumber")]
    public string FullApplicationNumber { get; set; } = "";

    [JsonPropertyName("Year")]
    public int Year { get; set; }

    [JsonPropertyName("ApplicationDate")]
    public DateTime ApplicationDate { get; set; }

    [JsonPropertyName("Category")]
    public ApplicationTypeCategory Category { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("ApplicationType")]
    public ApplicationType? ApplicationType { get; set; }

    [JsonPropertyName("ApplicationTypeFilter")]
    public ApplicationTypeFilter? ApplicationTypeFilter { get; set; }

    [JsonPropertyName("VisaCategory")]
    public VisaCategory? VisaCategory { get; set; }

    [JsonPropertyName("MigrationService")]
    public MigrationService? MigrationService { get; set; }

    [JsonPropertyName("Urgency")]
    public Urgency? Urgency { get; set; }

    [JsonPropertyName("VisaPeriod")]
    public VisaPeriod? VisaPeriod { get; set; }

    [JsonPropertyName("VisaType")]
    public VisaType? VisaType { get; set; }

    [JsonPropertyName("BusinessTripPlan")]
    public BusinessTripPlan? BusinessTripPlan { get; set; }

    [JsonPropertyName("ProjectContract")]
    public ProjectContract? ProjectContract { get; set; }

    [JsonPropertyName("FromCity")]
    public City? FromCity { get; set; }

    [JsonPropertyName("ToCity")]
    public City? ToCity { get; set; }

    [JsonPropertyName("MovementPermitLocation")]
    public MovementPermitLocation? MovementPermitLocation { get; set; }

    [JsonPropertyName("BorderZoneLocation")]
    public BorderZoneLocation? BorderZoneLocation { get; set; }

    [JsonPropertyName("Rejections")]
    public List<Rejection> Rejections { get; set; } = new();

    [JsonPropertyName("WorkPermits")]
    public List<WorkPermit> WorkPermits { get; set; } = new();
}

public class ApplicationItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("CurrentPassport")]
    public Passport? CurrentPassport { get; set; }

    [JsonPropertyName("PreviousPassport")]
    public Passport? PreviousPassport { get; set; }

    [JsonPropertyName("CurrentVisa")]
    public Visa? CurrentVisa { get; set; }

    [JsonPropertyName("CurrentPositionHistory")]
    public EmployeePositionHistory? CurrentPositionHistory { get; set; }

    [JsonPropertyName("RegistrationDate")]
    public DateTime? RegistrationDate { get; set; }

    [JsonPropertyName("TravelDate")]
    public DateTime? TravelDate { get; set; }

    [JsonPropertyName("TravelType")]
    public TravelType? TravelType { get; set; }

    [JsonPropertyName("MovementType")]
    public MovementType? MovementType { get; set; }

    [JsonPropertyName("CheckPoint")]
    public CheckPoint? CheckPoint { get; set; }

    [JsonPropertyName("PurposeOfTravel")]
    public PurposeOfTravel? PurposeOfTravel { get; set; }

    [JsonPropertyName("TravelNotes")]
    public string? TravelNotes { get; set; }

    [JsonPropertyName("BusinessTripAddress")]
    public BusinessTripAddress? BusinessTripAddress { get; set; }

    [JsonPropertyName("CurrentEmployeeContract")]
    public EmployeeContract? CurrentEmployeeContract { get; set; }

    [JsonPropertyName("CurrentWorkPermitItem")]
    public WorkPermitItem? CurrentWorkPermitItem { get; set; }

    [JsonPropertyName("PreviousWorkPermitItem")]
    public WorkPermitItem? PreviousWorkPermitItem { get; set; }

    [JsonPropertyName("CurrentInvitationItem")]
    public InvitationItem? CurrentInvitationItem { get; set; }

    [JsonPropertyName("CurrentAddressOfResidence")]
    public AddressOfResidence? CurrentAddressOfResidence { get; set; }

    [JsonPropertyName("CurrentMedicalRecord")]
    public MedicalRecord? CurrentMedicalRecord { get; set; }

    [JsonPropertyName("CurrentEducation")]
    public Education? CurrentEducation { get; set; }

    [JsonPropertyName("InvitationItemIsIssued")]
    public bool InvitationItemIsIssued { get; set; }

    [JsonPropertyName("WorkPermitItemIsIssued")]
    public bool WorkPermitItemIsIssued { get; set; }

    [JsonPropertyName("RejectionIssued")]
    public bool RejectionIssued { get; set; }

    [JsonPropertyName("VisaIssued")]
    public bool VisaIssued { get; set; }

    [JsonPropertyName("InvitationItemIsCancelled")]
    public bool InvitationItemIsCancelled { get; set; }

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("InvitationItemIsChanged")]
    public bool InvitationItemIsChanged { get; set; }

    [JsonPropertyName("WorkPermitItemIsChanged")]
    public bool WorkPermitItemIsChanged { get; set; }

    [JsonPropertyName("VisaIsCancelled")]
    public bool VisaIsCancelled { get; set; }

    [JsonPropertyName("VisaIsChanged")]
    public bool VisaIsChanged { get; set; }

    [JsonPropertyName("ApplicationItemsIsCancelled")]
    public bool ApplicationItemsIsCancelled { get; set; }

    [JsonPropertyName("ApplicationItemName")]
    public string ApplicationItemName { get; set; } = "";
}

public class ApplicationLocation
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class ApplicationState
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class ApplicationProgress
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("State")]
    public ApplicationState? State { get; set; }

    [JsonPropertyName("Location")]
    public ApplicationLocation? Location { get; set; }

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; } = "";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResidenceType
{
    Lodging,
    Hotel,
    PrivateHouse
}

public class AddressOfResidence
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Type")]
    public ResidenceType? Type { get; set; }

    [JsonPropertyName("Lodging")]
    public Lodging? Lodging { get; set; }

    [JsonPropertyName("FullAddress")]
    public string FullAddress { get; set; } = "";

    [JsonPropertyName("Region")]
    public Region? Region { get; set; }

    [JsonPropertyName("City")]
    public City? City { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }
}

public class City
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("Region")]
    public Region? Region { get; set; }

    [JsonPropertyName("RegionName")]
    public string RegionName { get; set; } = "";

    [JsonPropertyName("PdfForm_Code")]
    public string PdfFormCode { get; set; } = "";
}

public class BusinessTripAddress
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("City")]
    public City? City { get; set; }

    [JsonPropertyName("FullAddress")]
    public string FullAddress { get; set; } = "";
}

public class CheckPoint
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class BusinessTripPlan
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("EndDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("Region")]
    public Region? Region { get; set; }

    [JsonPropertyName("City")]
    public City? City { get; set; }
}

public class Ministry
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("RecipientBlock")]
    public string RecipientBlock { get; set; } = "";

    [JsonPropertyName("FormOfAddress")]
    public string FormOfAddress { get; set; } = "";
}

public class CompanyProfileDto
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("Address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("PhoneNumber")]
    public string PhoneNumber { get; set; } = "";

    [JsonPropertyName("Email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("TaxInformation")]
    public string TaxInformation { get; set; } = "";
}

public class AuthorizedSignatoryDto
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("PositionTitleTm")]
    public string PositionTitleTm { get; set; } = "";

    [JsonPropertyName("PassportNumber")]
    public string PassportNumber { get; set; } = "";

    [JsonPropertyName("PassportAuthority")]
    public string PassportAuthority { get; set; } = "";

    [JsonPropertyName("PassportIssueDate")]
    public DateTime? PassportIssueDate { get; set; }
}

public class AuthorizedRepresentativeDto
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("PositionTitleTm")]
    public string PositionTitleTm { get; set; } = "";

    [JsonPropertyName("Phone")]
    public string Phone { get; set; } = "";

    [JsonPropertyName("PassportNumber")]
    public string PassportNumber { get; set; } = "";

    [JsonPropertyName("PassportAuthority")]
    public string PassportAuthority { get; set; } = "";

    [JsonPropertyName("PassportIssueDate")]
    public DateTime? PassportIssueDate { get; set; }
}

public class SystemSettingsDto
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("AppNumberPrefix")]
    public string AppNumberPrefix { get; set; } = "";

    [JsonPropertyName("AppNumberFormat")]
    public string AppNumberFormat { get; set; } = "";

    [JsonPropertyName("ApplicationNumberSeed")]
    public int ApplicationNumberSeed { get; set; }

    [JsonPropertyName("ApplicationNumberPadding")]
    public int ApplicationNumberPadding { get; set; }
}

public class Country
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Department
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Education
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("EducationLevel")]
    public EducationLevel? EducationLevel { get; set; }

    [JsonPropertyName("EducationInstitution")]
    public EducationInstitution? EducationInstitution { get; set; }

    [JsonPropertyName("EducationCountry")]
    public Country? EducationCountry { get; set; }

    [JsonPropertyName("Specialty")]
    public Specialty? Specialty { get; set; }

    [JsonPropertyName("GraduationYear")]
    public int? GraduationYear { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }
}

public class EmployeeContract
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("ContractStartDate")]
    public DateTime ContractStartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("ValidityDuration")]
    public ValidityDuration? ValidityDuration { get; set; }

    [JsonPropertyName("PositionHistory")]
    public EmployeePositionHistory? PositionHistory { get; set; }

    [JsonPropertyName("Salary")]
    public decimal Salary { get; set; }
}

public class EmployeePositionHistory
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("EndDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("Position")]
    public Position? Position { get; set; }

    [JsonPropertyName("Department")]
    public Department? Department { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }
}

public class EducationInstitution
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class EducationLevel
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("TestProperty")]
    public string TestProperty { get; set; } = "";

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }
}

public class Gender
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }
}

public class MaritalStatus
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }
}

public class MigrationService
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class MovementPermitLocation
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class BorderZoneLocation
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class PassportType
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public string PdfFormCode { get; set; } = "";
}

public class Position
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class PurposeOfTravel
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Region
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public string PdfFormCode { get; set; } = "";
}

public class Relationship
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("ReverseNameTm")]
    public string ReverseNameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Specialty
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Subcontractor
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("ContactPerson")]
    public string ContactPerson { get; set; } = "";

    [JsonPropertyName("PhoneNumber")]
    public string PhoneNumber { get; set; } = "";
}

public class Urgency
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }
}

public class ValidityDuration
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("NumberOfDays")]
    public int NumberOfDays { get; set; }
}

public class VisaIssuedPlace
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class VisaPeriod
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm__Code")]
    public string PdfFormUnitCode { get; set; } = "";

    [JsonPropertyName("PdfForm_Count")]
    public int PdfFormCount { get; set; }
}

public class VisaCategory
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }
}

public class ProjectContract
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("Description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
}

public class Invitation
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("InvitationNumber")]
    public string InvitationNumber { get; set; } = "";

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("IsChanged")]
    public bool IsChanged { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("ValidityDuration")]
    public ValidityDuration? ValidityDuration { get; set; }

    [JsonPropertyName("InvitationItems")]
    public List<InvitationItem> InvitationItems { get; set; } = new();
}

public class InvitationItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Invitation")]
    public Invitation? Invitation { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("Passport")]
    public Passport? Passport { get; set; }

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("IsChanged")]
    public bool IsChanged { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("IsUsed")]
    public bool IsUsed { get; set; }
}

public class Visa
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("VisaNumber")]
    public string VisaNumber { get; set; } = "";

    [JsonPropertyName("VisaType")]
    public VisaType? VisaType { get; set; }

    [JsonPropertyName("VisaCategory")]
    public VisaCategory? VisaCategory { get; set; }

    [JsonPropertyName("VisaIssuedPlace")]
    public VisaIssuedPlace? VisaIssuedPlace { get; set; }

    [JsonPropertyName("IssueDate")]
    public DateTime IssueDate { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("BorderZoneLocation")]
    public string? BorderZoneLocation { get; set; }

    [JsonPropertyName("HasInvitation")]
    public bool HasInvitation { get; set; }

    [JsonPropertyName("Invitation")]
    public Invitation? Invitation { get; set; }

    [JsonPropertyName("Passport")]
    public Passport? Passport { get; set; }

    [JsonPropertyName("IssuingApplicationItem")]
    public ApplicationItem? IssuingApplicationItem { get; set; }

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "";

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("IsChanged")]
    public bool IsChanged { get; set; }

    [JsonPropertyName("IsExtended")]
    public bool IsExtended { get; set; }
}

public class Person
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = "";

    private string? _fullName;
    [JsonPropertyName("FullName")]
    public string FullName 
    { 
        get => string.IsNullOrWhiteSpace(_fullName) ? $"{FirstName} {LastName}".Trim() : _fullName;
        set => _fullName = value; 
    }

    [JsonPropertyName("MiddleName")]
    public string MiddleName { get; set; } = "";

    [JsonPropertyName("DateOfBirth")]
    public DateTime DateOfBirth { get; set; }

    [JsonPropertyName("BirthPlace")]
    public string BirthPlace { get; set; } = "";

    [JsonPropertyName("CountryOfBirth")]
    public Country? CountryOfBirth { get; set; }

    [JsonPropertyName("Gender")]
    public Gender? Gender { get; set; }

    [JsonPropertyName("MaritalStatus")]
    public MaritalStatus? MaritalStatus { get; set; }

    [JsonPropertyName("Nationality")]
    public Country? Nationality { get; set; }

    [JsonPropertyName("ForeignAddress")]
    public string ForeignAddress { get; set; } = "";

    [JsonPropertyName("ForeignAddressCountry")]
    public Country? ForeignAddressCountry { get; set; }

    [JsonPropertyName("ProjectContract")]
    public ProjectContract? ProjectContract { get; set; }

    [JsonPropertyName("IsArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("IsEmployee")]
    public bool IsEmployee { get; set; }

    [JsonPropertyName("IsSubcontractorEmployee")]
    public bool IsSubcontractorEmployee { get; set; }

    [JsonPropertyName("CurrentWorkPermitItem")]
    public WorkPermitItem? CurrentWorkPermitItem { get; set; }

    [JsonPropertyName("VisaIssued")]
    public bool VisaIssued { get; set; }

    [JsonPropertyName("RejectionIssued")]
    public bool RejectionIssued { get; set; }

    [JsonPropertyName("WorkPermitItemIsIssued")]
    public bool WorkPermitItemIsIssued { get; set; }

    [JsonPropertyName("InvitationItemIsIssued")]
    public bool InvitationItemIsIssued { get; set; }


    [JsonPropertyName("Subcontractor")]
    public Subcontractor? Subcontractor { get; set; }

    [JsonPropertyName("Email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("HireDate")]
    public DateTime? HireDate { get; set; }

    [JsonPropertyName("SponsoringEmployee")]
    public Person? SponsoringEmployee { get; set; }

    [JsonPropertyName("Relationship")]
    public Relationship? Relationship { get; set; }

    [JsonPropertyName("CurrentVisa")]
    public Visa? CurrentVisa { get; set; }

    [JsonPropertyName("CurrentAddressOfResidence")]
    public AddressOfResidence? CurrentAddressOfResidence { get; set; }

    [JsonPropertyName("CurrentEducation")]
    public Education? CurrentEducation { get; set; }

    [JsonPropertyName("Educations")]
    public List<Education> Educations { get; set; } = new();

    [JsonPropertyName("CurrentEmployeeContract")]
    public EmployeeContract? CurrentEmployeeContract { get; set; }

    [JsonPropertyName("CurrentPositionHistory")]
    public EmployeePositionHistory? CurrentPositionHistory { get; set; }

    [JsonPropertyName("CurrentPassport")]
    public Passport? CurrentPassport { get; set; }

    [JsonPropertyName("CurrentMedicalRecord")]
    public MedicalRecord? CurrentMedicalRecord { get; set; }

    [JsonPropertyName("CurrentInvitationItem")]
    public InvitationItem? CurrentInvitationItem { get; set; }

    [JsonPropertyName("CurrentRejectionItem")]
    public RejectionItem? CurrentRejectionItem { get; set; }

    [JsonPropertyName("CurrentTravelHistory")]
    public TravelHistory? CurrentTravelHistory { get; set; }

    [JsonPropertyName("WorkPermitItems")]
    public List<WorkPermitItem> WorkPermitItems { get; set; } = new();

    [JsonPropertyName("AddressesOfResidence")]
    public List<AddressOfResidence> AddressesOfResidence { get; set; } = new();

    [JsonPropertyName("EmployeeContracts")]
    public List<EmployeeContract> EmployeeContracts { get; set; } = new();

    [JsonPropertyName("InvitationItems")]
    public List<InvitationItem> InvitationItems { get; set; } = new();

    [JsonPropertyName("RejectionItems")]
    public List<RejectionItem> RejectionItems { get; set; } = new();

    [JsonPropertyName("Passports")]
    public List<Passport> Passports { get; set; } = new();

    [JsonPropertyName("MedicalRecords")]
    public List<MedicalRecord> MedicalRecords { get; set; } = new();

    [JsonPropertyName("FamilyMembers")]
    public List<Person> FamilyMembers { get; set; } = new();

    [JsonPropertyName("PositionHistory")]
    public List<EmployeePositionHistory> PositionHistory { get; set; } = new();
}

public class MedicalRecord
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("DocumentNumber")]
    public string DocumentNumber { get; set; } = "";

    [JsonPropertyName("IssueDate")]
    public DateTime IssueDate { get; set; }

    [JsonPropertyName("ValidityDuration")]
    public ValidityDuration? ValidityDuration { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

public class WorkPermit
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("WorkPermitNumber")]
    public string WorkPermitNumber { get; set; } = "";

    [JsonPropertyName("IssuedDate")]
    public DateTime IssuedDate { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("WorkPermitItems")]
    public List<WorkPermitItem> WorkPermitItems { get; set; } = new();

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }
}

public class WorkPermitItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("Passport")]
    public Passport? Passport { get; set; }

    [JsonPropertyName("CurrentPositionHistory")]
    public EmployeePositionHistory? CurrentPositionHistory { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime ExpirationDate { get; set; }

    [JsonPropertyName("WorkPermitNumber")]
    public string WorkPermitNumber { get; set; } = "";

    [JsonPropertyName("ASNumber")]
    public string ASNumber { get; set; } = "";

    [JsonPropertyName("WorkPermit")]
    public WorkPermit? WorkPermit { get; set; }

    [JsonPropertyName("WorkPermitedCities")]
    public List<City> WorkPermitedCities { get; set; } = new();

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("IsChanged")]
    public bool IsChanged { get; set; }

    [JsonPropertyName("IsExtended")]
    public bool IsExtended { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelType
{
    Internal,
    External
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MovementType
{
    Entry,
    Exit
}

public class TravelHistory
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("TravelDate")]
    public DateTime TravelDate { get; set; }

    [JsonPropertyName("TravelType")]
    public TravelType? TravelType { get; set; }

    [JsonPropertyName("MovementType")]
    public MovementType? MovementType { get; set; }

    [JsonPropertyName("CheckPoint")]
    public CheckPoint? CheckPoint { get; set; }

    [JsonPropertyName("PurposeOfTravel")]
    public PurposeOfTravel? PurposeOfTravel { get; set; }

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "";
}

public class Rejection
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("RejectedDocNumber")]
    public string RejectedDocNumber { get; set; } = "";

    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("RejectionItems")]
    public List<RejectionItem> RejectionItems { get; set; } = new();
}

public class RejectionItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Rejection")]
    public Rejection? Rejection { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = "";
}

public class Passport
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("PassportNumber")]
    public string PassportNumber { get; set; } = "";

    [JsonPropertyName("PersonalNumber")]
    public string PersonalNumber { get; set; } = "";

    [JsonPropertyName("PassportType")]
    public PassportType? PassportType { get; set; }

    [JsonPropertyName("IssueDate")]
    public DateTime? IssueDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("Authority")]
    public string Authority { get; set; } = "";

    [JsonPropertyName("IssuedCountry")]
    public Country? IssuedCountry { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("CurrentVisa")]
    public Visa? CurrentVisa { get; set; }

    [JsonPropertyName("Visas")]
    public List<Visa> Visas { get; set; } = new();
}

public class Lodging
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("FullAddress")]
    public string FullAddress { get; set; } = "";

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "";
}

// -----------------------------------------------------------------------
// Scenario definition — parsed from the "Scenarios" sheet in data.xlsx.
// Controls which groups of rows are seeded and in what order.
// -----------------------------------------------------------------------
public class ScenarioDefinition
{
    /// <summary>Execution order. Lower numbers run first. "Shared" rows use Order=0.</summary>
    public int Order { get; set; }

    /// <summary>Scenario name — must match the "Scenario" column value in every data sheet.</summary>
    public string Name { get; set; } = "";

    /// <summary>Human-readable description of what this scenario seeds.</summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Optional: name of another scenario that must run before this one.
    /// Informational only — enforced by setting Order correctly.
    /// </summary>
    public string DependsOn { get; set; } = "";

    /// <summary>OData entity name used for idempotency check (e.g. "Person").</summary>
    public string AnchorEntity { get; set; } = "";

    /// <summary>OData property name to filter on (e.g. "Email").</summary>
    public string AnchorKey { get; set; } = "";

    /// <summary>Expected value — if a record with this value exists, the scenario is skipped.</summary>
    public string AnchorValue { get; set; } = "";

    public override string ToString() =>
        $"[{Order}] {Name}{(string.IsNullOrWhiteSpace(DependsOn) ? "" : $" (depends: {DependsOn})")}";
}