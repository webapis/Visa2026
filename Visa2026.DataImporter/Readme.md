# Visa2026.DataImporter

A standalone .NET 8 console application for importing and managing data in the **Visa2026** system via its Backend Web API (OData/REST). It handles JWT authentication automatically and provides a clean, reusable client layer for all CRUD operations.

---

## Purpose

The DataImporter is designed to run alongside (or independently of) the `Visa2026.Blazor.Server` application. Its primary use cases are:

- **Bulk data import** — load large datasets from external sources (CSV, Excel, databases) into Visa2026 without using the UI
- **Data migration** — move records from legacy systems into Visa2026
- **Automated data seeding** — populate reference data (Visa Types, etc.) in new environments
- **Scripted CRUD operations** — perform programmatic create, read, update, and delete operations against any exposed entity

---

## Project Structure

```
Visa2026.DataImporter/
├── ApiClient.cs          # HTTP client: authentication, CRUD, OData queries
├── Models.cs             # C# mirrors of Visa2026 business objects
├── VisaTypeImporter.cs   # Import logic for the VisaType entity
└── Program.cs            # Entry point — orchestrates the import workflow
```

### `ApiClient.cs`

The core reusable HTTP client. Responsibilities:

- **`WaitForServerAsync()`** — polls the Blazor Server until it's ready, with configurable timeout and retry interval. Prevents connection errors when both projects start simultaneously.
- **`LoginAsync()`** — authenticates against `POST /api/Authentication/Authenticate` and stores the returned JWT token for all subsequent requests.
- **`GetAllAsync<T>(entityName)`** — fetches all records from an OData entity set.
- **`GetByIdAsync<T>(entityName, id)`** — fetches a single record by its GUID key.
- **`CreateAsync<T>(entityName, payload)`** — creates a new record via HTTP POST.
- **`UpdateAsync(entityName, id, payload)`** — partially updates a record via HTTP PATCH.
- **`DeleteAsync(entityName, id)`** — deletes a record via HTTP DELETE.
- **`QueryAsync<T>(entityName, odataQuery)`** — fetches records with OData query options (`$filter`, `$orderby`, `$top`, `$skip`, etc.).

### `Models.cs`

Plain C# classes that mirror the shape of Visa2026 business objects as returned by the OData API. Property names use `[JsonPropertyName]` attributes to match the API's PascalCase field names.

Add a new class here whenever you expose a new entity in `Startup.cs` via `options.BusinessObject<T>()`.

### `VisaTypeImporter.cs`

Entity-specific import logic built on top of `ApiClient`. Demonstrates:

- Listing all records
- Filtered queries (e.g. only default visa types)
- Creating a single record
- Bulk importing from a list with per-record error handling
- Updating an existing record
- Deleting a record

Use this as a template when adding importers for other entities.

### `Program.cs`

The application entry point. It:

1. Waits for the Blazor Server to become available (up to 120 seconds)
2. Authenticates as the `Admin` user
3. Runs the import workflow
4. Handles connection and timeout errors with clear console output

---

## How It Works

```
Program.cs
    │
    ├── WaitForServerAsync()    Poll /swagger/index.html until 200 OK
    ├── LoginAsync()            POST /api/Authentication/Authenticate → JWT token
    │
    └── VisaTypeImporter
            ├── ListAllAsync()          GET  /api/odata/VisaType
            ├── BulkImportAsync()       POST /api/odata/VisaType  (per record)
            ├── CreateOneAsync()        POST /api/odata/VisaType
            ├── UpdateAsync()           PATCH /api/odata/VisaType({id})
            └── DeleteAsync()           DELETE /api/odata/VisaType({id})
```

All requests include the JWT token in the `Authorization: Bearer <token>` header. The XAF Security System on the server validates the token and enforces role-based permissions on every operation.

---

## Configuration

Edit the constants at the top of `Program.cs`:

```csharp
const string ApiBaseUrl = "https://localhost:5001";  // Blazor Server URL
const string UserName   = "Admin";
const string Password   = "";                         // empty = default Admin
```

For production, move these values to `appsettings.json` or environment variables.

---

## Running the Project

> **Important:** Start `Visa2026.Blazor.Server` first. The DataImporter will wait automatically for the server to be ready, but it must be running.

### Option A — Run manually (recommended)

1. Start `Visa2026.Blazor.Server` (F5 or Ctrl+F5)
2. Wait for it to fully load
3. Right-click `Visa2026.DataImporter` in Solution Explorer → **Debug → Start new instance**

### Option B — Multiple startup projects

In Visual Studio: right-click the Solution → **Properties → Configure Startup Projects**:

| Project | Action |
|---|---|
| `Visa2026.Blazor.Server` | Start |
| `Visa2026.DataImporter` | Start |
| Others | None |

The `WaitForServerAsync()` method handles the timing — the importer will automatically retry until the server is ready.

---

## Adding a New Entity

To import data for a different entity (e.g. `VisaApplication`):

**1. Expose the entity in `Visa2026.Blazor.Server/Startup.cs`:**
```csharp
services.AddXafWebApi(configuration, options =>
{
    options.BusinessObject<Visa2026.Module.BusinessObjects.VisaType>();
    options.BusinessObject<Visa2026.Module.BusinessObjects.VisaApplication>(); // add this
});
```

**2. Add a model class in `Models.cs`:**
```csharp
public class VisaApplication
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("ApplicantName")]
    public string ApplicantName { get; set; } = "";

    // ... other properties
}
```

**3. Create an importer class (copy `VisaTypeImporter.cs` as a template):**
```csharp
public class VisaApplicationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "VisaApplication";
    // ...
}
```

**4. Call it from `Program.cs`:**
```csharp
var appImporter = new VisaApplicationImporter(api);
await appImporter.BulkImportAsync(yourData);
```

---

## OData Query Examples

The `QueryAsync` method accepts any standard OData v4 query string:

```csharp
// Filter by field value
await api.QueryAsync<VisaType>("VisaType", "$filter=IsDefault eq true");

// Sort and limit
await api.QueryAsync<VisaType>("VisaType", "$orderby=Name asc&$top=10");

// Filter by string contains
await api.QueryAsync<VisaType>("VisaType", "$filter=contains(Name, 'Tourist')");

// Skip and take (paging)
await api.QueryAsync<VisaType>("VisaType", "$skip=20&$top=10");
```

---

## Dependencies

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.Http` | `HttpClient` factory support |
| `System.Text.Json` | JSON serialization / deserialization |

No DevExpress packages are required — the importer communicates through the standard HTTP/OData API.