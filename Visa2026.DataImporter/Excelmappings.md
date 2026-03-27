# Technical Reference: ExcelMappings.cs

## 1. Overview
`ExcelMappings.cs` serves as the metadata registry for the `Visa2026.DataImporter`. It defines how the system should interpret Excel files (headers, sheets, and cell values) and map them to Business Objects in the Visa2026 system. 

By centralizing this logic, the importer can remain generic; adding a new entity to the import process usually only requires adding a new `SheetMap` entry here rather than writing new C# logic.

## 2. Core Data Structures

### A. ColumnKind (Enum)
This enum determines the processing strategy for a specific cell value:

| Value | Behavior |
| :--- | :--- |
| `Scalar` | The default. Attempts to intelligently parse the string into `int`, `decimal`, `DateTime`, or `bool`. |
| `StringValue` | Forces the value to remain a string. Crucial for phone numbers or codes that might look like numbers but must not lose leading zeros. |
| `Bool` | Standardizes various inputs ("yes", "1", "true") into a boolean `true`/`false`. |
| `LookupByName` | **Crucial for Relationships.** Treats the cell text as a name, queries the API for that name, and returns an `ID` object (e.g., `{ "ID": "guid" }`). |
| `PersonLookupByName` | Specialized lookup that searches the `Person` entity using a `FullName` filter. |

### B. ColumnMap
Defines the metadata for an individual Excel column.
*   **Header**: The exact string expected in the first row of the Excel sheet.
*   **PayloadProperty**: The name of the property as defined in the OData API (PascalCase).
*   **Required**: If `true` and the cell is empty, the entire row is skipped.
*   **ValueMap**: A dictionary for inline substitutions (e.g., mapping an Excel "0" to the API string "Employee").

### C. SheetMap
Groups `ColumnMap` definitions and associates them with a specific Excel worksheet.
*   **SheetName**: The name of the tab in the `.xlsx` or `.xlsm` file.
*   **EntityName**: The OData endpoint name (e.g., `https://localhost:5001/api/odata/EntityName`).

## 3. Organizational Logic

The file contains two primary static lists in the `ExcelMappings` class:

### 1. `LookupSheets` (Phase 0)
Used primarily by `LookupSeeder.cs`. 
*   **Ordering is critical**: Tables are listed in dependency order. Independent tables (like `Country`) are listed first, followed by tables that reference them (like `City`, which requires a `Region`).
*   Targeted file: `lookup.xlsm`.

### 2. `Sheets` (Phase 4)
Used by `ExcelImporter.cs` for transactional data.
*   Handles complex onboarding entities like `Persons`, `Passports`, and `Applications`.
*   Heavily utilizes `PersonLookupByName` to link documents to specific people already in the system.

## 4. How Mapping Works (Step-by-Step)

When the `LookupSeeder` or `ExcelImporter` processes a file:
1.  It searches the Excel file for a tab matching the `SheetName`.
2.  It reads the first row to index the `Header` locations.
3.  For every data row:
    *   It iterates through the `Columns` defined in the `SheetMap`.
    *   It retrieves the raw text from the Excel cell based on the header index.
    *   If a `ValueMap` exists, it substitutes the value immediately.
    *   It applies the logic associated with the `ColumnKind` (e.g., performing a network lookup for a GUID).
    *   It adds the result to a `Dictionary<string, object?>`.
4.  The final dictionary is serialized to JSON and POSTed to the API.

## 5. Extension Guide: Adding a New Table

To add a new reference table to the system (e.g., a new "Industry" lookup):

1.  **Add the entry to `LookupSheets`**:
    ```csharp
    new SheetMap { 
        SheetName = "Industry", 
        EntityName = "Industry", 
        DisplayName = "Industry",
        Columns = new() {
            new() { Header = "Name", PayloadProperty = "Name", Kind = ColumnKind.Scalar, Required = true },
            new() { Header = "Code", PayloadProperty = "Code", Kind = ColumnKind.Scalar }
        }
    },
    ```
2.  **Update the Excel file**: Create a tab named "Industry" in `lookup.xlsm` with "Name" and "Code" columns.
3.  **Run the Importer**: The `LookupSeeder` will automatically detect the new mapping and the new sheet.

## 6. Best Practices

*   **Case Sensitivity**: While the headers in `ExcelMappings` are matched with `StringComparer.OrdinalIgnoreCase`, it is best practice to keep them identical to the Excel file for readability.
*   **Safety First**: Always use `ColumnKind.StringValue` for identifier codes or phone numbers to prevent the `ParseScalar` logic from stripping leading zeros or converting large IDs into scientific notation.
*   **Required Fields**: Always mark the "Name" or primary identifier column as `Required = true` to avoid sending incomplete/corrupt data to the API.

---
*Document Version: 1.0*  
*Project: Visa2026.DataImporter*
```

<!--
[PROMPT_SUGGESTION]Explain how the ValueMap in ColumnMap is used to handle C# enums during import.[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]How can I add a new transactional sheet to 'Sheets' for 'Vehicle' records?[/PROMPT_SUGGESTION]
