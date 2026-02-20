# Visa2026 E2E Testing Best Practices

## 1. Testing Philosophy
The goal of End-to-End (E2E) testing in Visa2026 is to verify that business processes work as expected from a user's perspective. Tests should be **deterministic**, meaning they should produce the same result every time they run.

To achieve this:
- **Isolate Tests**: Each test should run in a clean state (Database is reset).
- **Control Data**: Do not rely on random demo data. Tests should create the specific data they need.
- **Focus on Business Value**: Prioritize testing critical flows (e.g., Visa Application process) over trivial property setters.

---

## 2. Data Management Strategy

### Lookup Data (Keep)
**Definition**: Static reference data required for the app to function (e.g., `Countries`, `VisaTypes`, `ApplicationTypes`).
**Strategy**: Always seed this data. Tests should rely on it existing.
**Why**: It is inefficient to recreate "Turkmenistan" or "Business Visa" in every single test script.

### Demo Data (Exclude)
**Definition**: Fake transactional data used for development demos (e.g., `Employees`, `Passports` generated in `Updater.cs`).
**Strategy**: **Disable seeding** of this data during EasyTest runs.
**Why**: 
- If a test expects "0 Employees" to verify a list is empty, seeded data breaks it.
- If a test edits "Employee A", a subsequent test might fail if it expects "Employee A" in its original state.

*Note: Ensure `Updater.cs` wraps demo data creation in `#if !EASYTEST`.*

---

## 3. Testing Scenarios & Patterns

### A. CRUD Operations (C# API)
For every major Business Object (e.g., `Employee`, `Application`), create a test method that verifies the lifecycle using `IApplicationContext`.

**Pattern (`EmployeeTests.cs`):**
```csharp
[Fact]
public void Employee_CRUD_Lifecycle()
{
    var appContext = FixtureContext.CreateApplicationContext("Visa2026Blazor");
    appContext.RunApplication();

    // 1. Create
    appContext.GetNavigationAction("Employee").Execute();
    appContext.GetAction("New").Execute();
    appContext.GetForm().FillForm(new { FirstName = "John", LastName = "Doe" });
    appContext.GetAction("Save").Execute();

    // Verify Creation
    Assert.Equal("John", appContext.GetForm().GetFieldValue("FirstName"));

    // 2. Update
    appContext.GetAction("Edit").Execute();
    appContext.GetForm().FillForm(new { LastName = "Smith" });
    appContext.GetAction("Save").Execute();
    
    // Verify Update
    Assert.Equal("Smith", appContext.GetForm().GetFieldValue("LastName"));

    // 3. Delete
    appContext.GetAction("Delete").Execute();
    // Handle confirmation dialog if applicable
}
```

### B. Validations (Constraints)
Verify that the system prevents invalid data entry.

**Pattern:**
```csharp
[Fact]
public void Employee_Validation_RequiredFields()
{
    var appContext = FixtureContext.CreateApplicationContext("Visa2026Blazor");
    appContext.RunApplication();

    appContext.GetNavigationAction("Employee").Execute();
    appContext.GetAction("New").Execute();
    
    // Try to save without filling mandatory fields
    try {
        appContext.GetAction("Save").Execute();
    } catch (AdapterOperationException) {
        // Expected: Validation error prevents saving
    }
    
    // Verify error message exists
    // Note: Specific verification depends on how validation errors are exposed in the UI adapter
}
```

### C. Read-Only & Appearance Rules
Verify that logic correctly hides/shows fields based on state.

**Pattern:**
1.  Create a new `Application`.
2.  Select an `ApplicationType` known to hide specific fields.
3.  **Verify**: Assert that `appContext.GetForm().GetField("WorkPermit")` throws an exception or returns null/hidden state.
4.  Change `ApplicationType` to one that shows fields.
5.  **Verify**: Assert that the field is now accessible.

---

## 4. Per-Test Seeding & Cleanup

### Seeding Data
Since global demo data is disabled, tests must create their own prerequisites.

**Approach 1: UI-Driven Seeding (Standard)**
Perform "Arrange" steps via the UI.
*Example*: To test "Assign Employee to Project", first create the Employee and Project via the UI in the same test method.

**Approach 2: Database Seeding (Performance)**
For static prerequisites (e.g., 100 existing records), insert directly into the SQL database using EF Core before calling `RunApplication()`.
*Note*: Requires `Visa2026.Module` reference in the Test project.

### Clearing Data
**Do not rely on "Cleanup at End".**
If a test fails or crashes, teardown logic might be skipped, leaving the database dirty for the next test.

**Best Practice: Clean on Start**
1.  **Start of Test**: Call `FixtureContext.DropDB()` (or equivalent reset method).
2.  **End of Test**: Just close the application (`Dispose`).

```csharp
public class EmployeeTests : IDisposable
{
    public EmployeeTests()
    {
        // Always start clean to ensure isolation
        FixtureContext.DropDB("Visa2026EasyTest"); 
    }
    
    // ... tests ...
}
```

---

## 5. Naming Conventions

### Test Classes (`.cs`)
Format: `[Module/BO]Tests.cs`

- `EmployeeTests.cs`
- `ApplicationWorkflowTests.cs`
- `SecurityTests.cs`

### Test Methods
Format: `[Feature]_[Scenario]`

- `CRUD_Lifecycle`
- `Validation_RequiredFields`
- `Login_InvalidPassword`

---

## 6. Common C# EasyTest API Reference

- **Start App**: `appContext.RunApplication();`
- **Navigate**: `appContext.GetNavigationAction("Employee").Execute();`
- **Fill Form**: `appContext.GetForm().FillForm(new { Name = "John Doe" });`
- **Get Field Value**: `var val = appContext.GetForm().GetFieldValue("Name");`
- **Click Action**: `appContext.GetAction("Save").Execute();`
- **Grid Interaction**: 
  ```csharp
  var grid = appContext.GetGrid("Employees");
  grid.ProcessRow(new { Name = "John Doe" }); // Click row
  ```