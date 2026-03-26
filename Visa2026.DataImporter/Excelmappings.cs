using System.Collections.Generic;

namespace Visa2026.DataImporter;

// ---------------------------------------------------------------------------
// Column descriptor — one entry per Excel column header
// ---------------------------------------------------------------------------

public enum ColumnKind
{
    /// <summary>Plain string / number / bool / date — set directly on the payload.</summary>
    Scalar,
    /// <summary>Always kept as a plain string — prevents numbers like phone numbers being parsed as int.</summary>
    StringValue,
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
    /// OData property path used in $filter for LookupByName.
    /// Defaults to "Name". Use navigation paths like "Position/Name" when the
    /// entity has no direct Name property (e.g. EmployeePositionHistory).
    /// </summary>
    public string LookupFilterProperty { get; init; } = "Name";
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
                new() { Header = "PhoneNumber",             PayloadProperty = "PhoneNumber",             Kind = ColumnKind.StringValue },
                new() { Header = "Email",                   PayloadProperty = "Email",                   Kind = ColumnKind.Scalar },
                new() { Header = "TaxInformation",          PayloadProperty = "TaxInformation",          Kind = ColumnKind.Scalar },
                new() { Header = "AppNumberPrefix",         PayloadProperty = "AppNumberPrefix",         Kind = ColumnKind.Scalar },
                new() { Header = "ApplicationNumberPadding",PayloadProperty = "ApplicationNumberPadding",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",               PayloadProperty = "IsDefault",               Kind = ColumnKind.Bool },
            }
        },
        // --- Depends on Company ---
        new SheetMap { SheetName = "ProjectContract", EntityName = "ProjectContract", DisplayName = "Project Contract",
            Columns = new() {
                new() { Header = "Name",        PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",      PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",        PayloadProperty = "Code",        Kind = ColumnKind.StringValue },
                new() { Header = "Description", PayloadProperty = "Description", Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",   PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
                new() { Header = "Company",     PayloadProperty = "Company",     Kind = ColumnKind.LookupByName, LookupEntity = "Company" },
            }
        },

        new SheetMap { SheetName = "ApplicationTypeFilter", EntityName = "ApplicationTypeFilter", DisplayName = "Application Type Filter",
            Columns = new() {
                new() { Header = "Name",      PayloadProperty = "Name",      Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",    PayloadProperty = "NameTm",    Kind = ColumnKind.Scalar },
                new() { Header = "Code",      PayloadProperty = "Code",      Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault", PayloadProperty = "IsDefault", Kind = ColumnKind.Bool },
                new() { Header = "Category",  PayloadProperty = "Category",  Kind = ColumnKind.Scalar,
                    ValueMap = new() { {"0","Employee"}, {"1","FamilyMember"}, {"2","Both"} } },
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
                new() { Header = "LifecycleStage", PayloadProperty = "LifecycleStage", Kind = ColumnKind.Scalar,
                    ValueMap = new() { {"0","Entry"}, {"1","Stay"}, {"2","Exit"} } },
                // Relationship → ApplicationTypeFilter
                new() { Header = "ApplicationTypeFilterNames", PayloadProperty = "ApplicationTypeFilter", Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationTypeFilter" },
                // Show* flags
                new() { Header = "ShowProjectContract",          PayloadProperty = "ShowProjectContract",          Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaPeriod",               PayloadProperty = "ShowVisaPeriod",               Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaCategory",             PayloadProperty = "ShowVisaCategory",             Kind = ColumnKind.Bool },
                new() { Header = "ShowUrgency",                  PayloadProperty = "ShowUrgency",                  Kind = ColumnKind.Bool },
                new() { Header = "ShowInvitations",              PayloadProperty = "ShowInvitations",              Kind = ColumnKind.Bool },
                new() { Header = "ShowRejections",               PayloadProperty = "ShowRejections",               Kind = ColumnKind.Bool },
                new() { Header = "ShowWorkPermits",              PayloadProperty = "ShowWorkPermits",              Kind = ColumnKind.Bool },
                new() { Header = "ShowRegistrations",            PayloadProperty = "ShowRegistrations",            Kind = ColumnKind.Bool },
                new() { Header = "ShowVisas",                    PayloadProperty = "ShowVisas",                    Kind = ColumnKind.Bool },
                new() { Header = "ShowApplicationItems",         PayloadProperty = "ShowApplicationItems",         Kind = ColumnKind.Bool },
                new() { Header = "ShowApplicationReason",        PayloadProperty = "ShowApplicationReason",        Kind = ColumnKind.Bool },
                new() { Header = "ShowMigrationService",         PayloadProperty = "ShowMigrationService",         Kind = ColumnKind.Bool },
                new() { Header = "ShowBusinessTripPlan",         PayloadProperty = "ShowBusinessTripPlan",         Kind = ColumnKind.Bool },
                new() { Header = "ShowBusinessTrips",            PayloadProperty = "ShowBusinessTrips",            Kind = ColumnKind.Bool },
                new() { Header = "ShowPreviousPassport",         PayloadProperty = "ShowPreviousPassport",         Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentVisa",              PayloadProperty = "ShowCurrentVisa",              Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentWorkPermitItem",    PayloadProperty = "ShowCurrentWorkPermitItem",    Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentInvitationItem",    PayloadProperty = "ShowCurrentInvitationItem",    Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentAddressOfResidence",PayloadProperty = "ShowCurrentAddressOfResidence",Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentRegistration",      PayloadProperty = "ShowCurrentRegistration",      Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentEmployeeContract",  PayloadProperty = "ShowCurrentEmployeeContract",  Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentMedicalRecord",     PayloadProperty = "ShowCurrentMedicalRecord",     Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentEducation",         PayloadProperty = "ShowCurrentEducation",         Kind = ColumnKind.Bool },
                new() { Header = "ShowInvitationItemIsIssued",   PayloadProperty = "ShowInvitationItemIsIssued",   Kind = ColumnKind.Bool },
                new() { Header = "ShowWorkPermitItemIsIssued",   PayloadProperty = "ShowWorkPermitItemIsIssued",   Kind = ColumnKind.Bool },
                new() { Header = "ShowRejectionIssued",          PayloadProperty = "ShowRejectionIssued",          Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaIssued",               PayloadProperty = "ShowVisaIssued",               Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaIsCancelled",          PayloadProperty = "ShowVisaIsCancelled",          Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaIsChanged",            PayloadProperty = "ShowVisaIsChanged",            Kind = ColumnKind.Bool },
                new() { Header = "ShowInvitationItemIsCancelled",PayloadProperty = "ShowInvitationItemIsCancelled",Kind = ColumnKind.Bool },
                new() { Header = "ShowWorkPermitItemIsCancelled",PayloadProperty = "ShowWorkPermitItemIsCancelled",Kind = ColumnKind.Bool },
                new() { Header = "ShowInvitationItemIsChanged",  PayloadProperty = "ShowInvitationItemIsChanged",  Kind = ColumnKind.Bool },
                new() { Header = "ShowWorkPermitItemIsChanged",  PayloadProperty = "ShowWorkPermitItemIsChanged",  Kind = ColumnKind.Bool },
                new() { Header = "ShowVisaType",                 PayloadProperty = "ShowVisaType",                 Kind = ColumnKind.Bool },
            }
        },

        // --- Depends on Region (import Region first) ---
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
        // --- Dependencies for Persons ---
        new SheetMap { SheetName = "Company",          EntityName = "Company",          DisplayName = "Company",
            Columns = new() {
                new() { Header = "Name",                    PayloadProperty = "Name",                    Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Address",                 PayloadProperty = "Address",                 Kind = ColumnKind.Scalar },
                new() { Header = "PhoneNumber",             PayloadProperty = "PhoneNumber",             Kind = ColumnKind.StringValue },
                new() { Header = "Email",                   PayloadProperty = "Email",                   Kind = ColumnKind.Scalar },
                new() { Header = "TaxInformation",          PayloadProperty = "TaxInformation",          Kind = ColumnKind.Scalar },
                new() { Header = "AppNumberPrefix",         PayloadProperty = "AppNumberPrefix",         Kind = ColumnKind.Scalar },
                new() { Header = "ApplicationNumberPadding",PayloadProperty = "ApplicationNumberPadding",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",               PayloadProperty = "IsDefault",               Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "ProjectContract", EntityName = "ProjectContract", DisplayName = "Project Contract",
            Columns = new() {
                new() { Header = "Name",        PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",      PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",        PayloadProperty = "Code",        Kind = ColumnKind.StringValue },
                new() { Header = "Description", PayloadProperty = "Description", Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",   PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
                new() { Header = "Company",     PayloadProperty = "Company",     Kind = ColumnKind.LookupByName, LookupEntity = "Company" },
            }
        },

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
                new() { Header = "Is Employee",            PayloadProperty = "IsEmployee",             Kind = ColumnKind.Bool },
                new() { Header = "Is Subcontractor",       PayloadProperty = "IsSubcontractorEmployee",Kind = ColumnKind.Bool },
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
        new SheetMap { SheetName = "CompanyHead",    EntityName = "CompanyHead",   DisplayName = "Company Head",
            Columns = new() {
                new() { Header = "Company",          PayloadProperty = "Company",          Kind = ColumnKind.LookupByName,       LookupEntity = "Company", Required = true },
                new() { Header = "Employee",         PayloadProperty = "Employee",         Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Position",         PayloadProperty = "Position",         Kind = ColumnKind.LookupByName,       LookupEntity = "Position" },
                new() { Header = "Is Local",         PayloadProperty = "IsLocalEmployee",  Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Representative", EntityName = "Representative", DisplayName = "Company Representative",
            Columns = new() {
                new() { Header = "Company",          PayloadProperty = "Company",          Kind = ColumnKind.LookupByName,       LookupEntity = "Company", Required = true },
                new() { Header = "Employee",         PayloadProperty = "Employee",         Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Is Local",         PayloadProperty = "IsLocalEmployee",  Kind = ColumnKind.Bool },
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
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Validity Duration",PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName,  LookupEntity = "ValidityDuration", Required = true },
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
                new() { Header = "Graduation Year",  PayloadProperty = "GraduationYear",      Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Person",           PayloadProperty = "Person",              Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Education Level",  PayloadProperty = "EducationLevel",      Kind = ColumnKind.LookupByName, LookupEntity = "EducationLevel", Required = true },
                new() { Header = "Institution",      PayloadProperty = "EducationInstitution",Kind = ColumnKind.LookupByName, LookupEntity = "EducationInstitution", Required = true },
                new() { Header = "Country",          PayloadProperty = "EducationCountry",    Kind = ColumnKind.LookupByName, LookupEntity = "Country", Required = true },
                new() { Header = "Specialty",        PayloadProperty = "Specialty",           Kind = ColumnKind.LookupByName, LookupEntity = "Specialty", Required = true },
            }
        },
        new SheetMap { SheetName = "PositionHistory",EntityName = "EmployeePositionHistory", DisplayName = "Position History",
            Columns = new() {
                new() { Header = "Start Date",   PayloadProperty = "StartDate",  Kind = ColumnKind.Scalar,           Required = true },
                new() { Header = "End Date",     PayloadProperty = "EndDate",    Kind = ColumnKind.Scalar },
                new() { Header = "Person",       PayloadProperty = "Person",     Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Position",     PayloadProperty = "Position",   Kind = ColumnKind.LookupByName,     LookupEntity = "Position", Required = true },
                new() { Header = "Department",   PayloadProperty = "Department", Kind = ColumnKind.LookupByName,     LookupEntity = "Department", Required = true },
            }
        },
        new SheetMap { SheetName = "EmployeeContracts", EntityName = "EmployeeContract", DisplayName = "Employee Contract",
            Columns = new() {
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Start Date",       PayloadProperty = "ContractStartDate",Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Salary",           PayloadProperty = "Salary",           Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Validity Duration",PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName, LookupEntity = "ValidityDuration", Required = true },
                // FIX: EmployeePositionHistory has no "Name" property.
                // The Excel cell contains a Position name, so we filter via the
                // navigation path Position/Name instead of the default "Name".
                new() { Header = "Position History", PayloadProperty = "PositionHistory",  Kind = ColumnKind.LookupByName,
                        LookupEntity = "EmployeePositionHistory", LookupFilterProperty = "Position/Name" },
            }
        },
        new SheetMap { SheetName = "Lodging",       EntityName = "Lodging",        DisplayName = "Lodging",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Full Address", PayloadProperty = "FullAddress", Kind = ColumnKind.StringValue },
                new() { Header = "Notes",        PayloadProperty = "Notes",       Kind = ColumnKind.StringValue },
                new() { Header = "Company",      PayloadProperty = "Company",     Kind = ColumnKind.LookupByName, LookupEntity = "Company" },
            }
        },
        new SheetMap { SheetName = "AddressOfResidence",      EntityName = "AddressOfResidence", DisplayName = "Address of Residence",
            Columns = new() {
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Type",             PayloadProperty = "Type",             Kind = ColumnKind.Scalar, ValueMap = new() { {"0","Lodging"}, {"1","Hotel"}, {"2","PrivateHouse"} } },
                new() { Header = "Full Address",     PayloadProperty = "FullAddress",      Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Region",           PayloadProperty = "Region",           Kind = ColumnKind.LookupByName, LookupEntity = "Region", Required = true },
                new() { Header = "City",             PayloadProperty = "City",             Kind = ColumnKind.LookupByName, LookupEntity = "City", Required = true },
                new() { Header = "Lodging",          PayloadProperty = "Lodging",          Kind = ColumnKind.LookupByName, LookupEntity = "Lodging" },
                new() { Header = "Start Date",       PayloadProperty = "StartDate",        Kind = ColumnKind.Scalar },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",   Kind = ColumnKind.Scalar },
            }
        },
        // -------------------------------------------------------------------
        // Application and ApplicationItem — depends on Company, ApplicationType,
        // ApplicationTypeFilter, Urgency, VisaPeriod all being seeded first.
        // -------------------------------------------------------------------
        new SheetMap { SheetName = "Applications", EntityName = "Application", DisplayName = "Application",
            Columns = new() {
                new() { Header = "Application Number", PayloadProperty = "ApplicationNumber", Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Prefix",             PayloadProperty = "AppNumberPrefix",   Kind = ColumnKind.Scalar },
                new() { Header = "Year",               PayloadProperty = "Year",              Kind = ColumnKind.Scalar },
                new() { Header = "Date",               PayloadProperty = "ApplicationDate",   Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Category",           PayloadProperty = "Category",          Kind = ColumnKind.Scalar,
                    ValueMap = new() { {"0","Employee"}, {"1","FamilyMember"}, {"2","Both"} } },
                new() { Header = "Is Active",          PayloadProperty = "IsActive",          Kind = ColumnKind.Bool },
                new() { Header = "Company",            PayloadProperty = "Company",           Kind = ColumnKind.LookupByName, LookupEntity = "Company" },
                new() { Header = "Project Contract",   PayloadProperty = "ProjectContract",   Kind = ColumnKind.LookupByName, LookupEntity = "ProjectContract" },
                new() { Header = "Application Type",   PayloadProperty = "ApplicationType",   Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationType" },
                new() { Header = "Filter",             PayloadProperty = "ApplicationTypeFilter", Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationTypeFilter" },
                new() { Header = "Visa Category",      PayloadProperty = "VisaCategory",      Kind = ColumnKind.LookupByName, LookupEntity = "VisaCategory" },
                new() { Header = "Migration Service",  PayloadProperty = "MigrationService",  Kind = ColumnKind.LookupByName, LookupEntity = "MigrationService" },
                new() { Header = "Urgency",            PayloadProperty = "Urgency",           Kind = ColumnKind.LookupByName, LookupEntity = "Urgency" },
                new() { Header = "Visa Period",        PayloadProperty = "VisaPeriod",        Kind = ColumnKind.LookupByName, LookupEntity = "VisaPeriod" },
                new() { Header = "Visa Type",          PayloadProperty = "VisaType",          Kind = ColumnKind.LookupByName, LookupEntity = "VisaType" },
                new() { Header = "Company Head",       PayloadProperty = "CompanyHead",       Kind = ColumnKind.LookupByName, LookupEntity = "CompanyHead", LookupFilterProperty = "FullName" },
                new() { Header = "Representative",     PayloadProperty = "Representative",    Kind = ColumnKind.LookupByName, LookupEntity = "Representative", LookupFilterProperty = "FullName" },
            }
        },
        // ApplicationItem — depends on Application and Person (via PositionHistory / EmployeeContract)
        new SheetMap { SheetName = "ApplicationItems", EntityName = "ApplicationItem", DisplayName = "Application Item",
            Columns = new() {
                new() { Header = "Application",        PayloadProperty = "Application",              Kind = ColumnKind.LookupByName,      LookupEntity = "Application",              Required = true },
                new() { Header = "Person",             PayloadProperty = "Person",                   Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Number",    PayloadProperty = "CurrentPassport",          Kind = ColumnKind.LookupByName,      LookupEntity = "Passport",      LookupFilterProperty = "PassportNumber", Required = true },
                new() { Header = "Visa Number",        PayloadProperty = "CurrentVisa",              Kind = ColumnKind.LookupByName,      LookupEntity = "Visa",          LookupFilterProperty = "VisaNumber" },
                new() { Header = "Position History",   PayloadProperty = "CurrentPositionHistory",   Kind = ColumnKind.LookupByName,      LookupEntity = "EmployeePositionHistory", LookupFilterProperty = "Position/Name" },
                new() { Header = "Contract",           PayloadProperty = "CurrentEmployeeContract",  Kind = ColumnKind.LookupByName,      LookupEntity = "EmployeeContract", LookupFilterProperty = "ContractStartDate" },
                new() { Header = "Previous Passport",  PayloadProperty = "PreviousPassport",         Kind = ColumnKind.LookupByName,      LookupEntity = "Passport",      LookupFilterProperty = "PassportNumber" },
                new() { Header = "Work Permit Item",   PayloadProperty = "CurrentWorkPermitItem",    Kind = ColumnKind.LookupByName,      LookupEntity = "WorkPermitItem", LookupFilterProperty = "WorkPermitNumber" },
                new() { Header = "Invitation Item",    PayloadProperty = "CurrentInvitationItem",    Kind = ColumnKind.LookupByName,      LookupEntity = "InvitationItem", LookupFilterProperty = "InvitationItemName" },
                new() { Header = "Address",            PayloadProperty = "CurrentAddressOfResidence", Kind = ColumnKind.LookupByName,      LookupEntity = "AddressOfResidence", LookupFilterProperty = "FullAddress" },
                new() { Header = "Registration",       PayloadProperty = "CurrentRegistration",      Kind = ColumnKind.LookupByName,      LookupEntity = "Registration", LookupFilterProperty = "RegistrationNumber" },
                new() { Header = "Medical Record",     PayloadProperty = "CurrentMedicalRecord",     Kind = ColumnKind.LookupByName,      LookupEntity = "MedicalRecord", LookupFilterProperty = "DocumentNumber" },
                new() { Header = "Education",          PayloadProperty = "CurrentEducation",         Kind = ColumnKind.LookupByName,      LookupEntity = "Education",      LookupFilterProperty = "EducationDescription" },

                new() { Header = "Invitation Issued",  PayloadProperty = "InvitationItemIsIssued",   Kind = ColumnKind.Bool },
                new() { Header = "Work Permit Issued", PayloadProperty = "WorkPermitItemIsIssued",   Kind = ColumnKind.Bool },
                new() { Header = "Rejection Issued",   PayloadProperty = "RejectionIssued",          Kind = ColumnKind.Bool },
                new() { Header = "Visa Issued",        PayloadProperty = "VisaIssued",               Kind = ColumnKind.Bool },
                new() { Header = "Inv Item Cancelled", PayloadProperty = "InvitationItemIsCancelled",Kind = ColumnKind.Bool },
                new() { Header = "WP Item Cancelled",  PayloadProperty = "IsCancelled",              Kind = ColumnKind.Bool },
                new() { Header = "Inv Item Changed",   PayloadProperty = "InvitationItemIsChanged",  Kind = ColumnKind.Bool },
                new() { Header = "WP Item Changed",    PayloadProperty = "WorkPermitItemIsChanged",  Kind = ColumnKind.Bool },
                new() { Header = "Visa Cancelled",     PayloadProperty = "VisaIsCancelled",          Kind = ColumnKind.Bool },
                new() { Header = "Visa Changed",       PayloadProperty = "VisaIsChanged",            Kind = ColumnKind.Bool },
            }
        },
    };
}