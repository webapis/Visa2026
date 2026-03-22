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
}

public class ApplicationItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Application")]
    public Application? Application { get; set; }

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

    [JsonPropertyName("FullAddress")]
    public string FullAddress { get; set; } = "";

    [JsonPropertyName("StartDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }
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
