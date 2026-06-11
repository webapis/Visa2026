using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests
{
    [SupportedOSPlatform("windows")]
    public abstract class E2ETestBase : IDisposable, IAsyncLifetime
    {
        protected const string BlazorAppName = "Visa2026Blazor";
        protected const string AppDBName = "Visa2026EasyTest";
        private const string TestAppUrl = "http://localhost:5050";
        private EasyTestFixtureContext FixtureContext { get; }
        private static bool _databaseDropped;

        protected IApplicationContext AppContext { get; }

        protected E2ETestBase()
        {
            FixtureContext = new EasyTestFixtureContext();

            var blazorServerPath = Path.GetFullPath(
                Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.Blazor.Server"));

            var webDriverPath = ResolveWebDriverDirectory();

            // Two-parameter ctor reads launchSettings and can open the wrong port (e.g. 65201 IIS Express).
            // Always pass Url + Configuration explicitly so browser and Kestrel use the same address.
            FixtureContext.RegisterApplications(
                new BlazorApplicationOptions(
                    name: BlazorAppName,
                    physicalPath: blazorServerPath,
                    url: TestAppUrl,
                    configuration: "EasyTest",
                    ignoreCase: true,
                    browser: "Edge",
                    arguments: "--launch-profile \"Visa2026 - EasyTest (LocalDB)\"",
                    webDriverPath: webDriverPath,
                    browserBinaryPath: string.Empty,
                    runHeadless: false)
            );

            FixtureContext.RegisterDatabases(
                new DatabaseOptions(
                    AppDBName,
                    "Visa2026EasyTest",
                    server: @"(localdb)\mssqllocaldb"
                )
            );

            AppContext = FixtureContext.CreateApplicationContext(BlazorAppName);
        }

        public Task InitializeAsync()
        {
            FixtureContext.CloseRunningApplications();

            if (!_databaseDropped)
            {
                FixtureContext.DropDB(AppDBName);
                _databaseDropped = true;
            }

            AppContext.RunApplication();
            return Task.CompletedTask;
        }

        protected void Login(string userName = "Admin", string password = "")
        {
            AppContext.GetForm().FillForm(
                new EasyTestParameter("User Name", userName),
                new EasyTestParameter("Password", password));
            AppContext.GetAction("Log In").Execute();
        }

        protected void NavigateEmployeesList()
        {
            const string listViewPath = E2ETestLoginValues.EmployeesListViewPath;

            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    EasyTestBlazorNavigationHelper.GoToRelativeUrl(AppContext, TestAppUrl, listViewPath);

                    if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, listViewPath)
                        && AppContext.GetAction("New") != null)
                    {
                        return;
                    }
                }
                catch (Exception) when (attempt < 29)
                {
                    // Blazor host / driver may still be starting.
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException(
                $"Could not open Employees list at /{listViewPath} (URL must contain '{listViewPath}').");
        }

        protected void AssertEmployeeDetailViewActive()
        {
            const string detailViewPath = E2ETestLoginValues.EmployeeDetailViewPath;
            if (!EasyTestBlazorNavigationHelper.UrlContains(AppContext, detailViewPath))
            {
                throw new InvalidOperationException(
                    $"Expected employee detail view '/{detailViewPath}' but browser URL is '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}'.");
            }
        }

        protected void ExecuteActionWithRetry(string actionCaption)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var action = AppContext.GetAction(actionCaption);
                if (action != null)
                {
                    action.Execute();
                    return;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException($"Action '{actionCaption}' was not found.");
        }

        /// <summary>
        /// Creates an employee with all required Person detail fields (mirrors person-employee-create scenario).
        /// Assumes login already completed. Does not verify list row — call <see cref="OpenEmployeeInListByPersonalNumber"/> after Save + navigate.
        /// </summary>
        protected void CreateEmployeeWithRequiredFields(
            string personalNumber = E2ETestEmployeeCreateValues.PersonalNumber,
            string firstName = E2ETestEmployeeCreateValues.FirstName,
            string lastName = E2ETestEmployeeCreateValues.LastName)
        {
            NavigateEmployeesList();
            ExecuteActionWithRetry("New");
            AssertEmployeeDetailViewActive();

            FillFormWithRetry(
                new EasyTestParameter("First Name", firstName),
                new EasyTestParameter("Last Name", lastName),
                new EasyTestParameter("Personal Number", personalNumber),
                new EasyTestParameter("Date of Birth", E2ETestEmployeeCreateValues.DateOfBirth),
                new EasyTestParameter("Birth Place", E2ETestEmployeeCreateValues.BirthPlace),
                new EasyTestParameter("Country of Birth", E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter("Gender", E2ETestEmployeeCreateValues.GenderDisplay),
                new EasyTestParameter("Marital Status", E2ETestEmployeeCreateValues.MaritalStatusDisplay),
                new EasyTestParameter("Nationality", E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter("Foreign Address", E2ETestEmployeeCreateValues.ForeignAddress),
                new EasyTestParameter("Foreign Address Country", E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter("Project Contract", E2ETestEmployeeCreateValues.ProjectContractDisplay),
                new EasyTestParameter("Company (Subcontractor)", E2ETestEmployeeCreateValues.SubcontractorDisplay));

            ExecuteActionWithRetry("Save");
        }

        protected void OpenEmployeeInListByPersonalNumber(string personalNumber)
        {
            NavigateEmployeesList();
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Personal Number", personalNumber));
            Assert.NotNull(AppContext.GetAction("Save"));
        }

        protected void FillFormWithRetry(params EasyTestParameter[] fields)
        {
            foreach (var field in fields)
            {
                FillSingleFieldWithRetry(field);
            }
        }

        private void FillSingleFieldWithRetry(EasyTestParameter field)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                try
                {
                    AppContext.GetForm().FillForm(field);
                    return;
                }
                catch (AdapterOperationException) when (attempt < 14)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            throw new InvalidOperationException($"Could not fill form field: {field.Name}.");
        }

        protected void CreateCountry(string name, string code)
        {
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetAction("New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", name), new EasyTestParameter("Code", code));
            AppContext.GetAction("Save").Execute();
        }

        protected void OpenCompanyProfile()
        {
            AppContext.Navigate("Organization.Company");
        }

        protected void OpenAuthorizedSignatory()
        {
            AppContext.Navigate("Organization.Authorized Signatory");
        }

        protected void OpenAuthorizedRepresentative()
        {
            AppContext.Navigate("Organization.Authorized Representative");
        }

        protected void CreateEmployee(string firstName, string lastName)
        {
            CreateEmployeeWithRequiredFields(
                personalNumber: E2ETestEmployeeCreateValues.PersonalNumber,
                firstName: firstName,
                lastName: lastName);
        }

        /// <summary>
        /// Opens Application detail, sets ministry type code (3 digits), and saves.
        /// <paramref name="selectionCode"/> must match a seeded <c>ApplicationType.SelectionCode</c> (e.g. 101 = App_Inv).
        /// </summary>
        protected void CreateApplicationWithTypeCode(string selectionCode)
        {
            AppContext.Navigate("Application");
            AppContext.GetAction("New").Execute();
            Assert.NotNull(AppContext.GetAction("Save"));

            if (!TryFillApplicationTypeQuickCode(selectionCode))
                PickApplicationTypeFromCodeList(selectionCode);

            AppContext.GetAction("Save").Execute();
        }

        /// <summary>
        /// Adds a new <c>ApplicationItem</c> on the current Application detail and selects <paramref name="personFullName"/> in the Person lookup.
        /// </summary>
        protected void AddApplicationItemWithPerson(string personFullName)
        {
            AppContext.GetAction("New").Execute();
            Assert.NotNull(AppContext.GetAction("Save"));

            SelectPersonFromLookup(personFullName);

            AppContext.GetAction("Save").Execute();
        }

        /// <summary>Sets a reference lookup (dropdown) by display text — e.g. Person full name.</summary>
        protected void SelectPersonFromLookup(string personFullName)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                try
                {
                    AppContext.GetForm().FillForm(new EasyTestParameter("Person", personFullName));
                    return;
                }
                catch (AdapterOperationException) when (attempt < 14)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            throw new InvalidOperationException($"Could not set Person lookup to '{personFullName}'.");
        }

        private bool TryFillApplicationTypeQuickCode(string selectionCode)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                try
                {
                    AppContext.GetForm().FillForm(new EasyTestParameter("Application Type Code", selectionCode));
                    return true;
                }
                catch (AdapterOperationException) when (attempt < 14)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }

            return false;
        }

        /// <summary>Fallback when the custom Blazor quick-code editor is not yet visible to EasyTest.</summary>
        private void PickApplicationTypeFromCodeList(string selectionCode)
        {
            try
            {
                AppContext.GetAction("Type codes").Execute();
            }
            catch (AdapterOperationException)
            {
                AppContext.GetAction("…").Execute();
            }

            AppContext.GetAction(selectionCode).Execute();
        }

        public void Dispose()
        {
            FixtureContext.CloseRunningApplications();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private static string ResolveWebDriverDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.CurrentDirectory, ".webdrivers"),
                Path.Combine(Environment.CurrentDirectory, @"..\..\..\.webdrivers"),
                Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.E2E.Tests\.webdrivers")
            };

            foreach (var path in candidates)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(Path.Combine(fullPath, "msedgedriver.exe")))
                    return fullPath;
            }

            return string.Empty;
        }
    }
}
