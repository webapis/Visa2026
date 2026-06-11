using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Outcome shield after logon — authenticated shell with navigable Application list.
        /// Navigates to Application list and expects toolbar <c>New</c>.
        /// </summary>
        protected void AssertAuthenticatedAppShell()
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, "LoginPage"))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    AppContext.Navigate("Application");
                    if (AppContext.GetAction("New") != null)
                        return;
                }
                catch (Exception) when (attempt < 29)
                {
                    // Blazor shell may still be loading after logon.
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException(
                $"Authenticated app shell not detected (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        protected void NavigateEmployeesList()
        {
            const string listViewPath = E2ETestLoginValues.EmployeesListViewPath;

            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    EasyTestBlazorNavigationHelper.GoToRelativeUrl(AppContext, TestAppUrl, listViewPath);

                    if (IsEmployeesListActive(listViewPath))
                        return;
                }
                catch (Exception) when (attempt < 29)
                {
                    // Blazor host / driver may still be starting.
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException(
                $"Could not open Employees list at /{listViewPath} (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private bool IsEmployeesListActive(string listViewPath)
        {
            if (IsEmployeeDetailFormReady())
                return false;

            if (AppContext.GetAction("New") == null)
                return false;

            if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, listViewPath))
                return true;

            return EasyTestBlazorNavigationHelper.ListHasColumnHeader(
                AppContext,
                E2ETestPersonFieldCaptions.PersonalNumber);
        }

        /// <summary>
        /// After <c>New</c> on the employees list, ensure employee detail (not family member) is active.
        /// TabbedMDI often keeps the browser URL at <c>/</c> — URL alone is unreliable; fall back to form captions.
        /// </summary>
        protected void AssertEmployeeDetailViewActive()
        {
            const string detailViewPath = E2ETestLoginValues.EmployeeDetailViewPath;

            for (var attempt = 0; attempt < 30; attempt++)
            {
                if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, detailViewPath))
                    return;

                if (IsEmployeeDetailFormReady())
                    return;

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException(
                $"Expected employee detail '/{detailViewPath}' or employee detail form ready, but URL is '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}'.");
        }

        private bool IsEmployeeDetailFormReady()
        {
            try
            {
                if (AppContext.GetAction("Save") == null)
                    return false;

                // Employee detail exposes these; family member detail after wrong-tab New does not.
                AppContext.GetForm().GetPropertyValue("First Name");
                AppContext.GetForm().GetPropertyValue("Project Contract");
                return true;
            }
            catch (AdapterOperationException)
            {
                return false;
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
            AssertEmployeeDetailViewActive(); // URL or employee form (TabbedMDI may stay on /)

            FillFormWithRetry(
                new EasyTestParameter(E2ETestPersonFieldCaptions.FirstName, firstName),
                new EasyTestParameter(E2ETestPersonFieldCaptions.LastName, lastName),
                new EasyTestParameter(E2ETestPersonFieldCaptions.PersonalNumber, personalNumber),
                new EasyTestParameter(E2ETestPersonFieldCaptions.DateOfBirth, E2ETestEmployeeCreateValues.DateOfBirth),
                new EasyTestParameter(E2ETestPersonFieldCaptions.BirthPlace, E2ETestEmployeeCreateValues.BirthPlace),
                new EasyTestParameter(E2ETestPersonFieldCaptions.CountryOfBirth, E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.Gender, E2ETestEmployeeCreateValues.GenderDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.MaritalStatus, E2ETestEmployeeCreateValues.MaritalStatusDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.Nationality, E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.ForeignAddress, E2ETestEmployeeCreateValues.ForeignAddress),
                new EasyTestParameter(E2ETestPersonFieldCaptions.ForeignAddressCountry, E2ETestEmployeeCreateValues.CountryDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.ProjectContract, E2ETestEmployeeCreateValues.ProjectContractDisplay),
                new EasyTestParameter(E2ETestPersonFieldCaptions.Subcontractor, E2ETestEmployeeCreateValues.SubcontractorDisplay));

            ExecuteActionWithRetry("Save");
        }

        protected void OpenEmployeeInListByPersonalNumber(string personalNumber)
        {
            NavigateEmployeesList();

            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    AppContext.GetGrid().ProcessRow(
                        new EasyTestParameter(E2ETestPersonFieldCaptions.PersonalNumber, personalNumber));
                    AssertEmployeeDetailShowsPersonalNumber(personalNumber);
                    return;
                }
                catch (AdapterOperationException)
                {
                    try
                    {
                        AppContext.GetGrid().ProcessRow(
                            new EasyTestParameter("Full Name", E2ETestEmployeeCreateValues.FullName));
                        AssertEmployeeDetailShowsPersonalNumber(personalNumber);
                        return;
                    }
                    catch (AdapterOperationException) when (attempt < 4)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
            }

            EasyTestBlazorNavigationHelper.ClickListRowContaining(AppContext, personalNumber);
            AssertEmployeeDetailShowsPersonalNumber(personalNumber);
        }

        private void AssertEmployeeDetailShowsPersonalNumber(string personalNumber)
        {
            Assert.NotNull(AppContext.GetAction("Save"));
            string actual = AppContext.GetForm().GetPropertyValue(E2ETestPersonFieldCaptions.PersonalNumber);
            Assert.Equal(personalNumber, actual);
        }

        protected void FillFormWithRetry(params EasyTestParameter[] fields)
        {
            foreach (EasyTestParameter field in fields)
            {
                FillSingleFieldWithRetry(field);
            }
        }

        private static readonly Dictionary<string, string> PersonFieldHookTestIds =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [E2ETestPersonFieldCaptions.FirstName] = "person-first-name",
                [E2ETestPersonFieldCaptions.LastName] = "person-last-name",
                [E2ETestPersonFieldCaptions.PersonalNumber] = "person-personal-number",
                [E2ETestPersonFieldCaptions.DateOfBirth] = "person-date-of-birth",
                [E2ETestPersonFieldCaptions.BirthPlace] = "person-birth-place",
                [E2ETestPersonFieldCaptions.CountryOfBirth] = "person-country-of-birth",
                [E2ETestPersonFieldCaptions.Gender] = "person-gender",
                [E2ETestPersonFieldCaptions.MaritalStatus] = "person-marital-status",
                [E2ETestPersonFieldCaptions.Nationality] = "person-nationality",
                [E2ETestPersonFieldCaptions.ForeignAddress] = "person-foreign-address",
                [E2ETestPersonFieldCaptions.ForeignAddressCountry] = "person-foreign-address-country",
                [E2ETestPersonFieldCaptions.ProjectContract] = "person-project-contract",
                [E2ETestPersonFieldCaptions.Subcontractor] = "person-subcontractor",
                ["Date of Birth"] = "person-date-of-birth",
                ["Country of Birth"] = "person-country-of-birth",
            };

        private void FillSingleFieldWithRetry(EasyTestParameter field)
        {
            foreach (string caption in GetCaptionAliases(field.Name))
            {
                for (var attempt = 0; attempt < 10; attempt++)
                {
                    try
                    {
                        AppContext.GetForm().FillForm(new EasyTestParameter(caption, field.Value));
                        return;
                    }
                    catch (AdapterOperationException) when (attempt < 9)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
                    }
                }
            }

            if (PersonFieldHookTestIds.TryGetValue(field.Name, out string? testId))
            {
                EasyTestBlazorNavigationHelper.FillInputByTestId(AppContext, testId, field.Value);
                return;
            }

            throw new InvalidOperationException($"Could not fill form field: {field.Name}.");
        }

        private static IEnumerable<string> GetCaptionAliases(string caption)
        {
            yield return caption;

            if (string.Equals(caption, E2ETestPersonFieldCaptions.DateOfBirth, StringComparison.OrdinalIgnoreCase)
                || string.Equals(caption, "Date of Birth", StringComparison.OrdinalIgnoreCase))
            {
                yield return E2ETestPersonFieldCaptions.DateOfBirth;
                yield return "Date of Birth";
            }
            else if (string.Equals(caption, E2ETestPersonFieldCaptions.CountryOfBirth, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(caption, "Country of Birth", StringComparison.OrdinalIgnoreCase))
            {
                yield return E2ETestPersonFieldCaptions.CountryOfBirth;
                yield return "Country of Birth";
            }
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
