using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests
{
    [SupportedOSPlatform("windows")]
    [Collection(EasyTestCollection.Name)]
    public abstract class E2ETestBase
    {
        protected const string BlazorAppName = EasyTestSessionFixture.BlazorAppName;
        protected const string AppDBName = EasyTestSessionFixture.AppDBName;

        protected IApplicationContext AppContext { get; }

        protected E2ETestBase(EasyTestSessionFixture session)
        {
            AppContext = session.AppContext;
        }

        protected void Login(string userName = "Admin", string password = "")
        {
            if (IsAuthenticatedShellReady())
                return;

            AppContext.GetForm().FillForm(
                new EasyTestParameter("User Name", userName),
                new EasyTestParameter("Password", password));
            AppContext.GetAction("Log In").Execute();
        }

        /// <summary>Skip logon when the session is already on the authenticated shell.</summary>
        private bool IsAuthenticatedShellReady()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, "LoginPage"))
                        return false;

                    if (AppContext.GetAction("Log In") != null)
                        return false;

                    AppContext.Navigate("Application");
                    if (AppContext.GetAction("New") != null)
                        return true;
                }
                catch (Exception) when (attempt < 9)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }
            }

            return false;
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
                    EasyTestBlazorNavigationHelper.GoToRelativeUrl(AppContext, EasyTestHostEnvironment.BaseUrl, listViewPath);

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
            // Fast Selenium signals first. GetForm()/GetAction() each block up to the
            // Config DefaultTimeout on a miss, so a GetForm()-based "is this a detail
            // view?" probe is very slow while we are actually on the list.
            bool urlIsList = EasyTestBlazorNavigationHelper.UrlContains(AppContext, listViewPath);
            bool hasListGrid = EasyTestBlazorNavigationHelper.ListHasColumnHeader(
                AppContext, E2ETestPersonFieldCaptions.PersonalNumber);

            // The Personal Number column header is unique to the Employees list grid
            // (the employee detail nests Passports/Educations, not a Personal Number
            // grid), so its presence already distinguishes list from detail.
            if (!urlIsList && !hasListGrid)
                return false;

            return AppContext.GetAction("New") != null;
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
        /// Creates an employee with all required Person detail fields (officer journey E2E-001).
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

        protected void OpenEmployeeInListByPersonalNumber(
            string personalNumber,
            string? fullNameFallback = null)
        {
            fullNameFallback ??= E2ETestEmployeeCreateValues.FullName;
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
                            new EasyTestParameter("Full Name", fullNameFallback));
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

        /// <summary>Native EasyTest layout tab — <c>*Action Passports</c> / <see cref="IEasyTestAction.Execute"/>.</summary>
        protected void ActivatePersonPassportsTab()
        {
            int maxAttempts = EasyTestCITuning.NestedListActionMaxAttempts;
            TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var tabAction = AppContext.GetAction("Passports");
                    if (tabAction != null)
                    {
                        tabAction.Execute();
                        Thread.Sleep(EasyTestCITuning.LayoutTabSettleDelay);
                        if (IsPassportsNestedListReady())
                            return;
                    }
                }
                catch (AdapterOperationException)
                {
                    // Retry after Blazor tab content loads.
                }

                if (attempt < maxAttempts - 1)
                    Thread.Sleep(delay);
            }

            EasyTestBlazorNavigationHelper.TryDumpDiagnostics(
                AppContext, EasyTestHostProcessLauncher.LogDirectory, "passports-tab");

            throw new InvalidOperationException(
                $"Could not activate Passports tab on Person detail (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        protected void ExecutePersonPassportsNestedNew()
        {
            int maxAttempts = EasyTestCITuning.NestedNewClickMaxAttempts;
            TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;
            var nestedNewClicked = false;

            // Re-verify that the nested passport detail actually opened after each
            // New click. On slow/headed CI the first Execute can return without
            // opening the detail; committing to a single click + one long wait then
            // always times out (the recurring "Passport detail did not open" CI fail).
            // Re-activate the tab and click again until the detail is detected.
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (IsPassportDetailFormReady())
                    return;

                ActivatePersonPassportsTab();

                if (TryClickPassportsNestedNew())
                {
                    nestedNewClicked = true;
                    if (TryWaitForPassportDetailReady(EasyTestCITuning.NestedNewProbeTimeout))
                        return;
                }

                if (attempt < maxAttempts - 1)
                    Thread.Sleep(delay);
            }

            EasyTestBlazorNavigationHelper.TryDumpDiagnostics(
                AppContext, EasyTestHostProcessLauncher.LogDirectory, "passport-nested-new");

            if (!nestedNewClicked)
            {
                throw new InvalidOperationException(
                    "Could not execute New on Person detail Passports nested list (native EasyTest actions).");
            }

            throw new InvalidOperationException(
                $"Passport detail did not open after nested New (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        /// <summary>
        /// Opens the nested Passports list <c>New</c>. Tries native EasyTest actions first
        /// (<c>Passports.New</c> then bare <c>New</c>, confirming the detail opened), then
        /// falls back to a direct DOM click of the real <c>New Passport</c> toolbar button
        /// for small/headed CI viewports where the adaptive toolbar virtualizes it.
        /// Returns whether a click was issued.
        /// </summary>
        private bool TryClickPassportsNestedNew()
        {
            // Native EasyTest first (matches local runs where the toolbar is not
            // adaptively collapsed). Confirm the detail actually opened — a returning
            // Execute does not guarantee the nested New fired (see DOM fallback below).
            foreach (string caption in new[] { "Passports.New", "New" })
            {
                try
                {
                    var newAction = AppContext.GetAction(caption);
                    if (newAction == null)
                        continue;

                    newAction.Execute();
                    if (TryWaitForPassportDetailReady(TimeSpan.FromSeconds(4)))
                        return true;
                }
                catch (AdapterOperationException)
                {
                    // Try next caption, then the DOM fallback below.
                }
            }

            // DOM fallback: on small/headed CI viewports XAF renders the nested
            // ListPropertyEditor New action with an empty data-xaf-action plus an
            // adaptive virtual clone, and there are sibling "New" buttons (Educations
            // vs Passports), so EasyTest's Execute is ambiguous and can no-op. Click
            // the real, displayed "New Passport" toolbar button directly.
            return EasyTestBlazorNavigationHelper.TryClickToolbarActionByTitle(
                AppContext, "New Passport", TimeSpan.FromSeconds(10));
        }

        private bool IsPassportsNestedListReady()
        {
            try
            {
                return AppContext.GetAction("Passports.New") != null
                       || AppContext.GetAction("New") != null;
            }
            catch (AdapterOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Fills required <see cref="BusinessObjects.Passport"/> detail fields (Person link comes from nested New).
        /// </summary>
        protected void FillPassportRequiredFields(
            string passportNumber = E2ETestPassportCreateValues.PassportNumber,
            string passportTypeDisplay = E2ETestPassportCreateValues.PassportTypeDisplay,
            string issuedCountryDisplay = E2ETestPassportCreateValues.IssuedCountryDisplay)
        {
            WaitForPassportDetailReady();

            FillPassportFormWithRetry(
                new EasyTestParameter(E2ETestPassportFieldCaptions.PassportNumber, passportNumber),
                new EasyTestParameter(E2ETestPassportFieldCaptions.PassportType, passportTypeDisplay),
                new EasyTestParameter(E2ETestPassportFieldCaptions.IssueDate, E2ETestPassportCreateValues.IssueDate),
                new EasyTestParameter(E2ETestPassportFieldCaptions.ExpirationDate, E2ETestPassportCreateValues.ExpirationDate),
                new EasyTestParameter(E2ETestPassportFieldCaptions.Authority, E2ETestPassportCreateValues.Authority),
                new EasyTestParameter(E2ETestPassportFieldCaptions.IssuedCountry, issuedCountryDisplay));
        }

        protected void SavePassportDetail()
        {
            ExecuteActionWithRetry("Save");
        }

        protected void AssertPassportDetailShowsNumber(string passportNumber)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, "Passport_DetailView")
                        || IsPassportDetailFormReady())
                    {
                        string actual = AppContext.GetForm().GetPropertyValue(E2ETestPassportFieldCaptions.PassportNumber);
                        Assert.Equal(passportNumber, actual);
                        return;
                    }
                }
                catch (AdapterOperationException) when (attempt < 19)
                {
                    // Passport detail may still be opening.
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            throw new InvalidOperationException(
                $"Passport detail with number '{passportNumber}' was not detected (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private bool IsPassportDetailFormReady()
        {
            if (EasyTestBlazorNavigationHelper.UrlContains(AppContext, "Passport_DetailView"))
                return true;

            try
            {
                if (AppContext.GetAction("Save") == null)
                    return false;

                AppContext.GetForm().GetPropertyValue(E2ETestPassportFieldCaptions.PassportNumber);
                return true;
            }
            catch (AdapterOperationException)
            {
                return false;
            }
        }

        private void WaitForPassportDetailReady()
        {
            if (TryWaitForPassportDetailReady(EasyTestCITuning.PassportDetailOpenTimeout))
                return;

            throw new InvalidOperationException(
                $"Passport detail did not open after nested New (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private bool TryWaitForPassportDetailReady(TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (IsPassportDetailFormReady())
                    return true;

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            return false;
        }

        private void FillPassportFormWithRetry(params EasyTestParameter[] fields)
        {
            foreach (EasyTestParameter field in fields)
            {
                FillSinglePassportFieldWithRetry(field);
            }
        }

        private void FillSinglePassportFieldWithRetry(EasyTestParameter field)
        {
            int maxAttempts = EasyTestCITuning.FormFieldMaxAttempts;
            TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    AppContext.GetForm().FillForm(new EasyTestParameter(field.Name, field.Value));
                    return;
                }
                catch (AdapterOperationException)
                {
                    if (attempt < maxAttempts - 1)
                        Thread.Sleep(delay);
                }
            }

            throw new InvalidOperationException(
                $"Could not fill passport form field: {field.Name} " +
                $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private void AssertEmployeeDetailShowsPersonalNumber(string personalNumber)
        {
            Assert.True(
                EmployeeDetailShowsPersonalNumber(personalNumber),
                $"Employee detail with Personal Number '{personalNumber}' was not detected " +
                $"(URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private bool EmployeeDetailShowsPersonalNumber(string personalNumber)
        {
            try
            {
                if (AppContext.GetAction("Save") == null)
                    return false;

                string actual = AppContext.GetForm().GetPropertyValue(E2ETestPersonFieldCaptions.PersonalNumber);
                return string.Equals(personalNumber, actual, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected void FillFormWithRetry(params EasyTestParameter[] fields)
        {
            foreach (EasyTestParameter field in fields)
            {
                FillSingleFieldWithRetry(field);
            }
        }

        private void FillSingleFieldWithRetry(EasyTestParameter field)
        {
            int maxAttempts = EasyTestCITuning.FormFieldMaxAttempts;
            TimeSpan delay = EasyTestCITuning.FormFieldRetryDelay;

            foreach (string caption in GetCaptionAliases(field.Name))
            {
                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        AppContext.GetForm().FillForm(new EasyTestParameter(caption, field.Value));
                        return;
                    }
                    catch (AdapterOperationException)
                    {
                        if (attempt < maxAttempts - 1)
                            Thread.Sleep(delay);
                    }
                }
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

    }
}
