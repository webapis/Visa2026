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

/// <summary>
/// One part of a composite upsert key (<c>$filter</c> clause). Values are read from the yaml/Excel row
/// unless <see cref="FromPayload"/> is true (then the resolved OData reference guid is used).
/// </summary>
public sealed class UpsertKeyPart
{
    public string ODataProperty { get; init; } = "";
    public string Header { get; init; } = "";
    public bool FromPayload { get; init; }
    public string? PayloadProperty { get; init; }
    /// <summary>When true, an empty row cell matches OData <c>null</c> instead of failing the filter.</summary>
    public bool Optional { get; init; }
    /// <summary>When true, the cell value is quoted as an OData string literal (no scalar parsing).</summary>
    public bool StringLiteral { get; init; }
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

    /// <summary>
    /// When scenario sync mode is on, existing rows are found via OData $filter and PATCHed.
    /// If empty, <see cref="ExcelMappings.DefaultUpsertKeys"/> may supply keys for the entity.
    /// </summary>
    public IReadOnlyList<UpsertKeyPart>? UpsertKeys { get; init; }

    /// <summary>
    /// Optional async hook called after each row is successfully POSTed.
    /// Use this for entities that require a follow-up PATCH on a related
    /// sub-object created server-side (e.g. ApplicationItem travel → MovementRecord).
    ///
    /// Parameters: (createdEntityId, rawRow, headerIndex, apiClient)
    /// </summary>
    public Func<Guid, List<object>, Dictionary<string, int>, ApiClient, Task>? PostSeedHook { get; init; }

    /// <summary>
    /// When true, import uses PATCH on the first existing OData row (or POST if none).
    /// Used for organization singleton BOs (CompanyProfile, AuthorizedSignatory, …).
    /// </summary>
    public bool SingletonUpsert { get; init; }
}

public static class ExcelMappings
{
    /// <summary>
    /// OData entities synced by <c>LookupCatalogSyncUpdater</c> / tenant JSON in Visa2026.Module.
    /// DataImporter must never POST these (use <c>lookup.xlsm</c> only with --export-lookup-catalogs).
    /// </summary>
    public static bool IsModuleLookupCatalogEntity(string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return false;

        foreach (var sheet in LookupSheets)
        {
            if (string.Equals(sheet.EntityName, entityName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return TenantLookupCatalogEntities.Contains(entityName);
    }

    /// <summary>Organization singletons synced from tenant JSON (not via DataImporter).</summary>
    public static bool IsModuleOrganizationSingletonEntity(string? entityName) =>
        !string.IsNullOrWhiteSpace(entityName) &&
        ModuleOrganizationSingletonEntities.Contains(entityName);

    /// <summary>Default upsert keys by OData entity when <see cref="SheetMap.UpsertKeys"/> is not set.</summary>
    public static IReadOnlyList<UpsertKeyPart>? GetDefaultUpsertKeys(string entityName) => null;

    /// <summary>Lookup catalogs and org singletons — maintained only in Visa2026.Module.</summary>
    public static bool IsBlockedImportEntity(string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return false;

        if (IsModuleLookupCatalogEntity(entityName))
            return true;

        if (IsModuleOrganizationSingletonEntity(entityName))
            return true;

        return ModuleManagedLookupEntities.Contains(entityName);
    }

    private static readonly HashSet<string> ModuleOrganizationSingletonEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "CompanyProfile", "ApplicationNumberingProfile", "AuthorizedSignatory", "AuthorizedRepresentative",
    };

    private static readonly HashSet<string> TenantLookupCatalogEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProjectContract", "Position", "Department", "Ministry", "Specialty", "EducationInstitution",
    };

    private static readonly HashSet<string> ModuleManagedLookupEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApplicationType", "ApplicationTypeFilter",
    };

    /// <summary>Sheet name stays <c>Company</c> for backward-compatible workbooks; targets CompanyProfile singleton.</summary>
    private static readonly SheetMap CompanyProfileSheetMap = new()
    {
        SheetName = "Company",
        EntityName = "CompanyProfile",
        DisplayName = "Company Profile",
        SingletonUpsert = true,
        Columns = new()
        {
            new() { Header = "Name", PayloadProperty = "Name", Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "Code", PayloadProperty = "Code", Kind = ColumnKind.Scalar },
            new() { Header = "Address", PayloadProperty = "Address", Kind = ColumnKind.Scalar },
            new() { Header = "PhoneNumber", PayloadProperty = "PhoneNumber", Kind = ColumnKind.StringValue },
            new() { Header = "Email", PayloadProperty = "Email", Kind = ColumnKind.Scalar },
            new() { Header = "TaxInformation", PayloadProperty = "TaxInformation", Kind = ColumnKind.Scalar },
        }
    };

    private static readonly SheetMap ApplicationNumberingProfileSheetMap = new()
    {
        SheetName = "ApplicationNumbering",
        EntityName = "ApplicationNumberingProfile",
        DisplayName = "Application Numbering",
        SingletonUpsert = true,
        Columns = new()
        {
            new() { Header = "Name", PayloadProperty = "Name", Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "AppNumberPrefix", PayloadProperty = "AppNumberPrefix", Kind = ColumnKind.Scalar },
            new() { Header = "AppNumberFormat", PayloadProperty = "AppNumberFormat", Kind = ColumnKind.Scalar },
            new() { Header = "ApplicationNumberPadding", PayloadProperty = "ApplicationNumberPadding", Kind = ColumnKind.Scalar },
            new() { Header = "ApplicationNumberSeed", PayloadProperty = "ApplicationNumberSeed", Kind = ColumnKind.Scalar },
        }
    };

    private static readonly SheetMap AuthorizedSignatorySheetMap = new()
    {
        SheetName = "CompanyHead",
        EntityName = "AuthorizedSignatory",
        DisplayName = "Authorized Signatory",
        SingletonUpsert = true,
        Columns = new()
        {
            new() { Header = "Full Name", PayloadProperty = "FullName", Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "Position (Tm)", PayloadProperty = "PositionTitleTm", Kind = ColumnKind.Scalar },
            new() { Header = "Passport Number", PayloadProperty = "PassportNumber", Kind = ColumnKind.StringValue },
            new() { Header = "Passport Authority", PayloadProperty = "PassportAuthority", Kind = ColumnKind.Scalar },
            new() { Header = "Passport Issue Date", PayloadProperty = "PassportIssueDate", Kind = ColumnKind.Scalar },
        }
    };

    private static readonly SheetMap AuthorizedRepresentativeSheetMap = new()
    {
        SheetName = "Representative",
        EntityName = "AuthorizedRepresentative",
        DisplayName = "Authorized Representative",
        SingletonUpsert = true,
        Columns = new()
        {
            new() { Header = "Full Name", PayloadProperty = "FullName", Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "Position (Tm)", PayloadProperty = "PositionTitleTm", Kind = ColumnKind.Scalar },
            new() { Header = "Phone", PayloadProperty = "Phone", Kind = ColumnKind.StringValue },
            new() { Header = "Passport Number", PayloadProperty = "PassportNumber", Kind = ColumnKind.StringValue },
            new() { Header = "Passport Authority", PayloadProperty = "PassportAuthority", Kind = ColumnKind.Scalar },
            new() { Header = "Passport Issue Date", PayloadProperty = "PassportIssueDate", Kind = ColumnKind.Scalar },
        }
    };

    // =======================================================================
    // LOOKUP SHEETS (lookup.xlsm → Module JSON export only)
    // Source file: lookup.xlsm — not imported at runtime.
    // Column structure: _RowNum | ID | [internal cols] | Name | Code | ...
    // We map only the payload-relevant columns (skip _RowNum, GCRecord, etc.)
    // =======================================================================
    public static readonly List<SheetMap> LookupSheets = new()
    {
        // --- No dependencies ---

        new SheetMap { SheetName = "Country",          EntityName = "Country",          DisplayName = "Country",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "isDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Gender",           EntityName = "Gender",           DisplayName = "Gender",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "MaritalStatus",    EntityName = "MaritalStatus",    DisplayName = "Marital Status",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Urgency",          EntityName = "Urgency",          DisplayName = "Urgency",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaCategory",     EntityName = "VisaCategory",     DisplayName = "Visa Category",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaPeriod",       EntityName = "VisaPeriod",       DisplayName = "Visa Period",
            Columns = new() {
                new() { Header = "Name",           PayloadProperty = "Name",          Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",         PayloadProperty = "NameTm",        Kind = ColumnKind.Scalar },
                new() { Header = "Code",           PayloadProperty = "Code",          Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm__Code",  PayloadProperty = "PdfForm__Code",  Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Count",  PayloadProperty = "PdfForm_Count", Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",      PayloadProperty = "IsDefault",     Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaType",         EntityName = "VisaType",         DisplayName = "Visa Type",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "EducationLevel",   EntityName = "EducationLevel",   DisplayName = "Education Level",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Purpose of Travel",EntityName = "PurposeOfTravel",  DisplayName = "Purpose of Travel",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Checkpoint",       EntityName = "CheckPoint",       DisplayName = "Checkpoint",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "VisaIssuedPlace", EntityName = "VisaIssuedPlace",  DisplayName = "Visa Issued Place",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "MigrationService", EntityName = "MigrationService",  DisplayName = "Migration Service",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "PassportType",     EntityName = "PassportType",     DisplayName = "Passport Type",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Specialty",      EntityName = "Specialty",        DisplayName = "Specialty",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "EducInstitution", EntityName = "EducationInstitution", DisplayName = "Education Institution",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Relationships",    EntityName = "Relationship",     DisplayName = "Relationship",
            Columns = new() {
                new() { Header = "Name",           PayloadProperty = "Name",          Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",         PayloadProperty = "NameTm",        Kind = ColumnKind.Scalar },
                new() { Header = "Code",           PayloadProperty = "Code",          Kind = ColumnKind.Scalar },
                new() { Header = "ReverseNameTm",  PayloadProperty = "ReverseNameTm", Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "ApplicationLocation", EntityName = "ApplicationLocation", DisplayName = "Application Location",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "MovementPermitLocation", EntityName = "MovementPermitLocation", DisplayName = "Movement Permit Location",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "BorderZoneLocation", EntityName = "BorderZoneLocation", DisplayName = "Border Zone Location",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Department",      EntityName = "Department",       DisplayName = "Department",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Position",        EntityName = "Position",         DisplayName = "Position",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Validation Duration", EntityName = "ValidityDuration", DisplayName = "Validity Duration",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "NumberOfDays", PayloadProperty = "NumberOfDays",Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "ApplicationStates",EntityName = "ApplicationState", DisplayName = "Application State",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",    PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "Region",           EntityName = "Region",           DisplayName = "Region",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
            }
        },
        new SheetMap { SheetName = "Ministry",         EntityName = "Ministry",         DisplayName = "Ministry",
            Columns = new() {
                new() { Header = "Name",           PayloadProperty = "Name",           Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "RecipientBlock", PayloadProperty = "RecipientBlock", Kind = ColumnKind.Scalar },
                new() { Header = "FormOfAddress",  PayloadProperty = "FormOfAddress",  Kind = ColumnKind.Scalar },
            }
        },
        CompanyProfileSheetMap,
        ApplicationNumberingProfileSheetMap,
        // --- Depends on Ministry ---
        new SheetMap { SheetName = "ProjectContract", EntityName = "ProjectContract", DisplayName = "Project Contract",
            Columns = new() {
                new() { Header = "Name",        PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",      PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",        PayloadProperty = "Code",        Kind = ColumnKind.StringValue },
                new() { Header = "Description", PayloadProperty = "Description", Kind = ColumnKind.Scalar },
                new() { Header = "IsDefault",   PayloadProperty = "IsDefault",   Kind = ColumnKind.Bool },
                new() { Header = "Ministry",    PayloadProperty = "Ministry",    Kind = ColumnKind.LookupByName, LookupEntity = "Ministry" },
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
                new() { Header = "ShowBusinessTrips",            PayloadProperty = "ShowBusinessTrips",            Kind = ColumnKind.Bool },
                new() { Header = "ShowPreviousPassport",         PayloadProperty = "ShowPreviousPassport",         Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentVisa",              PayloadProperty = "ShowCurrentVisa",              Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentWorkPermitItem",    PayloadProperty = "ShowCurrentWorkPermitItem",    Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentInvitationItem",    PayloadProperty = "ShowCurrentInvitationItem",    Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentAddressOfResidence",PayloadProperty = "ShowCurrentAddressOfResidence",Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentEmployeeContract",  PayloadProperty = "ShowCurrentEmployeeContract",  Kind = ColumnKind.Bool },
                new() { Header = "ShowCurrentWorkDuty",          PayloadProperty = "ShowCurrentWorkDuty",          Kind = ColumnKind.Bool },
                new() { Header = "ShowWorkPermittedLocations",   PayloadProperty = "ShowWorkPermittedLocations",   Kind = ColumnKind.Bool },
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
                new() { Header = "ShowFromCity",                 PayloadProperty = "ShowFromCity",                 Kind = ColumnKind.Bool },
                new() { Header = "ShowToCity",                   PayloadProperty = "ShowToCity",                   Kind = ColumnKind.Bool },
                new() { Header = "ShowMovementPermitLocation",  PayloadProperty = "ShowMovementPermitLocation",  Kind = ColumnKind.Bool },
                new() { Header = "ShowBorderZoneLocation",      PayloadProperty = "ShowBorderZoneLocation",      Kind = ColumnKind.Bool },
            }
        },

        // --- Depends on Region (import Region first) ---
        new SheetMap { SheetName = "City",             EntityName = "City",             DisplayName = "City",
            Columns = new() {
                new() { Header = "Name",         PayloadProperty = "Name",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "NameTm",       PayloadProperty = "NameTm",      Kind = ColumnKind.Scalar },
                new() { Header = "Code",         PayloadProperty = "Code",        Kind = ColumnKind.Scalar },
                new() { Header = "PdfForm_Code", PayloadProperty = "PdfForm_Code",Kind = ColumnKind.Scalar },
                new() { Header = "RegionName",   PayloadProperty = "Region",      Kind = ColumnKind.LookupByName, LookupEntity = "Region" },
            }
        },
    };

    // =======================================================================
    // SCENARIOS SHEET
    // Source file: data.xlsx
    // Defines named scenarios, their execution order, optional dependency,
    // and the anchor entity/key/value used for idempotency (skip-if-exists).
    //
    // Expected columns:
    //   Order | Name | Description | DependsOn | AnchorEntity | AnchorKey | AnchorValue
    //
    // This sheet is NOT posted to OData — it is parsed locally by
    // ExcelImporter.ReadScenarios() to drive scenario-based seeding.
    // =======================================================================
    public static readonly SheetMap ScenariosSheet = new()
    {
        SheetName   = "Scenarios",
        EntityName  = "",          // not an OData entity
        DisplayName = "Scenarios",
        Columns = new()
        {
            new() { Header = "Order",        PayloadProperty = "Order",        Kind = ColumnKind.Scalar },
            new() { Header = "Name",         PayloadProperty = "Name",         Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "Description",  PayloadProperty = "Description",  Kind = ColumnKind.Scalar },
            new() { Header = "DependsOn",    PayloadProperty = "DependsOn",    Kind = ColumnKind.Scalar },
            new() { Header = "AnchorEntity", PayloadProperty = "AnchorEntity", Kind = ColumnKind.Scalar },
            new() { Header = "AnchorKey",    PayloadProperty = "AnchorKey",    Kind = ColumnKind.Scalar },
            new() { Header = "AnchorValue",  PayloadProperty = "AnchorValue",  Kind = ColumnKind.Scalar },
        }
    };

    // =======================================================================
    // PERSONNEL / TRANSACTION SHEETS
    // Source file: data.xlsx  (or employees.xlsx for persons-only)
    // These sheets import operational data after lookups are seeded.
    // =======================================================================
    public static readonly List<SheetMap> Sheets = new()
    {
        new SheetMap { SheetName = "Persons",       EntityName = "Person",        DisplayName = "Person",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "Email", Header = "Email" } },
            Columns = new() {
                new() { Header = "First Name",             PayloadProperty = "FirstName",              Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Last Name",              PayloadProperty = "LastName",               Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Personal Number",        PayloadProperty = "PersonalNumber",         Kind = ColumnKind.StringValue },
                new() { Header = "Middle Name",            PayloadProperty = "MiddleName",             Kind = ColumnKind.Scalar },
                new() { Header = "Date of Birth",          PayloadProperty = "DateOfBirth",            Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Birth Place",            PayloadProperty = "BirthPlace",             Kind = ColumnKind.Scalar },
                new() { Header = "Email",                  PayloadProperty = "Email",                  Kind = ColumnKind.Scalar },
                new() { Header = "Hire Date",              PayloadProperty = "HireDate",               Kind = ColumnKind.Scalar },
                new() { Header = "Foreign Address",        PayloadProperty = "ForeignAddress",         Kind = ColumnKind.Scalar },
                new() { Header = "Is Employee",            PayloadProperty = "IsEmployee",             Kind = ColumnKind.Bool },
                new() { Header = "Gender",                 PayloadProperty = "Gender",                 Kind = ColumnKind.LookupByName,  LookupEntity = "Gender" },
                new() { Header = "Nationality",            PayloadProperty = "Nationality",            Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Country of Birth",       PayloadProperty = "CountryOfBirth",         Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Foreign Address Country",PayloadProperty = "ForeignAddressCountry",  Kind = ColumnKind.LookupByName,  LookupEntity = "Country" },
                new() { Header = "Marital Status",         PayloadProperty = "MaritalStatus",          Kind = ColumnKind.LookupByName,  LookupEntity = "MaritalStatus" },
                new() { Header = "Company (Subcontractor)", PayloadProperty = "Subcontractor",          Kind = ColumnKind.LookupByName,  LookupEntity = "Subcontractor" },
                new() { Header = "Subcontractor",          PayloadProperty = "Subcontractor",          Kind = ColumnKind.LookupByName,  LookupEntity = "Subcontractor" },
                new() { Header = "Project Contract",       PayloadProperty = "ProjectContract",        Kind = ColumnKind.LookupByName,  LookupEntity = "ProjectContract" },
                new() { Header = "Relationship",           PayloadProperty = "Relationship",           Kind = ColumnKind.LookupByName,  LookupEntity = "Relationship" },
                new() { Header = "Sponsoring Employee",    PayloadProperty = "SponsoringEmployee",     Kind = ColumnKind.PersonLookupByName },
            }
        },
        new SheetMap { SheetName = "Passports",     EntityName = "Passport",      DisplayName = "Passport",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "PassportNumber", Header = "Passport Number" } },
            Columns = new() {
                new() { Header = "Passport Number",  PayloadProperty = "PassportNumber",  Kind = ColumnKind.Scalar,            Required = true },
                new() { Header = "Personal Number",  PayloadProperty = "PersonalNumber",  Kind = ColumnKind.StringValue },
                new() { Header = "Authority",        PayloadProperty = "Authority",       Kind = ColumnKind.StringValue },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",       Kind = ColumnKind.Scalar },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",  Kind = ColumnKind.Scalar },
                new() { Header = "Person",           PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Type",    PayloadProperty = "PassportType",    Kind = ColumnKind.LookupByName,      LookupEntity = "PassportType" },
                new() { Header = "Issued Country",   PayloadProperty = "IssuedCountry",   Kind = ColumnKind.LookupByName,      LookupEntity = "Country" },
            }
        },
        new SheetMap { SheetName = "TravelHistory", EntityName = "TravelHistory",  DisplayName = "Travel History",
            Columns = new() {
                new() { Header = "Type",              PayloadProperty = "@odata.type",     Kind = ColumnKind.Scalar,
                    ValueMap = new() { 
                        {"ExternalArrival", "#Visa2026.Module.BusinessObjects.ExternalArrival"}, 
                        {"ExternalDeparture", "#Visa2026.Module.BusinessObjects.ExternalDeparture"},
                        {"InternalArrival", "#Visa2026.Module.BusinessObjects.InternalArrival"},
                        {"InternalDeparture", "#Visa2026.Module.BusinessObjects.InternalDeparture"}
                    } },
                new() { Header = "Travel Date",       PayloadProperty = "TravelDate",      Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Notes",             PayloadProperty = "Notes",           Kind = ColumnKind.Scalar },
                new() { Header = "Person",            PayloadProperty = "Person",          Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Check Point",       PayloadProperty = "CheckPoint",      Kind = ColumnKind.LookupByName,  LookupEntity = "CheckPoint" },
            }
        },
        new SheetMap { SheetName = "MedicalRecords",EntityName = "MedicalRecord",  DisplayName = "Medical Record",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "DocumentNumber", Header = "Document Number" } },
            Columns = new() {
                new() { Header = "Document Number",  PayloadProperty = "DocumentNumber",   Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",        Kind = ColumnKind.Scalar,        Required = true },
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Validity Duration",PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName,  LookupEntity = "ValidityDuration", Required = true },
            }
        },
        new SheetMap { SheetName = "Education",     EntityName = "Education",      DisplayName = "Education",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "GraduationYear", Header = "Graduation Year", Optional = true, StringLiteral = true },
                new UpsertKeyPart { ODataProperty = "EducationInstitution/ID", Header = "Institution", FromPayload = true, PayloadProperty = "EducationInstitution" },
            },
            Columns = new() {
                new() { Header = "Graduation Year",  PayloadProperty = "GraduationYear",      Kind = ColumnKind.StringValue },
                new() { Header = "Person",           PayloadProperty = "Person",              Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Education Level",  PayloadProperty = "EducationLevel",      Kind = ColumnKind.LookupByName, LookupEntity = "EducationLevel", Required = true },
                new() { Header = "Institution",      PayloadProperty = "EducationInstitution",Kind = ColumnKind.LookupByName, LookupEntity = "EducationInstitution", Required = true },
                new() { Header = "Country",          PayloadProperty = "EducationCountry",    Kind = ColumnKind.LookupByName, LookupEntity = "Country", Required = true },
                new() { Header = "Specialty",        PayloadProperty = "Specialty",           Kind = ColumnKind.LookupByName, LookupEntity = "Specialty", Required = true },
            }
        },
        new SheetMap { SheetName = "PositionHistory",EntityName = "EmployeePositionHistory", DisplayName = "Position History",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "StartDate", Header = "Start Date" },
                new UpsertKeyPart { ODataProperty = "Position/Name", Header = "Position" },
            },
            Columns = new() {
                new() { Header = "Start Date",   PayloadProperty = "StartDate",  Kind = ColumnKind.Scalar,           Required = true },
                new() { Header = "End Date",     PayloadProperty = "EndDate",    Kind = ColumnKind.Scalar },
                new() { Header = "Person",       PayloadProperty = "Person",     Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Position",     PayloadProperty = "Position",   Kind = ColumnKind.LookupByName,     LookupEntity = "Position", Required = true },
                new() { Header = "Department",   PayloadProperty = "Department", Kind = ColumnKind.LookupByName,     LookupEntity = "Department", Required = true },
            }
        },
        new SheetMap { SheetName = "EmployeeContracts", EntityName = "EmployeeContract", DisplayName = "Employee Contract",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "ContractStartDate", Header = "Start Date" },
            },
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
        new SheetMap { SheetName = "WorkDuties", EntityName = "WorkDuty", DisplayName = "Work Duty",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "Description", Header = "Description" },
            },
            Columns = new()
            {
                new() { Header = "Person",       PayloadProperty = "Person",       Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Description",  PayloadProperty = "Description",  Kind = ColumnKind.StringValue, Required = true },
            }
        },
        new SheetMap { SheetName = "Lodging",       EntityName = "Lodging",        DisplayName = "Lodging",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "FullAddress", Header = "Full Address" } },
            Columns = new() {
                new() { Header = "Full Address", PayloadProperty = "FullAddress", Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Notes",        PayloadProperty = "Notes",       Kind = ColumnKind.StringValue },
            }
        },
        new SheetMap { SheetName = "AddressOfResidence",      EntityName = "AddressOfResidence", DisplayName = "Address of Residence",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "FullAddress", Header = "Full Address" },
            },
            Columns = new() {
                new() { Header = "Person",           PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Type",             PayloadProperty = "Type",             Kind = ColumnKind.Scalar, ValueMap = new() { {"0","Lodging"}, {"1","Hotel"}, {"2","PrivateHouse"} } },
                new() { Header = "Full Address",     PayloadProperty = "FullAddress",      Kind = ColumnKind.StringValue, Required = true },
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
                new() { Header = "Full Application Number", PayloadProperty = "", Kind = ColumnKind.StringValue },
                new() { Header = "Application Number", PayloadProperty = "ApplicationNumber", Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Prefix",             PayloadProperty = "AppNumberPrefix",   Kind = ColumnKind.StringValue },
                new() { Header = "Year",               PayloadProperty = "Year",              Kind = ColumnKind.Scalar },
                new() { Header = "Date",               PayloadProperty = "ApplicationDate",   Kind = ColumnKind.Scalar, Required = true },
                // Category is on ApplicationType only — do not POST on Application (OData 400 Incorrect body).
                new() { Header = "Category",           PayloadProperty = "",                  Kind = ColumnKind.StringValue },
                new() { Header = "Project Contract",   PayloadProperty = "ProjectContract",   Kind = ColumnKind.LookupByName, LookupEntity = "ProjectContract" },
                new() { Header = "Application Type",   PayloadProperty = "ApplicationType",   Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationType" },
                // Filter / ApplicationTypeFilter removed from Application BO — type is chosen via ApplicationType only.
                new() { Header = "Visa Category",      PayloadProperty = "VisaCategory",      Kind = ColumnKind.LookupByName, LookupEntity = "VisaCategory" },
                new() { Header = "Migration Service",  PayloadProperty = "MigrationService",  Kind = ColumnKind.LookupByName, LookupEntity = "MigrationService" },
                new() { Header = "Urgency",            PayloadProperty = "Urgency",           Kind = ColumnKind.LookupByName, LookupEntity = "Urgency" },
                new() { Header = "Visa Period",        PayloadProperty = "VisaPeriod",        Kind = ColumnKind.LookupByName, LookupEntity = "VisaPeriod" },
                new() { Header = "Visa Type",          PayloadProperty = "VisaType",          Kind = ColumnKind.LookupByName, LookupEntity = "VisaType" },
                new() { Header = "From City",                 PayloadProperty = "FromCity",                Kind = ColumnKind.LookupByName, LookupEntity = "City" },
                new() { Header = "To City",                   PayloadProperty = "ToCity",                  Kind = ColumnKind.LookupByName, LookupEntity = "City" },
                new() { Header = "Movement Permit Location",  PayloadProperty = "MovementPermitLocation",  Kind = ColumnKind.LookupByName, LookupEntity = "MovementPermitLocation" },
                new() { Header = "Border Zone Location",      PayloadProperty = "BorderZoneLocation",      Kind = ColumnKind.LookupByName, LookupEntity = "BorderZoneLocation" },
                new() { Header = "Business Trip Start Date",  PayloadProperty = "BusinessTripStartDate",   Kind = ColumnKind.Scalar },
                new() { Header = "Business Trip End Date",    PayloadProperty = "BusinessTripEndDate",     Kind = ColumnKind.Scalar },
                new() { Header = "Business Trip Purpose",     PayloadProperty = "BusinessTripPurpose",     Kind = ColumnKind.LookupByName, LookupEntity = "BusinessTripPurpose" },
            }
        },
        // Visa — must come BEFORE ApplicationItems so that ApplicationItem.CurrentVisa lookups
        // succeed when a new visa is seeded in the same scenario (e.g. Visa extension scenarios).
        // Visas must come before ApplicationItems (CurrentVisa lookup).
        new SheetMap { SheetName = "Visas",         EntityName = "Visa",          DisplayName = "Visa",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "VisaNumber", Header = "Visa Number" } },
            Columns = new() {
                new() { Header = "Visa Number",      PayloadProperty = "VisaNumber",          Kind = ColumnKind.Scalar,       Required = true },
                new() { Header = "Issue Date",       PayloadProperty = "IssueDate",           Kind = ColumnKind.Scalar,       Required = true },
                new() { Header = "Start Date",       PayloadProperty = "StartDate",           Kind = ColumnKind.Scalar },
                new() { Header = "Expiration Date",  PayloadProperty = "ExpirationDate",      Kind = ColumnKind.Scalar },
                new() { Header = "Notes",            PayloadProperty = "Notes",               Kind = ColumnKind.Scalar },
                new() { Header = "Border Zone Location", PayloadProperty = "BorderZoneLocation", Kind = ColumnKind.Scalar },
                new() { Header = "Extension Required", PayloadProperty = "ExtensionRequired", Kind = ColumnKind.Bool },
                new() { Header = "Visa Type",        PayloadProperty = "VisaType",            Kind = ColumnKind.LookupByName, LookupEntity = "VisaType",        Required = true },
                new() { Header = "Visa Category",    PayloadProperty = "VisaCategory",        Kind = ColumnKind.LookupByName, LookupEntity = "VisaCategory" },
                new() { Header = "Issued Place",     PayloadProperty = "VisaIssuedPlace",     Kind = ColumnKind.LookupByName, LookupEntity = "VisaIssuedPlace" },
                new() { Header = "Passport Number",  PayloadProperty = "Passport",            Kind = ColumnKind.LookupByName, LookupEntity = "Passport", LookupFilterProperty = "PassportNumber" },
                new() { Header = "Application Item", PayloadProperty = "IssuingApplicationItem", Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationItem", LookupFilterProperty = "ApplicationItemName" },
                new() { Header = "Invitation Item",  PayloadProperty = "InvitationItem",      Kind = ColumnKind.LookupByName, LookupEntity = "InvitationItem", LookupFilterProperty = "InvitationItemName" },
            }
        },

        // ApplicationItem — depends on Application, Person, Passport, and optionally Visa.
        // Must come AFTER Visas so CurrentVisa lookups succeed.
        // Upsert in sync mode uses ApplicationItemName = "{Person} - {Application}" (see ExcelImporter).
        new SheetMap { SheetName = "ApplicationItems", EntityName = "ApplicationItem", DisplayName = "Application Item",
            Columns = new() {
                new() { Header = "Application",        PayloadProperty = "Application",              Kind = ColumnKind.LookupByName,      LookupEntity = "Application",              LookupFilterProperty = "FullApplicationNumber", Required = true },
                new() { Header = "Person",             PayloadProperty = "Person",                   Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Number",    PayloadProperty = "CurrentPassport",          Kind = ColumnKind.LookupByName,      LookupEntity = "Passport",      LookupFilterProperty = "PassportNumber", Required = true },
                new() { Header = "Visa Number",        PayloadProperty = "CurrentVisa",              Kind = ColumnKind.LookupByName,      LookupEntity = "Visa",          LookupFilterProperty = "VisaNumber" },
                new() { Header = "Next Visa Number",   PayloadProperty = "NextVisa",                 Kind = ColumnKind.LookupByName,      LookupEntity = "Visa",          LookupFilterProperty = "VisaNumber" },
                new() { Header = "Position History",   PayloadProperty = "CurrentPositionHistory",   Kind = ColumnKind.LookupByName,      LookupEntity = "EmployeePositionHistory", LookupFilterProperty = "Position/Name" },
                new() { Header = "Contract",           PayloadProperty = "CurrentEmployeeContract",  Kind = ColumnKind.LookupByName,      LookupEntity = "EmployeeContract", LookupFilterProperty = "ContractStartDate" },
                new() { Header = "Work Duty",          PayloadProperty = "CurrentWorkDuty",          Kind = ColumnKind.LookupByName,      LookupEntity = "WorkDuty",       LookupFilterProperty = "Description" },
                new() { Header = "Work Permitted Locations", PayloadProperty = "WorkPermittedLocations", Kind = ColumnKind.Scalar },
                new() { Header = "Previous Passport",  PayloadProperty = "PreviousPassport",         Kind = ColumnKind.LookupByName,      LookupEntity = "Passport",      LookupFilterProperty = "PassportNumber" },
                new() { Header = "Work Permit Item",   PayloadProperty = "CurrentWorkPermitItem",    Kind = ColumnKind.LookupByName,      LookupEntity = "WorkPermitItem", LookupFilterProperty = "WorkPermitNumber" },
                new() { Header = "Previous Work Permit Item", PayloadProperty = "PreviousWorkPermitItem", Kind = ColumnKind.LookupByName, LookupEntity = "WorkPermitItem", LookupFilterProperty = "WorkPermitNumber" },
                new() { Header = "Invitation Item",    PayloadProperty = "CurrentInvitationItem",    Kind = ColumnKind.LookupByName,      LookupEntity = "InvitationItem", LookupFilterProperty = "InvitationItemName" },
                new() { Header = "Address",            PayloadProperty = "CurrentAddressOfResidence", Kind = ColumnKind.LookupByName,      LookupEntity = "AddressOfResidence", LookupFilterProperty = "FullAddress" },
                new() { Header = "Registration Date",  PayloadProperty = "RegistrationDate",         Kind = ColumnKind.Scalar },
                new() { Header = "Travel Date",        PayloadProperty = "TravelDate",               Kind = ColumnKind.Scalar },
                new() { Header = "Travel Type",        PayloadProperty = "TravelType",               Kind = ColumnKind.Scalar },
                new() { Header = "Movement Type",      PayloadProperty = "MovementType",             Kind = ColumnKind.Scalar },
                new() { Header = "Check Point",        PayloadProperty = "CheckPoint",               Kind = ColumnKind.LookupByName,      LookupEntity = "CheckPoint",      LookupFilterProperty = "Name" },
                new() { Header = "Travel Notes",     PayloadProperty = "TravelNotes",              Kind = ColumnKind.Scalar },
                new() { Header = "Business Trip Address", PayloadProperty = "BusinessTripAddress", Kind = ColumnKind.LookupByName,      LookupEntity = "BusinessTripAddress", LookupFilterProperty = "FullAddress" },
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

        // BusinessTripAddress — linked from ApplicationItem on business-trip application types.
        new SheetMap { SheetName = "BusinessTripAddress", EntityName = "BusinessTripAddress", DisplayName = "Business Trip Address",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "City/ID", Header = "City", FromPayload = true, PayloadProperty = "City" },
                new UpsertKeyPart { ODataProperty = "FullAddress", Header = "Full Address" },
            },
            Columns = new() {
                new() { Header = "City",         PayloadProperty = "City",        Kind = ColumnKind.LookupByName, LookupEntity = "City" },
                new() { Header = "Full Address", PayloadProperty = "FullAddress", Kind = ColumnKind.Scalar, Required = true },
            }
        },

        new SheetMap { SheetName = "Invitations", EntityName = "Invitation", DisplayName = "Invitation",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "InvitationNumber", Header = "Invitation Number" } },
            Columns = new() {
                new() { Header = "Invitation Number", PayloadProperty = "InvitationNumber", Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Start Date",        PayloadProperty = "StartDate",        Kind = ColumnKind.Scalar,      Required = true },
                new() { Header = "Application",       PayloadProperty = "Application",      Kind = ColumnKind.LookupByName, LookupEntity = "Application", LookupFilterProperty = "FullApplicationNumber" },
                new() { Header = "Validity Duration", PayloadProperty = "ValidityDuration", Kind = ColumnKind.LookupByName, LookupEntity = "ValidityDuration" },
                new() { Header = "Is Cancelled",      PayloadProperty = "IsCancelled",      Kind = ColumnKind.Bool },
            }
        },
        // Upsert in sync mode uses InvitationItemName = "{Person} - {Invitation Number}" (see ExcelImporter).
        new SheetMap { SheetName = "InvitationItems", EntityName = "InvitationItem", DisplayName = "Invitation Item",
            Columns = new() {
                new() { Header = "Invitation Number", PayloadProperty = "Invitation",       Kind = ColumnKind.LookupByName, LookupEntity = "Invitation", LookupFilterProperty = "InvitationNumber", Required = true },
                new() { Header = "Person",            PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Number",   PayloadProperty = "Passport",         Kind = ColumnKind.LookupByName, LookupEntity = "Passport",   LookupFilterProperty = "PassportNumber", Required = true },
                new() { Header = "Is Used",           PayloadProperty = "IsUsed",           Kind = ColumnKind.Bool },
                new() { Header = "Is Cancelled",      PayloadProperty = "IsCancelled",      Kind = ColumnKind.Bool },
                new() { Header = "Is Changed",        PayloadProperty = "IsChanged",        Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "WorkPermits", EntityName = "WorkPermit", DisplayName = "Work Permit",
            UpsertKeys = new[] { new UpsertKeyPart { ODataProperty = "WorkPermitNumber", Header = "Work Permit Number" } },
            Columns = new() {
                new() { Header = "Work Permit Number", PayloadProperty = "WorkPermitNumber", Kind = ColumnKind.StringValue, Required = true },
                // WorkPermit BO uses IssuedDate (mapped to DB column StartDate).
                // OData payload must send IssuedDate, not StartDate.
                new() { Header = "Start Date",         PayloadProperty = "IssuedDate",       Kind = ColumnKind.Scalar,      Required = true },
                new() { Header = "Application",        PayloadProperty = "Application",      Kind = ColumnKind.LookupByName, LookupEntity = "Application", LookupFilterProperty = "FullApplicationNumber" },
                new() { Header = "Is Cancelled",       PayloadProperty = "IsCancelled",      Kind = ColumnKind.Bool },
            }
        },
        new SheetMap { SheetName = "WorkPermitItems", EntityName = "WorkPermitItem", DisplayName = "Work Permit Item",
            UpsertKeys = new[]
            {
                new UpsertKeyPart { ODataProperty = "Person/ID", Header = "Person", FromPayload = true, PayloadProperty = "Person" },
                new UpsertKeyPart { ODataProperty = "WorkPermitNumber", Header = "Item Number" },
            },
            Columns = new() {
                new() { Header = "Work Permit Number", PayloadProperty = "WorkPermit",       Kind = ColumnKind.LookupByName, LookupEntity = "WorkPermit", LookupFilterProperty = "WorkPermitNumber", Required = true },
                new() { Header = "Item Number",        PayloadProperty = "WorkPermitNumber", Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Person",             PayloadProperty = "Person",           Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Passport Number",    PayloadProperty = "Passport",         Kind = ColumnKind.LookupByName, LookupEntity = "Passport", LookupFilterProperty = "PassportNumber", Required = true },
                new() { Header = "Position History",   PayloadProperty = "CurrentPositionHistory", Kind = ColumnKind.LookupByName, LookupEntity = "EmployeePositionHistory", LookupFilterProperty = "Position/Name", Required = true },
                new() { Header = "Start Date",         PayloadProperty = "StartDate",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Expiration Date",    PayloadProperty = "ExpirationDate",   Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "AS Number",          PayloadProperty = "ASNumber",         Kind = ColumnKind.StringValue },
                new() { Header = "Is Cancelled",       PayloadProperty = "IsCancelled",      Kind = ColumnKind.Bool },
                new() { Header = "Is Changed",         PayloadProperty = "IsChanged",        Kind = ColumnKind.Bool },
                new() { Header = "Is Extended",        PayloadProperty = "IsExtended",       Kind = ColumnKind.Bool },
                new() { Header = "Work Permitted Locations", PayloadProperty = "WorkPermittedLocations", Kind = ColumnKind.Scalar },
            }
        },

        new SheetMap { SheetName = "Rejections", EntityName = "Rejection", DisplayName = "Rejection",
            Columns = new() {
                new() { Header = "Rejection Number", PayloadProperty = "RejectedDocNumber", Kind = ColumnKind.StringValue, Required = true },
                new() { Header = "Date",             PayloadProperty = "Date",              Kind = ColumnKind.Scalar,      Required = true },
                new() { Header = "Reason",           PayloadProperty = "Reason",            Kind = ColumnKind.Scalar },
                new() { Header = "Application",      PayloadProperty = "Application",       Kind = ColumnKind.LookupByName, LookupEntity = "Application", LookupFilterProperty = "FullApplicationNumber" },
            }
        },
        new SheetMap { SheetName = "RejectionItems", EntityName = "RejectionItem", DisplayName = "Rejection Item",
            Columns = new() {
                new() { Header = "Rejection Number", PayloadProperty = "Rejection",         Kind = ColumnKind.LookupByName, LookupEntity = "Rejection", LookupFilterProperty = "RejectedDocNumber", Required = true },
                new() { Header = "Person",            PayloadProperty = "Person",            Kind = ColumnKind.PersonLookupByName, Required = true },
                new() { Header = "Reason",            PayloadProperty = "Reason",            Kind = ColumnKind.Scalar },
            }
        },

        // ApplicationProgresses — depends on Application and ApplicationState/ApplicationLocation lookups.
        new SheetMap { SheetName = "ApplicationProgresses", EntityName = "ApplicationProgress", DisplayName = "Application Progress",
            Columns = new() {
                new() { Header = "Application", PayloadProperty = "Application", Kind = ColumnKind.LookupByName, LookupEntity = "Application", LookupFilterProperty = "FullApplicationNumber", Required = true },
                new() { Header = "State",       PayloadProperty = "State",       Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationState",    LookupFilterProperty = "Code", Required = true },
                new() { Header = "Location",    PayloadProperty = "Location",    Kind = ColumnKind.LookupByName, LookupEntity = "ApplicationLocation",  LookupFilterProperty = "Code", Required = true },
                new() { Header = "Date",        PayloadProperty = "Date",        Kind = ColumnKind.Scalar, Required = true },
                new() { Header = "Description", PayloadProperty = "Description", Kind = ColumnKind.Scalar },
            }
        },
    };
}