using System.Collections.Generic;

namespace Visa2026.DataImporter;

// ---------------------------------------------------------------------------
// Column descriptor — one entry per Excel column header
// ---------------------------------------------------------------------------

public enum ColumnKind
{
    /// <summary>Plain string / number / bool / date — set directly on the payload.</summary>
    Scalar,
    /// <summary>Always parsed as boolean: "0"/"false"/"no" → false, anything else → true.</summary>
    Bool,
    /// <summary>Value is a Name resolved to a lookup record via the API. Sends { ID = guid }.</summary>
    LookupByName,
    /// <summary>Value is a FullName resolved to a Person record via the API. Sends { ID = guid }.</summary>
    PersonLookupByName,
}

public class ColumnMap
{
    /// <summary>Exact Excel column header (case-insensitive match).</summary>
    public string Header { get; init; } = "";
    /// <summary>Property name in the OData JSON payload.</summary>
    public string PayloadProperty { get; init; } = "";
    public ColumnKind Kind { get; init; } = ColumnKind.Scalar;
    /// <summary>For LookupByName: the OData entity to search.</summary>
    public string LookupEntity { get; init; } = "";
    /// <summary>If true and the cell is empty, the row is skipped entirely.</summary>
    public bool Required { get; init; } = false;
    /// <summary>
    /// Optional value substitution map applied before type parsing.
    /// Key = raw Excel cell value (case-insensitive), Value = replacement string sent to the API.
    /// Useful for mapping integer enum codes to their string names, e.g. "1" → "FamilyMember".
    /// </summary>
    public Dictionary<string, string>? ValueMap { get; init; } = null;
}

public class SheetMap
{
    /// <summary>Exact Excel sheet name (case-insensitive match).</summary>
    public string SheetName { get; init; } = "";
    /// <summary>OData entity name used in the API URL.</summary>
    public string EntityName { get; init; } = "";
    /// <summary>Human-readable label for console output.</summary>
    public string DisplayName { get; init; } = "";
    public List<ColumnMap> Columns { get; init; } = new();
}

public static class ExcelMappings
{
    // =======================================================================
    // LOOKUP / SEED SHEETS
    // Source file: lookup.xlsm
    // These sheets seed the reference/lookup tables in the database.
    // Import order matters: independent tables first, then dependent ones.
    // Column structure: _RowNum | ID | [internal cols] | Name | Code | ...
    // We map only the payload-relevant columns (skip _RowNum, GCRecord, etc.)
    // =======================================================================
    public static readonly List<SheetMap> LookupSheets = new()
    {
        // --- No dependencies ---

        new SheetMap { SheetName = "Country",          EntityName = "Country",          DisplayName = "Country",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "isDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Gender",           EntityName = "Gender",           DisplayName = "Gender",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "MaritalStatus",    EntityName = "MaritalStatus",    DisplayName = "Marital Status",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Urgency",          EntityName = "Urgency",          DisplayName = "Urgency",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaCategory",     EntityName = "VisaCategory",     DisplayName = "Visa Category",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaPeriod",       EntityName = "VisaPeriod",       DisplayName = "Visa Period",
            Columns = new() {
                new() { Header = "Name",           PayloadProperty = "Name",          Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",           PayloadProperty = "Code",          Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm__Code",  PayloadProperty = "PdfForm__Code",  Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Count",  PayloadProperty = "PdfForm_Count", Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",      PayloadProperty = "IsDefault",     Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaType",         EntityName = "VisaType",         DisplayName = "Visa Type",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "EducationLevel",   EntityName = "EducationLevel",   DisplayName = "Education Level",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Purpose of Travel",EntityName = "PurposeOfTravel",  DisplayName = "Purpose of Travel",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Checkpoint",       EntityName = "CheckPoint",       DisplayName = "Checkpoint",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "VisaIssuedPlace", EntityName = "VisaIssuedPlace",  DisplayName = "Visa Issued Place",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "PassportType",     EntityName = "PassportType",     DisplayName = "Passport Type",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Specialty",      EntityName = "Specialty",        DisplayName = "Specialty",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "EducInstitution", EntityName = "EducationInstitution", DisplayName = "Education Institution",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Relationships",    EntityName = "Relationship",     DisplayName = "Relationship",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "ApplicationLocation", EntityName = "ApplicationLocation", DisplayName = "Application Location",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Department",      EntityName = "Department",       DisplayName = "Department",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Position",        EntityName = "Position",         DisplayName = "Position",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Validation Duration", EntityName = "ValidityDuration", DisplayName = "Validity Duration",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "NumberOfDays", PayloadProperty = "NumberOfDays",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "ApplicationStates",EntityName = "ApplicationState", DisplayName = "Application State",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Region",           EntityName = "Region",           DisplayName = "Region",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Company",          EntityName = "Company",          DisplayName = "Company",
            Columns = new() {
                new() { Header = "Name",                    PayloadProperty = "Name",                    Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Address",                 PayloadProperty = "Address",                 Kind = ColumnKind.Scalar },
                new() { Header = "PhoneNumber",             PayloadProperty = "PhoneNumber",             Kind = ColumnKind.Scalar },
                new() { Header = "Email",                   PayloadProperty = "Email",                   Kind = ColumnKind.Scalar },
                new() { Header = "TaxInformation",          PayloadProperty = "TaxInformation",          Kind = ColumnKind.Scalar },
                new() { Header = "AppNumberPrefix",         PayloadProperty = "AppNumberPrefix",         Kind = ColumnKind.Scalar },
                new() { Header = "ApplicationNumberPadding",PayloadProperty = "ApplicationNumberPadding",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",               PayloadProperty = "IsDefault",               Kind = ColumnKind.Bool },
            }
        },

        new SheetMap { SheetName = "ApplicationType",  EntityName = "ApplicationType",  DisplayName = "Application Type",
            Columns = new() {
                new() { Header = "Name",           PayloadProperty = "Name",           Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",         PayloadProperty = "NameTm",         Kind = ColumnKind.Scalar },
                new() { Header = "Code",           PayloadProperty = "Code",           Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code",   PayloadProperty = "PdfForm_Code",   Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",      PayloadProperty = "IsDefault",      Kind = ColumnKind.Bool },
                new() { Header = "DurationInDays", PayloadProperty = "DurationInDays", Kind = ColumnKind.Scalar },
                new() { Header = "Category",       PayloadProperty = "Category",       Kind = ColumnKind.Scalar,
                    ValueMap = new() { {"0","Employee"}, {"1","FamilyMember"}, {"2","Both"} } },
            }
        },

        // --- Depends on Region (import Region first) ---
        // RegionName column contains plain text region name, resolved via lookup after Region is seeded.
        new SheetMap { SheetName = "City",             EntityName = "City",             DisplayName = "City",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "RegionName",   PayloadProperty = "Region",      Kind = ColumnKind.LookupByName, LookupEntity = "Region" },
            }
        },
    };

    // =======================================================================
    // PERSONNEL / TRANSACTION SHEETS
    // Source file: data.xlsx  (or employees.xlsx for persons-only)
    // These sheets import operational data after lookups are seeded.
    // =======================================================================
    public static readonly List<SheetMap> Sheets = new()
    {
        new SheetMap { SheetName = "Persons",       EntityName = "Person",        DisplayName = "Person",
            Columns = new() {
                new() { Header = "First Name",             PayloadProperty = "FirstName",              Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Last Name",              PayloadProperty = "LastName",               Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Middle Name",            PayloadProperty = "MiddleName",             Kind = ColumnKind.Scalar },
                new() { Header = "Date of Birth",          PayloadProperty = "DateOfBirth",            Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Birth Place",            PayloadProperty = "BirthPlace",             Kind = ColumnKind.Scalar },
                new() { Header = "Email",                  PayloadProperty = "Email",                  Kind = ColumnKind.Scalar },
                new() { Header = "Hire Date",              PayloadProperty = "HireDate",               Kind = ColumnKind.Scalar },
                new() { Header = "Foreign Address",        PayloadProperty = "ForeignAddress",         Kind = ColumnKind.Scalar },
                new() { Header = "Is Employee",            PayloadProperty = "IsEmployee",             Kind = ColumnKind.Scalar },
                new() { Header = "Is Subcontractor",       PayloadProperty = "IsSubcontractorEmployee",Kind = ColumnKind.Scalar },
                new() { Header = "Gender",                 PayloadProperty = "Gender",                 Kind = ColumnKind.LookupByName,  LookupEntity = "Gender" },
                new() { Header = "Nationality",            PayloadProperty = "Nationality",            Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Country of Birth",       PayloadProperty = "CountryOfBirth",         Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Foreign Address Country",PayloadProperty = "ForeignAddressCountry",  Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Marital Status",         PayloadProperty = "MaritalStatus",          Kind = ColumnKind.LookupByName,  LookupEntity = "MaritalStatus" },
                new() { Header = "Company",                PayloadProperty = "Company",                Kind = ColumnKind.LookupByName,  LookupEntity = "Company" },
                new() { Header = "Subcontractor",          PayloadProperty = "Subcontractor",          Kind = ColumnKind.LookupByName,  LookupEntity = "Subcontractor" },
                new() { Header = "Project Contract",       PayloadProperty = "ProjectContract",        Kind = ColumnKind.LookupByName,  LookupEntity = "ProjectContract" },
                new() { Header = "Relationship",           PayloadProperty = "Relationship",           Kind = ColumnKind.LookupByName,  LookupEntity = "Relationship" },
                new() { Header = "Sponsoring Employee",    PayloadProperty = "SponsoringEmployee",     Kind = ColumnKind.PersonLookupByName },
            }
        },
        new SheetMap { SheetName = "Passports",     EntityName = "Passport",      DisplayName = "Passport",
            Columns = new() {
                new() { Header = "Passport Number",  PayloadProperty = "PassportNumber",  Kind = ColumnKind.Scalar,            Required = true },
                new() { Header = "Personal Number",  PayloadProperty = "PersonalNumber",  Kind = ColumnKind.Scalar },
                new() { Header = "Authority",        PayloadProperty = "Authority",       Kind = ColumnKind.Scalar },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",       Kind = ColumnKind.Scalar },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",  Kind = ColumnKind.Scalar },
                new() { Header = "Person",           PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Type",    PayloadProperty = "PassportType",    Kind = ColumnKind.LookupByName,      LookupEntity = "PassportType" },
                new() { Header = "Issued Country",   PayloadProperty = "IssuedCountry",   Kind = ColumnKind.LookupByName,      LookupEntity = "Country" },
            }
        },
        new SheetMap { SheetName = "Visas",         EntityName = "Visa",          DisplayName = "Visa",
            Columns = new() {
                new() { Header = "Visa Number",      PayloadProperty = "VisaNumber",          Kind = ColumnKind.Scalar,       Required = true },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",           Kind = ColumnKind.Scalar,       Required = true },
                new() { Header = "Start Date",       PayloadProperty = "StartDate",           Kind = ColumnKind.Scalar },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",      Kind = ColumnKind.Scalar },
                new() { Header = "Notes",            PayloadProperty = "Notes",               Kind = ColumnKind.Scalar },
                new() { Header = "Has Invitation",   PayloadProperty = "HasInvitation",       Kind = ColumnKind.Scalar },
                new() { Header = "Has Border Zone",  PayloadProperty = "HasBorderZonePermit", Kind = ColumnKind.Bool },
                new() { Header = "Visa Type",        PayloadProperty = "VisaType",            Kind = ColumnKind.LookupByName, LookupEntity = "VisaType",        Required = true },
                new() { Header = "Visa Category",    PayloadProperty = "VisaCategory",        Kind = ColumnKind.LookupByName, LookupEntity = "VisaCategory" },
                new() { Header = "Issued Place",     PayloadProperty = "VisaIssuedPlace",     Kind = ColumnKind.LookupByName, LookupEntity = "VisaIssuedPlace" },
                new() { Header = "Passport Number",  PayloadProperty = "Passport",            Kind = ColumnKind.LookupByName, LookupEntity = "Passport" },
            }
        },
        new SheetMap { SheetName = "TravelHistory", EntityName = "TravelHistory",  DisplayName = "Travel History",
            Columns = new() {
                new() { Header = "Travel Date",       PayloadProperty = "TravelDate",      Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Travel Type",       PayloadProperty = "TravelType",      Kind = ColumnKind.Scalar },
                new() { Header = "Movement Type",     PayloadProperty = "MovementType",    Kind = ColumnKind.Scalar },
                new() { Header = "From Location",     PayloadProperty = "FromLocation",    Kind = ColumnKind.Scalar },
                new() { Header = "To Location",       PayloadProperty = "ToLocation",      Kind = ColumnKind.Scalar },
                new() { Header = "Notes",             PayloadProperty = "Notes",           Kind = ColumnKind.Scalar },
                new() { Header = "Person",            PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Check Point",       PayloadProperty = "CheckPoint",      Kind = ColumnKind.LookupByName,  LookupEntity = "CheckPoint" },
                new() { Header = "Purpose of Travel", PayloadProperty = "PurposeOfTravel", Kind = ColumnKind.LookupByName,  LookupEntity = "PurposeOfTravel" },
            }
        },
        new SheetMap { SheetName = "MedicalRecords",EntityName = "MedicalRecord",  DisplayName = "Medical Record",
            Columns = new() {
                new() { Header = "Document Number",  PayloadProperty = "DocumentNumber",   Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",        Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",   Kind = ColumnKind.Scalar },
                new() { Header = "Is Active",        PayloadProperty = "IsActive",         Kind = ColumnKind.Scalar },
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Validity Duration",PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName,  LookupEntity = "ValidityDuration" },
            }
        },
        new SheetMap { SheetName = "Registrations", EntityName = "Registration",   DisplayName = "Registration",
            Columns = new() {
                new() { Header = "Registration Number",PayloadProperty = "RegistrationNumber",Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Registration Date",  PayloadProperty = "RegistrationDate",  Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Expiration Date",    PayloadProperty = "ExpirationDate",    Kind = ColumnKind.Scalar },
                new() { Header = "Person",             PayloadProperty = "Person",            Kind = ColumnKind.PersonLookupByName, Required = true },
            }
        },
        new SheetMap { SheetName = "Education",     EntityName = "Education",      DisplayName = "Education",
            Columns = new() {
                new() { Header = "Graduation Year",  PayloadProperty = "GraduationYear",      Kind = ColumnKind.Scalar },
                new() { Header = "Person",           PayloadProperty = "Person",              Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Education Level",  PayloadProperty = "EducationLevel",      Kind = ColumnKind.LookupByName, LookupEntity = "EducationLevel" },
                new() { Header = "Institution",      PayloadProperty = "EducationInstitution",Kind = ColumnKind.LookupByName, LookupEntity = "EducationInstitution" },
                new() { Header = "Country",          PayloadProperty = "EducationCountry",    Kind = ColumnKind.LookupByName, LookupEntity = "Country" },
                new() { Header = "Specialty",        PayloadProperty = "Specialty",           Kind = ColumnKind.LookupByName, LookupEntity = "Specialty" },
            }
        },
        new SheetMap { SheetName = "PositionHistory",EntityName = "EmployeePositionHistory", DisplayName = "Position History",
            Columns = new() {
                new() { Header = "Start Date",   PayloadProperty = "StartDate",  Kind = ColumnKind.Scalar,           Required = true },
                new() { Header = "End Date",     PayloadProperty = "EndDate",    Kind = ColumnKind.Scalar },
                new() { Header = "Person",       PayloadProperty = "Person",     Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Position",     PayloadProperty = "Position",   Kind = ColumnKind.LookupByName,     LookupEntity = "Position" },
                new() { Header = "Department",   PayloadProperty = "Department", Kind = ColumnKind.LookupByName,     LookupEntity = "Department" },
            }
        },
        new SheetMap { SheetName = "Lodging",       EntityName = "Lodging",        DisplayName = "Lodging",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Full Address", PayloadProperty = "FullAddress", Kind = ColumnKind.Scalar },
                new() { Header = "Notes",        PayloadProperty = "Notes",       Kind = ColumnKind.Scalar },
                new() { Header = "Company",      PayloadProperty = "Company",     Kind = ColumnKind.LookupByName, LookupEntity = "Company" },
            }
        },
    };
}