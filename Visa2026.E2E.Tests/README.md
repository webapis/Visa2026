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
- **`Config.xml`**: The configuration file for EasyTest, defining application aliases, database connections, and other settings. The browser for testing is also specified here.
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

### User Actions
1.  **Prerequisites**: Ensure that the appropriate Selenium browser driver is installed and its path is added to the system's PATH environment variable.
2.  **Build**: Build the `Visa2026` solution in Visual Studio using the `Debug` or `EasyTest` configuration.
3.  **Execute**: Open the **Test Explorer** in Visual Studio (`Test` > `Test Explorer`). Locate the tests under the `Visa2026.E2E.Tests` project (e.g., `TestBlazorAppWithEts`). Right-click on a test and select **Run**.

### Verifying Browser Driver Configuration
The EasyTest framework is currently configured to use **Microsoft Edge**.

1.  **Download the Driver**:
    *   Download the `msedgedriver.exe` version that matches your installed Microsoft Edge version from the [official site](https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/).
2.  **Add to PATH**:
    *   Place `msedgedriver.exe` in a specific folder, for example, `C:\CWebDrivers`.
    *   Add this folder (`C:\CWebDrivers`) to your system's `PATH` environment variable.
3.  **Verification**:
    *   Open a **new** Command Prompt or PowerShell window.
    *   Type `msedgedriver.exe` and press Enter.
    *   **If configured correctly**, you will see a message like `Starting Microsoft Edge WebDriver...`.
    *   **If not configured**, you will see an error like `'msedgedriver.exe' is not recognized...`. This means the PATH is not set up correctly.

The application is configured to find the driver automatically through the system's PATH. The browser type is specified in `Config.xml` via the `Browser="Edge"` attribute.

### Application Behavior
1.  **Initialization**: The test runner initializes the test fixture. The existing test database (`Visa2026EasyTest`) is dropped and recreated to ensure a clean environment.
2.  **Launch**: A new Microsoft Edge browser window will automatically open.
3.  **Navigation**: The browser will navigate to the local URL of the Blazor application (e.g., `http://localhost:5000`).
4.  **Simulation**: You will see the browser automatically interacting with the application based on the `.ets` script.
5.  **Completion**: Once the script finishes, the browser window will close automatically.

### Expected Results
- **Test Explorer**:
    - **Green Checkmark**: Indicates the test passed successfully.
    - **Red X**: Indicates the test failed.
- **Logs**: If a test fails, the Test Explorer output will provide a log detailing the step where the failure occurred.
https://msedgewebdriverstorage.z22.web.core.windows.net/?prefix=145.0.3800.65/