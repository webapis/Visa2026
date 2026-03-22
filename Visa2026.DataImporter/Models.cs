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
