using System.Collections.Generic;

namespace Visa2026.DataImporter;

// ---------------------------------------------------------------------------
// Column descriptor — one entry per Excel column header
// ---------------------------------------------------------------------------

/// <summary>
/// How a column value should be interpreted when building the API payload.
/// </summary>
public enum ColumnKind
{
    /// <summary>Plain string / number / bool / date — set directly on the payload.</summary>
    Scalar,

    /// <summary>
    /// Value is a Name that must be resolved to a lookup record via the API.
    /// The resolved object's ID is sent as { ID = guid }.
    /// </summary>
    LookupByName,

    /// <summary>
    /// Value is a Name that resolves to a Person record (looked up in the
    /// Person entity by FullName). Used for SponsoringEmployee etc.
    /// </summary>
    PersonLookupByName,
}

/// <summary>
/// Describes one Excel column and how it maps to the OData payload.
/// </summary>
public class ColumnMap
{
    /// <summary>Exact Excel column header (case-insensitive match).</summary>
    public string Header { get; init; } = "";

    /// <summary>Property name in the OData payload (matches JsonPropertyName).</summary>
    public string PayloadProperty { get; init; } = "";

    public ColumnKind Kind { get; init; } = ColumnKind.Scalar;

    /// <summary>
    /// For LookupByName: the OData entity name to search (e.g. "Gender", "Country").
    /// Ignored for Scalar and PersonLookupByName.
    /// </summary>
    public string LookupEntity { get; init; } = "";

    /// <summary>
    /// Whether this column is required. If true and the cell is empty, the row is skipped.
    /// </summary>
    public bool Required { get; init; } = false;
}

// ---------------------------------------------------------------------------
// Sheet descriptor — one entry per Excel sheet
// ---------------------------------------------------------------------------

/// <summary>
/// Maps one Excel sheet to one OData entity and defines its columns.
/// </summary>
public class SheetMap
{
    /// <summary>Exact Excel sheet name (case-insensitive match).</summary>
    public string SheetName { get; init; } = "";

    /// <summary>OData entity name used in the API URL (e.g. "Person", "Passport").</summary>
    public string EntityName { get; init; } = "";

    /// <summary>Human-readable label for console output.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Column definitions for this sheet.</summary>
    public List<ColumnMap> Columns { get; init; } = new();
}

// ---------------------------------------------------------------------------
// Master registry
// ---------------------------------------------------------------------------

/// <summary>
/// Hardcoded mapping registry.
/// Add a new SheetMap entry whenever you add a new importable sheet.
/// Column headers are matched case-insensitively and trimmed.
/// </summary>
public static class ExcelMappings
{
    public static readonly List<SheetMap> Sheets = new()
    {
        // ===================================================================
        // PERSONS
        // Sheet: "Persons"  →  OData: Person
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Persons",
            EntityName = "Person",
            DisplayName = "Person",
            Columns = new()
            {
                new() { Header = "First Name",          PayloadProperty = "FirstName",             Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Last Name",           PayloadProperty = "LastName",              Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Middle Name",         PayloadProperty = "MiddleName",            Kind = ColumnKind.Scalar                          },
                new() { Header = "Date of Birth",       PayloadProperty = "DateOfBirth",           Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Birth Place",         PayloadProperty = "BirthPlace",            Kind = ColumnKind.Scalar                          },
                new() { Header = "Email",               PayloadProperty = "Email",                 Kind = ColumnKind.Scalar                          },
                new() { Header = "Hire Date",           PayloadProperty = "HireDate",              Kind = ColumnKind.Scalar                          },
                new() { Header = "Foreign Address",     PayloadProperty = "ForeignAddress",        Kind = ColumnKind.Scalar                          },
                new() { Header = "Is Employee",         PayloadProperty = "IsEmployee",            Kind = ColumnKind.Scalar                          },
                new() { Header = "Is Subcontractor",    PayloadProperty = "IsSubcontractorEmployee", Kind = ColumnKind.Scalar                        },
                // Lookups resolved by Name
                new() { Header = "Gender",              PayloadProperty = "Gender",                Kind = ColumnKind.LookupByName,  LookupEntity = "Gender"         },
                new() { Header = "Nationality",         PayloadProperty = "Nationality",           Kind = ColumnKind.LookupByName,  LookupEntity = "Country"        },
                new() { Header = "Country of Birth",    PayloadProperty = "CountryOfBirth",        Kind = ColumnKind.LookupByName,  LookupEntity = "Country"        },
                new() { Header = "Foreign Address Country", PayloadProperty = "ForeignAddressCountry", Kind = ColumnKind.LookupByName, LookupEntity = "Country"     },
                new() { Header = "Marital Status",      PayloadProperty = "MaritalStatus",         Kind = ColumnKind.LookupByName,  LookupEntity = "MaritalStatus"  },
                new() { Header = "Company",             PayloadProperty = "Company",               Kind = ColumnKind.LookupByName,  LookupEntity = "Company"        },
                new() { Header = "Subcontractor",       PayloadProperty = "Subcontractor",         Kind = ColumnKind.LookupByName,  LookupEntity = "Subcontractor"  },
                new() { Header = "Project Contract",    PayloadProperty = "ProjectContract",       Kind = ColumnKind.LookupByName,  LookupEntity = "ProjectContract" },
                new() { Header = "Relationship",        PayloadProperty = "Relationship",          Kind = ColumnKind.LookupByName,  LookupEntity = "Relationship"   },
                new() { Header = "Sponsoring Employee", PayloadProperty = "SponsoringEmployee",    Kind = ColumnKind.PersonLookupByName                              },
            }
        },

        // ===================================================================
        // PASSPORTS
        // Sheet: "Passports"  →  OData: Passport
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Passports",
            EntityName = "Passport",
            DisplayName = "Passport",
            Columns = new()
            {
                new() { Header = "Passport Number",   PayloadProperty = "PassportNumber",  Kind = ColumnKind.Scalar,       Required = true  },
                new() { Header = "Personal Number",   PayloadProperty = "PersonalNumber",  Kind = ColumnKind.Scalar                         },
                new() { Header = "Authority",         PayloadProperty = "Authority",       Kind = ColumnKind.Scalar                         },
                new() { Header = "Issue Date",        PayloadProperty = "IssueDate",       Kind = ColumnKind.Scalar                         },
                new() { Header = "Expiration Date",   PayloadProperty = "ExpirationDate",  Kind = ColumnKind.Scalar                         },
                // Lookups
                new() { Header = "Person",            PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Type",     PayloadProperty = "PassportType",    Kind = ColumnKind.LookupByName,  LookupEntity = "PassportType"  },
                new() { Header = "Issued Country",    PayloadProperty = "IssuedCountry",   Kind = ColumnKind.LookupByName,  LookupEntity = "Country"       },
            }
        },

        // ===================================================================
        // VISAS
        // Sheet: "Visas"  →  OData: Visa
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Visas",
            EntityName = "Visa",
            DisplayName = "Visa",
            Columns = new()
            {
                new() { Header = "Visa Number",       PayloadProperty = "VisaNumber",      Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Issue Date",        PayloadProperty = "IssueDate",       Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Start Date",        PayloadProperty = "StartDate",       Kind = ColumnKind.Scalar                          },
                new() { Header = "Expiration Date",   PayloadProperty = "ExpirationDate",  Kind = ColumnKind.Scalar                          },
                new() { Header = "Notes",             PayloadProperty = "Notes",           Kind = ColumnKind.Scalar                          },
                new() { Header = "Has Invitation",    PayloadProperty = "HasInvitation",   Kind = ColumnKind.Scalar                          },
                new() { Header = "Has Border Zone",   PayloadProperty = "HasBorderZonePermit", Kind = ColumnKind.Scalar                      },
                // Lookups
                new() { Header = "Visa Type",         PayloadProperty = "VisaType",        Kind = ColumnKind.LookupByName,  LookupEntity = "VisaType",         Required = true },
                new() { Header = "Visa Category",     PayloadProperty = "VisaCategory",    Kind = ColumnKind.LookupByName,  LookupEntity = "VisaCategory"      },
                new() { Header = "Issued Place",      PayloadProperty = "VisaIssuedPlace", Kind = ColumnKind.LookupByName,  LookupEntity = "VisaIssuedPlace"   },
                new() { Header = "Passport Number",   PayloadProperty = "Passport",        Kind = ColumnKind.LookupByName,  LookupEntity = "Passport"          },
            }
        },

        // ===================================================================
        // TRAVEL HISTORY
        // Sheet: "TravelHistory"  →  OData: TravelHistory
        // ===================================================================
        new SheetMap
        {
            SheetName  = "TravelHistory",
            EntityName = "TravelHistory",
            DisplayName = "Travel History",
            Columns = new()
            {
                new() { Header = "Travel Date",       PayloadProperty = "TravelDate",      Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Travel Type",       PayloadProperty = "TravelType",      Kind = ColumnKind.Scalar                          },
                new() { Header = "Movement Type",     PayloadProperty = "MovementType",    Kind = ColumnKind.Scalar                          },
                new() { Header = "From Location",     PayloadProperty = "FromLocation",    Kind = ColumnKind.Scalar                          },
                new() { Header = "To Location",       PayloadProperty = "ToLocation",      Kind = ColumnKind.Scalar                          },
                new() { Header = "Notes",             PayloadProperty = "Notes",           Kind = ColumnKind.Scalar                          },
                // Lookups
                new() { Header = "Person",            PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Check Point",       PayloadProperty = "CheckPoint",      Kind = ColumnKind.LookupByName,  LookupEntity = "CheckPoint"       },
                new() { Header = "Purpose of Travel", PayloadProperty = "PurposeOfTravel", Kind = ColumnKind.LookupByName,  LookupEntity = "PurposeOfTravel"  },
            }
        },

        // ===================================================================
        // MEDICAL RECORDS
        // Sheet: "MedicalRecords"  →  OData: MedicalRecord
        // ===================================================================
        new SheetMap
        {
            SheetName  = "MedicalRecords",
            EntityName = "MedicalRecord",
            DisplayName = "Medical Record",
            Columns = new()
            {
                new() { Header = "Document Number",   PayloadProperty = "DocumentNumber",  Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Issue Date",        PayloadProperty = "IssueDate",       Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Expiration Date",   PayloadProperty = "ExpirationDate",  Kind = ColumnKind.Scalar                          },
                new() { Header = "Is Active",         PayloadProperty = "IsActive",        Kind = ColumnKind.Scalar                          },
                // Lookups
                new() { Header = "Person",            PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Validity Duration", PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName, LookupEntity = "ValidityDuration" },
            }
        },

        // ===================================================================
        // REGISTRATIONS
        // Sheet: "Registrations"  →  OData: Registration
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Registrations",
            EntityName = "Registration",
            DisplayName = "Registration",
            Columns = new()
            {
                new() { Header = "Registration Number", PayloadProperty = "RegistrationNumber", Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Registration Date",   PayloadProperty = "RegistrationDate",   Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Expiration Date",     PayloadProperty = "ExpirationDate",     Kind = ColumnKind.Scalar                  },
                // Lookups
                new() { Header = "Person",              PayloadProperty = "Person",             Kind = ColumnKind.PersonLookupByName, Required = true },
            }
        },

        // ===================================================================
        // EDUCATION
        // Sheet: "Education"  →  OData: Education
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Education",
            EntityName = "Education",
            DisplayName = "Education",
            Columns = new()
            {
                new() { Header = "Graduation Year",    PayloadProperty = "GraduationYear",     Kind = ColumnKind.Scalar                          },
                // Lookups
                new() { Header = "Person",             PayloadProperty = "Person",             Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Education Level",    PayloadProperty = "EducationLevel",     Kind = ColumnKind.LookupByName,  LookupEntity = "EducationLevel"      },
                new() { Header = "Institution",        PayloadProperty = "EducationInstitution", Kind = ColumnKind.LookupByName, LookupEntity = "EducationInstitution" },
                new() { Header = "Country",            PayloadProperty = "EducationCountry",   Kind = ColumnKind.LookupByName,  LookupEntity = "Country"             },
                new() { Header = "Specialty",          PayloadProperty = "Specialty",          Kind = ColumnKind.LookupByName,  LookupEntity = "Specialty"           },
            }
        },

        // ===================================================================
        // EMPLOYEE POSITION HISTORY
        // Sheet: "PositionHistory"  →  OData: EmployeePositionHistory
        // ===================================================================
        new SheetMap
        {
            SheetName  = "PositionHistory",
            EntityName = "EmployeePositionHistory",
            DisplayName = "Position History",
            Columns = new()
            {
                new() { Header = "Start Date",    PayloadProperty = "StartDate",   Kind = ColumnKind.Scalar,           Required = true  },
                new() { Header = "End Date",      PayloadProperty = "EndDate",     Kind = ColumnKind.Scalar                              },
                // Lookups
                new() { Header = "Person",        PayloadProperty = "Person",      Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Position",      PayloadProperty = "Position",    Kind = ColumnKind.LookupByName,     LookupEntity = "Position"   },
                new() { Header = "Department",    PayloadProperty = "Department",  Kind = ColumnKind.LookupByName,     LookupEntity = "Department" },
            }
        },

        // ===================================================================
        // LODGING
        // Sheet: "Lodging"  →  OData: Lodging
        // ===================================================================
        new SheetMap
        {
            SheetName  = "Lodging",
            EntityName = "Lodging",
            DisplayName = "Lodging",
            Columns = new()
            {
                new() { Header = "Name",          PayloadProperty = "Name",        Kind = ColumnKind.Scalar,        Required = true  },
                new() { Header = "Full Address",  PayloadProperty = "FullAddress", Kind = ColumnKind.Scalar                          },
                new() { Header = "Notes",         PayloadProperty = "Notes",       Kind = ColumnKind.Scalar                          },
                // Lookups
                new() { Header = "Company",       PayloadProperty = "Company",     Kind = ColumnKind.LookupByName,  LookupEntity = "Company" },
            }
        },
    };
}