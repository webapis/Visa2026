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

        /// <summary>
        /// Shared <see cref="EasyTestSessionFixture"/> keeps one browser session for all facts —
        /// skip logon when a prior test already authenticated.
        /// </summary>
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

        /// <summary>
        /// Opens seeded employee when <see cref="E2ETestDataSeedUpdater"/> ran; otherwise creates the same identity via UI (E2E-020 arrange).
        /// </summary>
        protected void EnsureEmployeeParentForChildBoTest()
        {
            NavigateEmployeesList();

            if (TryOpenEmployeeRow(E2ETestDataSeed.PersonPersonalNumber, E2ETestDataSeed.PersonFullName))
                return;

            CreateEmployeeWithRequiredFields(
                E2ETestDataSeed.PersonPersonalNumber,
                E2ETestDataSeed.PersonFirstName,
                E2ETestDataSeed.PersonLastName);

            OpenEmployeeInListByPersonalNumber(
                E2ETestDataSeed.PersonPersonalNumber,
                E2ETestDataSeed.PersonFullName);
        }

        private bool TryOpenEmployeeRow(string personalNumber, string fullNameFallback)
        {
            if (!EasyTestBlazorNavigationHelper.ListHasColumnHeader(
                    AppContext,
                    E2ETestPersonFieldCaptions.PersonalNumber))
                return false;

            try
            {
                AppContext.GetGrid().ProcessRow(
                    new EasyTestParameter(E2ETestPersonFieldCaptions.PersonalNumber, personalNumber));
                if (EmployeeDetailShowsPersonalNumber(personalNumber))
                    return true;
            }
            catch (AdapterOperationException)
            {
                // Fall through to full-name row match.
            }

            try
            {
                AppContext.GetGrid().ProcessRow(
                    new EasyTestParameter("Full Name", fullNameFallback));
                return EmployeeDetailShowsPersonalNumber(personalNumber);
            }
            catch (AdapterOperationException)
            {
                return false;
            }
        }

        /// <summary>Native EasyTest layout tab — <c>*Action Passports</c> / <see cref="IEasyTestAction.Execute"/>.</summary>
        protected void ActivatePersonPassportsTab()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    var tabAction = AppContext.GetAction("Passports");
                    if (tabAction != null)
                    {
                        tabAction.Execute();
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
                        return;
                    }
                }
                catch (AdapterOperationException) when (attempt < 9)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }
            }

            throw new InvalidOperationException("Could not activate Passports tab on Person detail.");
        }

        protected void ExecutePersonPassportsNestedNew()
        {
            ActivatePersonPassportsTab();

            string[] newActionCaptions = ["Passports.New", "New"];
            for (var attempt = 0; attempt < 10; attempt++)
            {
                foreach (string caption in newActionCaptions)
                {
                    try
                    {
                        var newAction = AppContext.GetAction(caption);
                        if (newAction != null)
                        {
                            newAction.Execute();
                            WaitForPassportDetailReady();
                            return;
                        }
                    }
                    catch (AdapterOperationException) when (attempt < 9)
                    {
                        // Try next caption or retry after tab content loads.
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            throw new InvalidOperationException("Could not execute New on Person detail Passports nested list.");
        }

        /// <summary>
        /// Fills required <see cref="BusinessObjects.Passport"/> detail fields (Person link comes from nested New).
        /// </summary>
        protected void FillPassportRequiredFields(
            string passportNumber = E2ETestPassportCreateValues.PassportNumber,
            string passportTypeDisplay = E2ETestPassportCreateValues.PassportTypeDisplay,
            string issuedCountryDisplay = E2ETestPassportCreateValues.IssuedCountryDisplay)
        {
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
            try
            {
                return AppContext.GetAction("Save") != null;
            }
            catch (AdapterOperationException)
            {
                return false;
            }
        }

        private void WaitForPassportDetailReady()
        {
            DateTime deadline = DateTime.UtcNow + EasyTestCITuning.PassportDetailOpenTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (IsPassportDetailFormReady()
                    || EasyTestBlazorNavigationHelper.UrlContains(AppContext, "Passport_DetailView")
                    || EasyTestBlazorNavigationHelper.IsHookInputVisible(
                        AppContext,
                        E2ETestPassportHookTestIds.PassportNumber))
                {
                    return;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            throw new InvalidOperationException(
                $"Passport detail did not open after nested New (URL: '{EasyTestBlazorNavigationHelper.GetCurrentUrl(AppContext)}').");
        }

        private void FillPassportFormWithRetry(params EasyTestParameter[] fields)
        {
            foreach (EasyTestParameter field in fields)
            {
                FillSinglePassportFieldWithRetry(field);
            }
        }

        private static readonly Dictionary<string, string> PassportFieldHookTestIds =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [E2ETestPassportFieldCaptions.PassportNumber] = E2ETestPassportHookTestIds.PassportNumber,
                [E2ETestPassportFieldCaptions.PassportType] = E2ETestPassportHookTestIds.PassportType,
                [E2ETestPassportFieldCaptions.IssueDate] = E2ETestPassportHookTestIds.IssueDate,
                [E2ETestPassportFieldCaptions.ExpirationDate] = E2ETestPassportHookTestIds.ExpirationDate,
                [E2ETestPassportFieldCaptions.Authority] = E2ETestPassportHookTestIds.Authority,
                [E2ETestPassportFieldCaptions.IssuedCountry] = E2ETestPassportHookTestIds.IssuedCountry,
            };

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

            if (PassportFieldHookTestIds.TryGetValue(field.Name, out string? testId)
                && EasyTestBlazorNavigationHelper.TryFillInputByTestId(AppContext, testId, field.Value))
                return;

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

            if (PersonFieldHookTestIds.TryGetValue(field.Name, out string? testId)
                && EasyTestBlazorNavigationHelper.TryFillInputByTestId(AppContext, testId, field.Value))
                return;

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
