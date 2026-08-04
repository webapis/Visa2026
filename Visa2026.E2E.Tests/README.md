# Visa2026 End-to-End (E2E) Tests

## 1. Overview

This project contains the end-to-end (E2E) functional tests for the Visa2026 application. It is designed to simulate user interactions with the Blazor UI and verify the application's behavior from a user's perspective.

---

## 2. Frameworks and Tools

- **Microsoft Playwright:** Officer E2E for **Local** (`:5050`) and **Staging** (live URL). Uses injected `e2e-*` CSS locators. See `Playwright/` folder.
- **DevExpress EasyTest:** Legacy framework (still in repo for existing journeys). Prefer Playwright for new manual media work.
- **xUnit:** The primary test runner for executing the test fixtures. The `[Theory]` and `[InlineData]` attributes are used to create and run tests.
- **Selenium WebDriver:** Used under the hood by the `DevExpress.ExpressApp.EasyTest.BlazorAdapter` to control the web browser and interact with the Blazor application.
- **.NET 8:** The target framework for the test project.

---

## 3. Project Structure

- **`Visa2026.E2E.Tests.csproj`**: The project file, containing all dependencies and configurations. It references the `Visa2026.Module` project to ensure it has context of the application's business objects.
- **`PersonOfficerJourneyTests.cs`**: Officer Person master-data CRUD journey (`E2E-001`…`E2E-008`) — login, create employee, passport→visa, education, address, medical CRUD, position, work duty, salary, travel; inherits `E2ETestBase` (+ `E2ETestBase.PersonMasterData.cs`).
- **`scenarios/`**: Scenario maps (`*_map.md`) and yaml **specs** (Option A — C# executes steps; see [scenarios/README.md](./scenarios/README.md)).
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

### Playwright E2E (Local + Staging)

```powershell
dotnet build Visa2026.slnx -c EasyTest
.\Visa2026.E2E.Tests\bin\EasyTest\net8.0\playwright.ps1 install msedge

# Local — fresh DB + :5050
.\scripts\local\Record-PlaywrightE2e.ps1 -Target Local

# Staging — live URL (manual)
.\scripts\local\Record-PlaywrightE2e.ps1 -Target Staging -BaseUrl 'https://10.100.128.25:8080'
```

Environment:

| Variable | Purpose |
|----------|---------|
| `VISA2026_E2E_TARGET` | `Local` (default) or `Staging` |
| `VISA2026_E2E_BASE_URL` | Override app URL |
| `VISA2026_E2E_USER` / `VISA2026_E2E_PASSWORD` | Officer credentials (staging) |
| `VISA2026_E2E_SCREENSHOTS` | `false` to disable milestone PNGs |

Filter: `dotnet test ... --filter "Driver=Playwright&E2ETarget=Local"`.

---
1.  **Prerequisites**: Ensure that the appropriate Selenium browser driver is installed and its path is added to the system's PATH environment variable.
2.  **Build**: Build the solution with the **`EasyTest`** configuration (required for the Blazor host under test):

    ```powershell
    dotnet build Visa2026.slnx -c EasyTest
    ```
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
1.  **Initialization**: The test runner initializes the test fixture. The existing PostgreSQL test database (`visa2026_easytest`) is dropped and recreated to ensure a clean environment.
2.  **Launch**: On local dev, a visible Microsoft Edge window opens (headed). On CI (`CI=true` / `VISA2026_E2E_HEADLESS`), Edge runs headless — see `EasyTestBrowserMode.cs`.
3.  **Navigation**: The browser will navigate to the local URL of the Blazor application (e.g., `http://localhost:5050`).
4.  **Simulation**: You will see the browser automatically interacting with the application based on the `.ets` script.
5.  **Completion**: Once the script finishes, the browser window will close automatically.

### Troubleshooting: HTTP 404 on `localhost:65201`

EasyTest must open **`http://localhost:5050`**, not **5000** (IDE dev). `E2ETestBase` registers the built **`Visa2026.Blazor.Server.exe`** (not the project folder) with **`--urls http://localhost:5050 --environment Development`** — `--launch-profile` only works with `dotnet run` and leaves the standalone host on **:5000** (`ERR_CONNECTION_REFUSED` on **:5050**). After each test preflight, `EasyTestDatabaseProvisioner` drops/creates **`visa2026_easytest`** on PostgreSQL and runs **`--updateDatabase --silent`**. Build **EasyTest** (`dotnet build Visa2026.slnx -c EasyTest`). Requires local PostgreSQL (`localhost:5432`, password from `PG_PASSWORD` or default `Visa2026Local`). Optional: `msedgedriver.exe` in `Visa2026.E2E.Tests\.webdrivers\`.

### Expected Results
- **Test Explorer**:
    - **Green Checkmark**: Indicates the test passed successfully.
    - **Red X**: Indicates the test failed.
- **Logs**: If a test fails, the Test Explorer output will provide a log detailing the step where the failure occurred.
https://msedgewebdriverstorage.z22.web.core.windows.net/?prefix=145.0.3800.65/