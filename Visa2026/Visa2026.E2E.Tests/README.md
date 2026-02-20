# Visa2026 End-to-End (E2E) Tests

## 1. Overview

This project contains the end-to-end (E2E) functional tests for the Visa2026 application. It is designed to simulate user interactions with the Blazor UI and verify the application's behavior from a user's perspective.

---

## 2. Frameworks and Tools

- **DevExpress EasyTest:** The core framework used for scripting and running the E2E tests. EasyTest provides a high-level, script-based approach to testing XAF applications.
- **xUnit:** The primary test runner for executing the test fixtures. The `[Theory]` and `[InlineData]` attributes are used to create and run tests.
- **Selenium WebDriver:** Used under the hood by the `DevExpress.ExpressApp.EasyTest.BlazorAdapter` to control the web browser and interact with the Blazor application.
- **.NET 8:** The target framework for the test project.

---

## 3. Project Structure

- **`Visa2026.E2E.Tests.csproj`**: The project file, containing all dependencies and configurations. It references the `Visa2026.Module` project to ensure it has context of the application's business objects.
- **`Tests.cs`**: The main test fixture class. It contains the xUnit tests that initialize the EasyTest environment, launch the Blazor application, and execute test scripts.
- **`Config.xml`**: The configuration file for EasyTest, defining application aliases, database connections, and other settings.
- **`*.ets` files**: These are the EasyTest script files. They contain a sequence of commands that represent user actions (e.g., navigating to a view, filling a form, clicking a button) and assertions to verify outcomes.

---

## 4. How It Works

1.  **Test Initialization**: An xUnit test method (e.g., `TestBlazorAppWithEts`) is executed.
2.  **Fixture Setup**: The `EasyTestFixtureContext` is initialized. It registers the Blazor application and the test database.
3.  **Database Reset**: The test database is dropped and recreated to ensure a clean state for each test run.
4.  **Application Launch**: The `RunApplication` method launches the Blazor application using the settings from `Config.xml`.
5.  **Test Script Execution**: The `ExecuteTest` method runs a specified `.ets` script file. The EasyTest adapter translates the script commands into Selenium WebDriver actions that are performed in the browser.
6.  **Assertions**: The script checks for expected outcomes, such as a specific view being displayed or data being saved correctly. If an assertion fails, the test fails.
7.  **Application Shutdown**: After the test completes, the `Dispose` method in `Visa2026Tests` closes the application.

---

## 5. Running the Tests

To run the functional tests:
1.  **Install Browser Drivers**: Ensure the appropriate Selenium browser driver (e.g., `chromedriver.exe` for Chrome) is installed and its location is included in the system's PATH variable.
2.  **Build the Solution**: Build the entire `Visa2026` solution in the `Debug` or `EasyTest` configuration.
3.  **Run via Test Explorer**: Open the Test Explorer in Visual Studio and run the tests from the `Visa2026.E2E.Tests` project.

---
