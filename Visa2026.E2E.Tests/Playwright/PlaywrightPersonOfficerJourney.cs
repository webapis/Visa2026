using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Visa2026.E2E.Tests.UserManual;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>Officer person journey — Playwright implementation (Local + Staging).</summary>
internal sealed class PlaywrightPersonOfficerJourney
{
    private readonly IPage _page;

    internal PlaywrightPersonOfficerJourney(IPage page) => _page = page;

    internal async Task RunLoginCreateEmployeeAddPassportAsync(
        string personalNumber,
        string firstName,
        string lastName,
        string fullName,
        string passportNumber)
    {
        await PlaywrightPageInteractions.GotoRelativeAsync(_page, "LoginPage");
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy00LogonPage);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.LoginStep01Logon,
            PlaywrightPageInteractions.LoginSubmitButton(_page));
        await LoginAsync(skipNavigation: true);
        await PlaywrightPageInteractions.WaitForApplicationShellAsync(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.LoginStep02ReportDashboard);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.NavigationStep01Shell);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.NavigationStep02LeftMenu,
            PlaywrightPageInteractions.NavigationMenuItem(_page, "Employees"));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy01AfterLogin);

        await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
        await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
        ILocator newToolbar = PlaywrightPageInteractions.ToolbarButton(_page, "New");
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep01EmployeesList, newToolbar);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.NavigationStep03EmployeesList, newToolbar);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy02EmployeesList);

        await CreateEmployeeAsync(personalNumber, firstName, lastName);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep02SavedDetail);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy03EmployeeCreated);

        if (!await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
        {
            await OpenEmployeeFromListAsync(personalNumber);
        }

        Assert.Equal(firstName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(lastName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-last-name", E2ETestPersonFieldCaptions.LastName));
        Assert.Equal(personalNumber, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));
        ILocator passportsTab = PlaywrightPageInteractions.TabItem(_page, "Passports");
        await PlaywrightE2eStepRunner.RunAsync(_page, "visa-family-manual", () => AddVisaFamilyManualLinesAsync(personalNumber));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonAddPassportStep01EmployeeDetail, passportsTab);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.PersonRegisterStep03OpenFromList,
            PlaywrightPageInteractions.ToolbarButton(_page, "Save"));
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.NavigationStep04DetailForm,
            PlaywrightPageInteractions.ToolbarButton(_page, "Save"));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy04EmployeeDetail);

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-passport", () => AddPassportAsync(passportNumber));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy07PassportSaved);

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-visa", () => AddVisaAsync());

        _ = fullName;
    }

    private async Task LoginAsync(bool skipNavigation = false)
    {
        if (!skipNavigation)
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, "LoginPage");
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-login-user-name", PlaywrightE2eEnvironment.UserName, "User Name");
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-login-password", PlaywrightE2eEnvironment.Password, "Password");
        await PlaywrightPageInteractions.LoginSubmitButton(_page).ClickAsync();
        await _page.WaitForURLAsync(
            url => !url.Contains("LoginPage", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 120_000 });
    }

    private async Task CreateEmployeeAsync(string personalNumber, string firstName, string lastName)
    {
        await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
        await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
        await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "New");
        await PlaywrightPageInteractions.WaitForEmployeeDetailAsync(_page);

        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-first-name", firstName, E2ETestPersonFieldCaptions.FirstName);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-last-name", lastName, E2ETestPersonFieldCaptions.LastName);
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestPersonFieldCaptions.DateOfBirth);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-date-of-birth", E2ETestEmployeeCreateValues.DateOfBirth, E2ETestPersonFieldCaptions.DateOfBirth);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-birth-place", E2ETestEmployeeCreateValues.BirthPlace, E2ETestPersonFieldCaptions.BirthPlace);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-country-of-birth", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.CountryOfBirth);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-gender", E2ETestEmployeeCreateValues.GenderDisplay, E2ETestPersonFieldCaptions.Gender);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-marital-status", E2ETestEmployeeCreateValues.MaritalStatusDisplay, E2ETestPersonFieldCaptions.MaritalStatus);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-nationality", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.Nationality);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-personal-number", personalNumber, E2ETestPersonFieldCaptions.PersonalNumber);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-person-foreign-address", E2ETestEmployeeCreateValues.ForeignAddress, E2ETestPersonFieldCaptions.ForeignAddress);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-foreign-address-country", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.ForeignAddressCountry);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-project-contract", E2ETestEmployeeCreateValues.ProjectContractDisplay, E2ETestPersonFieldCaptions.ProjectContract);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-subcontractor", E2ETestEmployeeCreateValues.SubcontractorDisplay, E2ETestPersonFieldCaptions.Subcontractor);

        await SaveEmployeeDetailAndConfirmAsync(personalNumber);
    }

    private async Task SaveEmployeeDetailAndConfirmAsync(string personalNumber)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await EnsureEmployeeRequiredLookupsBoundAsync();
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
            await Task.Delay(2000);

            string content = await _page.ContentAsync();
            if (content.Contains("must not be empty", StringComparison.OrdinalIgnoreCase)
                || content.Contains("Data Validation Error", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
                return;

            if (await TryFindEmployeeInListAsync(personalNumber))
                return;
        }

        await PlaywrightPageInteractions.DumpPageDiagnosticsAsync(_page, "employee-save-failed");
        throw new InvalidOperationException(
            $"Employee with Personal Number '{personalNumber}' was not confirmed after Save.");
    }

    private async Task EnsureEmployeeRequiredLookupsBoundAsync()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            string project = await TryReadLookupDisplayAsync("e2e-person-project-contract", E2ETestPersonFieldCaptions.ProjectContract);
            string subcontractor = await TryReadLookupDisplayAsync("e2e-person-subcontractor", E2ETestPersonFieldCaptions.Subcontractor);
            if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(subcontractor))
                return;

            if (string.IsNullOrWhiteSpace(project))
            {
                await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-project-contract",
                    E2ETestEmployeeCreateValues.ProjectContractDisplay, E2ETestPersonFieldCaptions.ProjectContract);
            }

            if (string.IsNullOrWhiteSpace(subcontractor))
            {
                await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-person-subcontractor",
                    E2ETestEmployeeCreateValues.SubcontractorDisplay, E2ETestPersonFieldCaptions.Subcontractor);
            }
        }
    }

    private async Task<string> TryReadLookupDisplayAsync(string cssClass, string caption)
    {
        try
        {
            return await PlaywrightPageInteractions.ReadFieldAsync(_page, cssClass, caption);
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }

    private async Task<bool> EmployeeDetailShowsPersonalNumberAsync(string personalNumber)
    {
        try
        {
            string actual = await PlaywrightPageInteractions.ReadFieldAsync(
                _page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber);
            return string.Equals(actual, personalNumber, StringComparison.Ordinal);
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private async Task OpenEmployeeFromListAsync(string personalNumber)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
            await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
            await Task.Delay(1000);

            if (await TryFindEmployeeInListAsync(personalNumber))
                return;
        }

        throw new InvalidOperationException($"Employee list row containing '{personalNumber}' was not found.");
    }

    private async Task<bool> TryFindEmployeeInListAsync(string personalNumber)
    {
        try
        {
            await PlaywrightPageInteractions.ClickListRowContainingAsync(_page, personalNumber);
            return await EmployeeDetailShowsPersonalNumberAsync(personalNumber);
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private async Task AddVisaFamilyManualLinesAsync(string personalNumber)
    {
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestVisaFamilyManualUi.FieldCaption);
        await PlaywrightPageInteractions.FillLookupAsync(
            _page,
            "e2e-person-marital-status",
            E2ETestVisaFamilyManualValues.MarriedMaritalStatusDisplay,
            E2ETestPersonFieldCaptions.MaritalStatus);

        ILocator familyField = PlaywrightPageInteractions.VisaFamilyManualFieldContainer(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.EmployeeVisaFamilyManualStep01Field,
            familyField);

        await PlaywrightPageInteractions.OpenVisaFamilyManualPopupAsync(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.EmployeeVisaFamilyManualStep02PopupOpen,
            _page.Locator(".visa-family-lines-popup").First);

        ILocator mainPopup = _page.Locator(".visa-family-lines-popup").First;
        await mainPopup.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = E2ETestVisaFamilyManualUi.AddMember }).ClickAsync();
        await _page.Locator(".visa-family-lines-edit").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await PlaywrightPageInteractions.FillVisaFamilyManualMemberFormAsync(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.EmployeeVisaFamilyManualStep03AddMemberForm,
            _page.Locator(".visa-family-lines-edit").First);

        await PlaywrightPageInteractions.ClickVisaFamilyManualEditSaveAsync(_page);
        await _page.Locator(".visa-family-lines-popup__name")
            .Filter(new LocatorFilterOptions { HasText = E2ETestVisaFamilyManualValues.MemberFullName })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.EmployeeVisaFamilyManualStep04PopupWithMember,
            _page.Locator(".visa-family-lines-popup").First);

        await PlaywrightPageInteractions.ClickVisaFamilyManualMainOkAsync(_page);
        await Task.Delay(500);

        await SaveEmployeeDetailAndConfirmAsync(personalNumber);

        ILocator familyFieldAfterSave = PlaywrightPageInteractions.VisaFamilyManualFieldContainer(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.EmployeeVisaFamilyManualStep05SavedSummary,
            familyFieldAfterSave);
    }

    private async Task AddPassportAsync(string passportNumber)
    {
        await PlaywrightE2eStepRunner.RunAsync(_page, "add-passport-open-form", async () =>
        {
            await PlaywrightPageInteractions.ClickPassportsNestedNewAsync(_page);
            ILocator passportNumberField = await PlaywrightPageInteractions.WaitForPassportNumberFieldAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonAddPassportStep02PassportFormNew, passportNumberField);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy05PassportDetailNew);
        });

        ILocator passportNumberField = await PlaywrightPageInteractions.WaitForPassportNumberFieldAsync(_page);

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-passport-fill-fields", async () =>
        {
            await PlaywrightPageInteractions.FillTextFieldAsync(
                _page,
                "e2e-passport-passport-number",
                passportNumber,
                E2ETestPassportFieldCaptions.PassportNumber);
            await PlaywrightPageInteractions.EnsureLookupBoundAsync(
                _page,
                "e2e-passport-passport-type",
                E2ETestPassportCreateValues.PassportTypeDisplay,
                E2ETestPassportFieldCaptions.PassportType);
            await PlaywrightPageInteractions.FillDateFieldAsync(
                _page,
                "e2e-passport-issue-date",
                E2ETestPassportCreateValues.IssueDate,
                E2ETestPassportFieldCaptions.IssueDate);
            await PlaywrightPageInteractions.FillDateFieldAsync(
                _page,
                "e2e-passport-expiration-date",
                E2ETestPassportCreateValues.ExpirationDate,
                E2ETestPassportFieldCaptions.ExpirationDate);
            await PlaywrightPageInteractions.FillTextFieldAsync(
                _page,
                "e2e-passport-authority",
                E2ETestPassportCreateValues.Authority,
                E2ETestPassportFieldCaptions.Authority);
            await PlaywrightPageInteractions.EnsureLookupBoundAsync(
                _page,
                "e2e-passport-issued-country",
                E2ETestPassportCreateValues.IssuedCountryDisplay,
                E2ETestPassportFieldCaptions.IssuedCountry);

            string passportType = await PlaywrightPageInteractions.TryReadLookupDisplayAsync(
                _page, "e2e-passport-passport-type", E2ETestPassportFieldCaptions.PassportType);
            Assert.True(
                PlaywrightPageInteractions.LookupDisplayMatches(passportType, E2ETestPassportCreateValues.PassportTypeDisplay),
                $"Passport Type must be selected before save (actual: '{passportType}').");

            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonAddPassportStep03PassportFieldsFilled, passportNumberField);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy06PassportFieldsFilled);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-passport-save", async () =>
        {
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
            await Task.Delay(2000);

            string content = await _page.ContentAsync();
            Assert.False(
                content.Contains("Passport type must not be empty", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("must not be empty", StringComparison.OrdinalIgnoreCase)
                        && content.Contains("Passport Type", StringComparison.OrdinalIgnoreCase),
                "Passport save failed validation — Passport Type may be unbound.");

            string actual = await PlaywrightPageInteractions.ReadFieldAsync(
                _page,
                "e2e-passport-passport-number",
                E2ETestPassportFieldCaptions.PassportNumber);
            Assert.Equal(passportNumber, actual);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonAddPassportStep04PassportSaved,
                PlaywrightPageInteractions.ToolbarButton(_page, "Save"));
        });
    }

    private async Task AddVisaAsync()
    {
        await PlaywrightE2eStepRunner.RunAsync(_page, "add-visa-open-passport", async () =>
        {
            await PlaywrightPageInteractions.ActivateMdiPassportTabAsync(_page);
            ILocator passportsTab = PlaywrightPageInteractions.TabItem(_page, "Visas");
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonAddVisaStep01PassportDetail,
                passportsTab);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-visa-open-form", async () =>
        {
            await PlaywrightPageInteractions.ClickPassportVisasNestedNewAsync(_page);
            ILocator visaNumberField = await PlaywrightPageInteractions.WaitForVisaNumberFieldAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonAddVisaStep02VisaFormNew,
                visaNumberField);
        });

        ILocator visaNumberField = await PlaywrightPageInteractions.WaitForVisaNumberFieldAsync(_page);

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-visa-fill-fields", async () =>
        {
            await PlaywrightPageInteractions.ActivateMdiVisaTabAsync(_page);
            await PlaywrightPageInteractions.FillTextFieldAsync(
                _page,
                "e2e-visa-process-number",
                E2ETestVisaCreateValues.ProcessNumber,
                E2ETestVisaFieldCaptions.ProcessNumber);
            await PlaywrightPageInteractions.FillTextFieldAsync(
                _page,
                "e2e-visa-visa-number",
                E2ETestVisaCreateValues.VisaNumber,
                E2ETestVisaFieldCaptions.VisaNumber);
            await PlaywrightPageInteractions.FillDateFieldAsync(
                _page,
                "e2e-visa-issue-date",
                E2ETestVisaCreateValues.IssueDate,
                E2ETestVisaFieldCaptions.IssueDate);
            await PlaywrightPageInteractions.FillDateFieldAsync(
                _page,
                "e2e-visa-start-date",
                E2ETestVisaCreateValues.StartDate,
                E2ETestVisaFieldCaptions.StartDate);
            await PlaywrightPageInteractions.FillDateFieldAsync(
                _page,
                "e2e-visa-expiration-date",
                E2ETestVisaCreateValues.ExpirationDate,
                E2ETestVisaFieldCaptions.ExpirationDate);

            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonAddVisaStep03VisaFieldsFilled,
                visaNumberField);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "add-visa-save", async () =>
        {
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
            await Task.Delay(2000);

            string actual = await PlaywrightPageInteractions.ReadFieldAsync(
                _page,
                "e2e-visa-visa-number",
                E2ETestVisaFieldCaptions.VisaNumber);
            Assert.Equal(E2ETestVisaCreateValues.VisaNumber, actual);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonAddVisaStep04VisaSaved,
                PlaywrightPageInteractions.ToolbarButton(_page, "Save"));
        });
    }
}
