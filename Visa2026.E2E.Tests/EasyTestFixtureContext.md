# EasyTestFixtureContext Documentation

## Overview
The `EasyTestFixtureContext` class is the central entry point for configuring and managing the lifecycle of End-to-End (E2E) tests in DevExpress XAF applications. It acts as the bridge between the xUnit test runner and the Blazor application under test.

## Key Capabilities

### 1. Configuration
It allows you to define the environment in which the test runs:
- **Applications**: Specifies where the build artifacts are located and how to launch them (e.g., Blazor Server vs. WinForms).
- **Databases**: Defines connection strings and database aliases used during testing.

### 2. Lifecycle Management
- **Setup**: Prepares the database (e.g., dropping it to ensure a clean state).
- **Execution**: Launches the application and provides contexts for interaction.
- **Teardown**: Closes browser windows and application processes to free resources.

## Key Methods and Properties

### `RegisterApplications(params TestApplicationOptions[] options)`
Registers the applications to be tested.
- **Parameters**: Accepts `BlazorApplicationOptions` or `WinApplicationOptions`.
- **Usage**:
  ```csharp
  FixtureContext.RegisterApplications(
      new BlazorApplicationOptions("Visa2026Blazor", @"Path\To\Project")
  );
  ```

### `RegisterDatabases(params DatabaseOptions[] options)`
Registers the databases used by the applications.
- **Parameters**: `DatabaseOptions` containing the alias, real DB name, and server instance.
- **Usage**:
  ```csharp
  FixtureContext.RegisterDatabases(
      new DatabaseOptions("AppDB", "Visa2026EasyTest", server: @"(localdb)\mssqllocaldb")
  );
  ```

### `DropDB(string databaseAlias)`
Drops the specified database to ensure the test starts with a clean slate.
- **Usage**: Typically called at the very beginning of a test method.

### `CreateApplicationContext(string applicationName)`
Creates an `IApplicationContext` for writing tests using the **C# API**.
- **Returns**: `IApplicationContext`
- **Usage**:
  ```csharp
  var appContext = FixtureContext.CreateApplicationContext("Visa2026Blazor");
  appContext.RunApplication();
  appContext.GetForm().FillForm(...);
  ```

### `GetTestExecutor(string applicationName)`
Creates a `TestExecutor` instance for running **.ets scripts**.
- **Returns**: `TestExecutor`
- **Usage**:
  ```csharp
  var executor = FixtureContext.GetTestExecutor("Visa2026Blazor");
  executor.ExecuteTestScript("sample.ets");
  ```

### `CloseRunningApplications()`
Terminates all applications and browser instances started by the fixture.
- **Usage**: Must be called in the `Dispose` method of the test class to prevent zombie processes.

## Comparison: C# API vs .ets Scripts

`EasyTestFixtureContext` supports both testing approaches.

| Feature | C# API (`CreateApplicationContext`) | .ets Scripts (`GetTestExecutor`) |
| :--- | :--- | :--- |
| **Language** | C# (Strongly Typed) | EasyTest Script (Domain Specific Language) |
| **Syntax** | `appContext.GetAction("Save").Execute();` | `*Action Save` |
| **Logic** | Full C# power (Loops, If/Else, Variables, LINQ) | Linear execution, limited control flow |
| **Debugging** | Standard Visual Studio Debugger (Breakpoints, Watch) | Limited debugging capabilities |
| **Refactoring** | Easy (Rename, Find Usages via IDE) | Harder (Requires text search/replace) |
| **Flexibility** | High (Can integrate with other .NET libraries) | Low (Restricted to EasyTest commands) |
| **Target Audience** | Developers | QA Engineers, Domain Experts |

## Summary

- Use **C# API** when you need complex logic, data setup, or want to leverage strong typing and refactoring tools.
- Use **.ets Scripts** for quick, readable, linear scenarios that act as "smoke tests" or when working with non-developer stakeholders.