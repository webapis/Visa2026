using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
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
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "00-logon-page");
        await LoginAsync();
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "01-after-login");

        await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
        await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "02-employees-list");

        await CreateEmployeeAsync(personalNumber, firstName, lastName);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "03-employee-created");

        if (!await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
        {
            await OpenEmployeeFromListAsync(personalNumber);
        }

        Assert.Equal(firstName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(lastName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-last-name", E2ETestPersonFieldCaptions.LastName));
        Assert.Equal(personalNumber, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "04-employee-detail");

        await AddPassportAsync(passportNumber);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "07-passport-saved");

        _ = fullName;
    }

    private async Task LoginAsync()
    {
        await PlaywrightPageInteractions.GotoRelativeAsync(_page, "LoginPage");
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-login-user-name", PlaywrightE2eEnvironment.UserName, "User Name");
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-login-password", PlaywrightE2eEnvironment.Password, "Password");
        await _page.Locator(".e2e-login-submit button, button[title='Log In'], button:has-text('Log In')").First
            .ClickAsync();
        await _page.WaitForURLAsync(url => !url.Contains("LoginPage", StringComparison.OrdinalIgnoreCase),
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

    private async Task AddPassportAsync(string passportNumber)
    {
        await PlaywrightPageInteractions.ClickTabAsync(_page, "Passports");
        await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "New Passport");
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "05-passport-detail-new");

        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-passport-passport-number", passportNumber);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-passport-passport-type", E2ETestPassportCreateValues.PassportTypeDisplay);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-passport-issue-date", E2ETestPassportCreateValues.IssueDate);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-passport-expiration-date", E2ETestPassportCreateValues.ExpirationDate);
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-passport-authority", E2ETestPassportCreateValues.Authority);
        await PlaywrightPageInteractions.FillLookupAsync(_page, "e2e-passport-issued-country", E2ETestPassportCreateValues.IssuedCountryDisplay);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, "06-passport-fields-filled");

        await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
        await Task.Delay(1000);

        string actual = await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-passport-passport-number");
        Assert.Equal(passportNumber, actual);
    }
}
