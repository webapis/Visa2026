# Technical Reference: LookupSeeder.cs

## 1. Overview
The `LookupSeeder` is a specialized engine within the `Visa2026.DataImporter` project responsible for **Phase 0: Infrastructure Seeding**. Its primary purpose is to populate the system's reference (lookup) tables—such as Countries, Visa Types, and Regions—from the `lookup.xlsm` workbook before any transactional data (like Persons or Applications) is imported.

## 2. Core Responsibilities

### A. Dependency-Aware Seeding
The seeder does not process sheets randomly. It follows the order defined in `ExcelMappings.LookupSheets`. This list is manually sorted to handle database constraints:
1.  **Independent Tables**: Tables with no foreign keys (e.g., `Country`, `Gender`).
2.  **Dependent Tables**: Tables that reference the independent ones (e.g., `City` which requires a `Region` ID).

### B. Idempotency (Skip if Exists)
To prevent duplicate records and allow the importer to be run multiple times safely, the seeder performs a check before processing any sheet:
```csharp
var existing = await _api.QueryAsync<IdHolder>(sheetMap.EntityName, "$top=1");
if (existing.Any()) {
    // Skip the sheet if the table already contains data
}
```

### C. Lookup Resolution & Caching
When a sheet contains a relationship (e.g., a City row mentioning "Balkan" as its Region), the seeder uses `ResolveLookupByNameAsync`. 
*   It queries the API for the entity name where `Name eq 'Balkan'`.
*   It extracts the `ID` (Guid).
*   It stores the result in an internal `_lookupCache` (Dictionary) to avoid redundant API calls for subsequent rows referencing the same value.

## 3. The Import Pipeline

For every row in an Excel sheet, the seeder performs the following steps:

1.  **Header Mapping**: It builds a map of Column Name → Column Index based on the first row of the sheet.
2.  **Sentinel Row Detection**: It skips rows starting with "Start " or "End " (internal markers often used by Excel plugins like SaveToDB).
3.  **Field Parsing**:
    *   **Scalars**: Attempts to parse values as `int`, `decimal`, or `DateTime`.
    *   **Booleans**: Standardizes "yes", "true", or "1" as `true`; "no", "false", or "0" as `false`.
    *   **Lookups**: Converts human-readable names into the `{ ID = guid }` format required by the OData API.
4.  **Payload Construction**: Builds a `Dictionary<string, object?>` containing only the mapped properties.
5.  **API Submission**: Sends a `POST` request via the `ApiClient`. If the server returns an error, the seeder logs the failed payload for debugging.

## 4. Key Methods

| Method | Description |
| :--- | :--- |
| `SeedAllAsync(path)` | Iterates through all mappings in `ExcelMappings.LookupSheets` and seeds them if the sheets exist in the file. |
| `SeedOneAsync(path, entity)` | Targets a specific entity (e.g., "Country") for isolated seeding. |
| `ResolveLookupByNameAsync` | The "glue" logic that finds GUIDs for text values using the API and a local cache. |
| `ParseScalar` | Logic to intelligently guess the type of an Excel cell string (Int > Decimal > Bool > Date > String). |

## 5. Handling Excel Quirks
The seeder includes specific logic to handle common Excel export/formula artifacts:
*   **Formula Placeholders**: Skips values starting with `=DETERMINISTIC_GUID`.
*   **Library Metadata**: Skips cells containing `<openpyxl` (often left by Python-based Excel tools).
*   **Empty Rows**: Dynamically detects and ignores rows where all cells are whitespace or null.

## 6. Error Handling Strategy
*   **Missing Columns**: If a column defined in `ExcelMappings` is missing from the Excel sheet, the seeder logs a warning but continues, omitting that property from the API payload.
*   **API Failures**: If a `POST` fails, the error is caught, the specific row number is identified, and the first 300 characters of the JSON payload are dumped to the console to help identify validation issues.
*   **Duplicate Detection**: Beyond the initial sheet-level check, it relies on the `ApiClient` to handle and log duplicate key exceptions from the server.

## 7. Integration with Program.cs
In the main `Program.cs` orchestration, `LookupSeeder` is executed in **Phase 0**. 
```csharp
await lookupSeeder.SeedAllAsync("lookup.xlsm");
```
Following this, `BaseImporter<object>.ClearLookupCache()` is called to ensure that subsequent transactional importers start with a clean state before they begin resolving their own IDs.

## 8. Requirements for `lookup.xlsm`
For a sheet to be processed by the `LookupSeeder`:
1.  The sheet name must match the `SheetName` in `ExcelMappings.cs`.
2.  The first row must contain headers matching the `Header` properties in the mapping.
3.  Data should typically start on row 2.

---
*Document Version: 1.0*  
*Project: Visa2026.DataImporter*
```

<!--
[PROMPT_SUGGESTION]How do I add a new lookup table to the LookupSeeder process?[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]Explain the relationship between LookupSeeder.cs and ExcelMappings.cs.[/PROMPT_SUGGESTION]
