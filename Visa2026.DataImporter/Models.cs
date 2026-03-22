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

public enum ApplicationLifecycleStage
{
    Entry,
    Stay,
    Exit
}

public enum ApplicationTypeCategory
{
    Employee,
    FamilyMember,
    Both
}

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
}

public class Application
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("ApplicationNumber")]
    public string ApplicationNumber { get; set; } = "";

    [JsonPropertyName("AppNumberPrefix")]
    public string AppNumberPrefix { get; set; } = "";

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

    [JsonPropertyName("Urgency")]
    public Urgency? Urgency { get; set; }

    [JsonPropertyName("VisaPeriod")]
    public VisaPeriod? VisaPeriod { get; set; }

    [JsonPropertyName("VisaType")]
    public VisaType? VisaType { get; set; }

    [JsonPropertyName("BusinessTripPlan")]
    public BusinessTripPlan? BusinessTripPlan { get; set; }

    [JsonPropertyName("Company")]
    public Company? Company { get; set; }

    [JsonPropertyName("Registrations")]
    public List<Registration> Registrations { get; set; } = new();

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

    [JsonPropertyName("CurrentPositionHistory")]
    public EmployeePositionHistory? CurrentPositionHistory { get; set; }

    [JsonPropertyName("CurrentRegistration")]
    public Registration? CurrentRegistration { get; set; }

    [JsonPropertyName("CurrentEmployeeContract")]
    public EmployeeContract? CurrentEmployeeContract { get; set; }

    [JsonPropertyName("CurrentWorkPermitItem")]
    public WorkPermitItem? CurrentWorkPermitItem { get; set; }

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

public enum BusinessTripStatus
{
    Planned,
    Ongoing,
    Completed,
    Cancelled
}

public class BusinessTrip
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Purpose")]
    public string Purpose { get; set; } = "";

    [JsonPropertyName("DestinationCountry")]
    public Country? DestinationCountry { get; set; }

    [JsonPropertyName("DestinationCity")]
    public string DestinationCity { get; set; } = "";

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("EndDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("Status")]
    public BusinessTripStatus Status { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

    [JsonPropertyName("Address")]
    public BusinessTripAddress? Address { get; set; }
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

public class Company
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("Address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("PhoneNumber")]
    public string PhoneNumber { get; set; } = "";

    [JsonPropertyName("Email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("TaxInformation")]
    public string TaxInformation { get; set; } = "";

    [JsonPropertyName("AppNumberPrefix")]
    public string AppNumberPrefix { get; set; } = "";

    [JsonPropertyName("ApplicationNumberPadding")]
    public int ApplicationNumberPadding { get; set; }

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("CurrentAuthorizedSignatory")]
    public CompanyHead? CurrentAuthorizedSignatory { get; set; }

    [JsonPropertyName("CurrentRepresentative")]
    public Representative? CurrentRepresentative { get; set; }

    [JsonPropertyName("Representatives")]
    public List<Representative> Representatives { get; set; } = new();

    [JsonPropertyName("ProjectContracts")]
    public List<ProjectContract> ProjectContracts { get; set; } = new();
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

    [JsonPropertyName("Company")]
    public Company? Company { get; set; }
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

    [JsonPropertyName("HasBorderZonePermit")]
    public bool HasBorderZonePermit { get; set; }

    [JsonPropertyName("BorderZoneLocations")]
    public List<City> BorderZoneLocations { get; set; } = new();

    [JsonPropertyName("HasInvitation")]
    public bool HasInvitation { get; set; }

    [JsonPropertyName("Invitation")]
    public Invitation? Invitation { get; set; }

    [JsonPropertyName("Passport")]
    public Passport? Passport { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

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

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

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

    [JsonPropertyName("Company")]
    public Company? Company { get; set; }

    [JsonPropertyName("IsSubcontractorEmployee")]
    public bool IsSubcontractorEmployee { get; set; }

    [JsonPropertyName("CurrentWorkPermitItem")]
    public WorkPermitItem? CurrentWorkPermitItem { get; set; }

    [JsonPropertyName("CurrentPassport")]
    public Passport? CurrentPassport { get; set; }

    [JsonPropertyName("CurrentVisa")]
    public Visa? CurrentVisa { get; set; }

    [JsonPropertyName("CurrentInvitationItem")]
    public InvitationItem? CurrentInvitationItem { get; set; }

    [JsonPropertyName("CurrentMedicalRecord")]
    public MedicalRecord? CurrentMedicalRecord { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

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
    public DateTime HireDate { get; set; }

    [JsonPropertyName("SponsoringEmployee")]
    public Person? SponsoringEmployee { get; set; }

    [JsonPropertyName("Relationship")]
    public Relationship? Relationship { get; set; }

    [JsonPropertyName("CurrentVisa")]
    public Visa? CurrentVisa { get; set; }

    [JsonPropertyName("CurrentAddressOfResidence")]
    public AddressOfResidence? CurrentAddressOfResidence { get; set; }

    [JsonPropertyName("CurrentBusinessTrip")]
    public BusinessTrip? CurrentBusinessTrip { get; set; }

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

    [JsonPropertyName("CurrentRegistration")]
    public Registration? CurrentRegistration { get; set; }

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

    [JsonPropertyName("Registrations")]
    public List<Registration> Registrations { get; set; } = new();

    [JsonPropertyName("RejectionItems")]
    public List<RejectionItem> RejectionItems { get; set; } = new();

    [JsonPropertyName("Passports")]
    public List<Passport> Passports { get; set; } = new();

    [JsonPropertyName("MedicalRecords")]
    public List<MedicalRecord> MedicalRecords { get; set; } = new();

    [JsonPropertyName("FamilyMembers")]
    public List<Person> FamilyMembers { get; set; } = new();

    [JsonPropertyName("BusinessTrips")]
    public List<BusinessTrip> BusinessTrips { get; set; } = new();

    [JsonPropertyName("PositionHistory")]
    public List<EmployeePositionHistory> PositionHistory { get; set; } = new();
}

public class LocalEmployee
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = "";

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";
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

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

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

    [JsonPropertyName("Cities")]
    public List<City> Cities { get; set; } = new();

    [JsonPropertyName("IsCancelled")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("IsChanged")]
    public bool IsChanged { get; set; }

    [JsonPropertyName("IsExtended")]
    public bool IsExtended { get; set; }
}

public enum TravelType
{
    Internal,
    External
}

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

    [JsonPropertyName("FromLocation")]
    public string FromLocation { get; set; } = "";

    [JsonPropertyName("ToLocation")]
    public string ToLocation { get; set; } = "";

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

public class Representative
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Company")]
    public Company? Company { get; set; }

    [JsonPropertyName("IsLocalEmployee")]
    public bool IsLocalEmployee { get; set; }

    [JsonPropertyName("LocalEmployee")]
    public LocalEmployee? LocalEmployee { get; set; }

    [JsonPropertyName("Employee")]
    public Person? Employee { get; set; }

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
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

public class Registration
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Person")]
    public Person? Person { get; set; }

    [JsonPropertyName("RegistrationDate")]
    public DateTime RegistrationDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("RegistrationNumber")]
    public string RegistrationNumber { get; set; } = "";

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }
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

    [JsonPropertyName("Company")]
    public Company? Company { get; set; }

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "";
}

public class CompanyHead
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("IsLocalEmployee")]
    public bool IsLocalEmployee { get; set; }

    [JsonPropertyName("LocalEmployee")]
    public LocalEmployee? LocalEmployee { get; set; }

    [JsonPropertyName("Employee")]
    public Person? Employee { get; set; }

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("Position")]
    public Position? Position { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}
