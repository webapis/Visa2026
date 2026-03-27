# Technical Reference: Program.cs (Orchestration Engine)

## 1. Overview
`Program.cs` is the entry point for the `Visa2026.DataImporter` console application. Its primary role is **orchestration**: it manages the sequence of data ingestion, handles authentication, ensures system availability, and provides detailed logging for the entire import lifecycle.

## 2. Startup & Connectivity
Before any data is processed, the application performs a "Self-Healing" startup sequence:

1.  **Wait-for-Server**: Since the importer often runs in Docker alongside the Blazor Server, it uses `api.WaitForServerAsync`. This polls the `/swagger/index.html` endpoint for up to 300 seconds, ensuring the database migrations and OData service are fully initialized before the first request.
2.  **Authentication**: It authenticates against the `AuthenticationController` using the `Admin` credentials to obtain a JWT token, which is then attached to all subsequent OData requests.

## 3. The 8-Phase Import Workflow

The data is imported in strict phases to prevent "Missing Parent" errors (Foreign Key violations).

### Phase 0: Seeding Lookups
Processes `lookup.xlsm` using the `LookupSeeder`. This populates the "infrastructure" of the system (Countries, Regions, Visa Types). This phase is **idempotent**—it skips tables that already contain data.

### Phase 1: Importer Initialization
Instantiates every specific importer class (e.g., `PersonImporter`, `PassportImporter`). This acts as the registry of capabilities for the current run.

### Phase 2: Prerequisite Verification
Uses `SafeQuery` to fetch the first record of critical lookup tables. If essential data (like `Country` or `ApplicationType`) is missing, the program aborts immediately to avoid cascading failures.

### Phase 3: Organizational Structure
Sets up the default environment:
*   Identifies the **Default Company** and **Project Contract**.
*   Creates the **Local Employee**, **Company Head**, and **Representative** records required to sign applications.

### Phase 4: Employee Onboarding
The core of the import. It looks for data in this priority:
1.  `data.xlsx`: Full relational dataset.
2.  `employees.xlsx`: Simple personnel list.
3.  `employees.csv`: Bulk CSV import fallback.
4.  **Demo Mode**: If no files are found, it creates a "John Smith" demo record to ensure the developer has data to work with.

It also links the Person to their Passport, Education, and Position History during this phase.

### Phase 5: Application Creation
Creates the `Application` record and the associated `ApplicationItem` that links a Person to a specific process.

### Phase 6: Application Documents
Generates the outcomes of the application:
*   Invitations
*   Work Permits
*   Visas
*   Registrations
*   Rejections (for testing error flows)

### Phase 7: Miscellaneous Metadata
Populates secondary records like Travel History, Business Trips, and Lodging details.

## 4. Helper Logic

### SafeQuery & SafeLookup
These static methods wrap OData queries in try-catch blocks. They are used to verify that lookups exist without crashing the orchestration if a single table is empty.

### Custom Logging (`Log` Class)
`Program.cs` contains a nested `Log` class that provides:
*   **Multi-target output**: Writes to the Console AND a timestamped log file (e.g., `import_20231027_1000.log`).
*   **Visual cues**: Uses `ConsoleColor` to distinguish between `Phase` (Cyan), `Step` (White), `Ok` (Green), and `Error` (Red).
*   **Thread Safety**: Uses a `lock` object to ensure logs from parallel tasks (if implemented) don't interleave characters.

## 5. Error Handling Strategy
The entire orchestration is wrapped in a global `try-catch`.
*   **Detailed Stack Traces**: On failure, it prints the exception type, message, and inner exception.
*   **Payload Logging**: Individual importers (invoked by `Program.cs`) are designed to log the JSON payload that caused a server-side `400 Bad Request`, making it easy to identify schema mismatches.

## 6. Execution Command-Line Args
| Argument | Description |
| :--- | :--- |
| `--verbose` / `-v` | Enables verbose logging in the `ApiClient`, showing raw JSON payloads for every POST and PATCH request. |

---
*Document Version: 1.0*  
*Project: Visa2026.DataImporter*
```

<!--
[PROMPT_SUGGESTION]How do I change the order of phases in Program.cs if I add a new entity?[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]Explain the 'demo fallback' logic in Phase 4 of Program.cs.[/PROMPT_SUGGESTION]
