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
    private bool _loginPasswordWasDummyForCapture;

    internal PlaywrightPersonOfficerJourney(IPage page) => _page = page;

    internal async Task RunLoginCreateEmployeeAddPassportAsync(
        string personalNumber,
        string firstName,
        string lastName,
        string fullName,
        string passportNumber)
    {
        await RunSignInToReportDashboardAsync();
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.NavigationStep01Shell);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.NavigationStep02LeftMenu,
            PlaywrightPageInteractions.NavigationMenuItem(_page, "Employees"));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy01AfterLogin);

        await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
        await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
        ILocator newToolbar = PlaywrightPageInteractions.VisibleToolbarButton(_page, "New");
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep01EmployeesList);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep02New, newToolbar);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.NavigationStep03EmployeesList, newToolbar);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy02EmployeesList);

        await CreateEmployeeAsync(personalNumber, firstName, lastName, skipListNavigation: true);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep02SavedDetail);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.Legacy03EmployeeCreated);

        await OpenEmployeeFromListAsync(personalNumber, fullName);
        await PlaywrightPageInteractions.WaitForEmployeeDetailAsync(_page);
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonRegisterStep03OpenFromList);

        if (!await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
        {
            await OpenEmployeeFromListAsync(personalNumber, fullName);
        }

        Assert.Equal(firstName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(lastName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-last-name", E2ETestPersonFieldCaptions.LastName));
        Assert.Equal(personalNumber, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));

        await PlaywrightE2eStepRunner.RunAsync(
            _page,
            "register-family-member",
            () => RegisterFamilyMemberAsync(personalNumber, fullName));

        await OpenEmployeeFromListAsync(personalNumber, fullName);
        await PlaywrightPageInteractions.WaitForEmployeeDetailAsync(_page);

        ILocator passportsTab = PlaywrightPageInteractions.TabItem(_page, "Passports");
        await PlaywrightE2eStepRunner.RunAsync(_page, "visa-family-manual", () => AddVisaFamilyManualLinesAsync(personalNumber));
        await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.PersonAddPassportStep01EmployeeDetail, passportsTab);
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

    private async Task RegisterFamilyMemberAsync(string sponsorPersonalNumber, string sponsorDisplayName)
    {
        await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
        await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
        await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
        await Task.Delay(1500);

        ILocator newToolbar = PlaywrightPageInteractions.ToolbarButton(_page, "New");
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.PersonRegisterFamilyMemberStep01FamilyMembersList,
            newToolbar);

        if (await TryFindFamilyMemberInListAsync(E2ETestFamilyMemberCreateValues.PersonalNumber, sponsorDisplayName))
        {
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterFamilyMemberStep02SavedDetail,
                PlaywrightPageInteractions.ToolbarButton(_page, "Save"));

            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
            await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
            await OpenFamilyMemberFromListAsync(E2ETestFamilyMemberCreateValues.PersonalNumber, sponsorDisplayName);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterFamilyMemberStep03OpenFromList,
                PlaywrightPageInteractions.ToolbarButton(_page, "Save"));
            return;
        }

        await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "New");
        await PlaywrightPageInteractions.WaitForFamilyMemberDetailAsync(_page);
        await FillFamilyMemberFormAsync(sponsorDisplayName);

        await SaveFamilyMemberDetailAndConfirmAsync(
            E2ETestFamilyMemberCreateValues.PersonalNumber,
            sponsorDisplayName);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.PersonRegisterFamilyMemberStep02SavedDetail,
            PlaywrightPageInteractions.ToolbarButton(_page, "Save"));

        await OpenFamilyMemberFromListAsync(E2ETestFamilyMemberCreateValues.PersonalNumber, sponsorDisplayName);
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.PersonRegisterFamilyMemberStep03OpenFromList,
            PlaywrightPageInteractions.ToolbarButton(_page, "Save"));

        Assert.Equal(
            E2ETestFamilyMemberCreateValues.FirstName,
            await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(
            E2ETestFamilyMemberCreateValues.PersonalNumber,
            await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));

        string sponsor = await PlaywrightPageInteractions.TryReadLookupDisplayAsync(
            _page,
            "e2e-person-sponsoring-employee",
            E2ETestFamilyMemberFieldCaptions.SponsoringEmployee);
        Assert.True(
            sponsor.Contains(sponsorPersonalNumber, StringComparison.OrdinalIgnoreCase)
                || sponsor.Contains(sponsorDisplayName, StringComparison.OrdinalIgnoreCase),
            $"Sponsoring Employee must reference sponsor (actual: '{sponsor}').");

        _ = sponsorPersonalNumber;
    }

    private async Task FillFamilyMemberFormAsync(string sponsorDisplayName)
    {
        await PlaywrightPageInteractions.FillTextFieldAsync(
            _page,
            "e2e-person-first-name",
            E2ETestFamilyMemberCreateValues.FirstName,
            E2ETestPersonFieldCaptions.FirstName);
        await PlaywrightPageInteractions.FillTextFieldAsync(
            _page,
            "e2e-person-last-name",
            E2ETestFamilyMemberCreateValues.LastName,
            E2ETestPersonFieldCaptions.LastName);
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestPersonFieldCaptions.DateOfBirth);
        await PlaywrightPageInteractions.FillTextFieldAsync(
            _page,
            "e2e-person-date-of-birth",
            E2ETestFamilyMemberCreateValues.DateOfBirth,
            E2ETestPersonFieldCaptions.DateOfBirth);
        await PlaywrightPageInteractions.FillTextFieldAsync(
            _page,
            "e2e-person-birth-place",
            E2ETestFamilyMemberCreateValues.BirthPlace,
            E2ETestPersonFieldCaptions.BirthPlace);
        await PlaywrightPageInteractions.FillLookupAsync(
            _page,
            "e2e-person-country-of-birth",
            E2ETestFamilyMemberCreateValues.CountryDisplay,
            E2ETestPersonFieldCaptions.CountryOfBirth);
        await PlaywrightPageInteractions.FillLookupAsync(
            _page,
            "e2e-person-gender",
            E2ETestFamilyMemberCreateValues.GenderDisplay,
            E2ETestPersonFieldCaptions.Gender);
        await PlaywrightPageInteractions.EnsureLookupBoundAsync(
            _page,
            "e2e-person-nationality",
            E2ETestFamilyMemberCreateValues.CountryDisplay,
            E2ETestPersonFieldCaptions.Nationality);
        await PlaywrightPageInteractions.FillTextFieldAsync(
            _page,
            "e2e-person-personal-number",
            E2ETestFamilyMemberCreateValues.PersonalNumber,
            E2ETestPersonFieldCaptions.PersonalNumber);
        await PlaywrightPageInteractions.FillLookupAsync(
            _page,
            "e2e-person-project-contract",
            E2ETestEmployeeCreateValues.ProjectContractDisplay,
            E2ETestPersonFieldCaptions.ProjectContract);
        await PlaywrightPageInteractions.FillLookupAsync(
            _page,
            "e2e-person-subcontractor",
            E2ETestEmployeeCreateValues.SubcontractorDisplay,
            E2ETestPersonFieldCaptions.Subcontractor);
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestFamilyMemberFieldCaptions.SponsoringEmployee);
        await PlaywrightPageInteractions.EnsureLookupBoundAsync(
            _page,
            "e2e-person-sponsoring-employee",
            sponsorDisplayName,
            E2ETestFamilyMemberFieldCaptions.SponsoringEmployee);
        await Task.Delay(500);
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestFamilyMemberFieldCaptions.Relationship);
        await PlaywrightPageInteractions.EnsureLookupBoundAsync(
            _page,
            "e2e-person-relationship",
            E2ETestFamilyMemberCreateValues.RelationshipDisplay,
            E2ETestFamilyMemberFieldCaptions.Relationship);
    }

    private async Task SaveFamilyMemberDetailAndConfirmAsync(string personalNumber, string sponsorDisplayName)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
            {
                await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
                await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
                await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
                await Task.Delay(1500);
                if (await TryFindFamilyMemberInListAsync(personalNumber, sponsorDisplayName))
                {
                    return;
                }

                await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "New");
                await PlaywrightPageInteractions.WaitForFamilyMemberDetailAsync(_page);
                await FillFamilyMemberFormAsync(sponsorDisplayName);
            }

            await PlaywrightPageInteractions.ActivateMdiDocumentTabAsync(
                _page,
                E2ETestFamilyMemberCreateValues.FullName);
            await EnsureFamilyMemberRequiredLookupsBoundAsync(sponsorDisplayName);
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
            await Task.Delay(4000);

            if (await PlaywrightPageInteractions.PageShowsDuplicatePersonalNumberAsync(_page))
            {
                await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
                await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
                await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
                await Task.Delay(2000);
                if (await TryFindFamilyMemberInListAsync(personalNumber, sponsorDisplayName))
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"Personal Number '{personalNumber}' is reported as duplicate but the Family Members list row was not found.");
            }

            if (await PlaywrightPageInteractions.PageShowsValidationErrorAsync(_page))
            {
                continue;
            }

            if (await FamilyMemberDetailShowsPersonalNumberAsync(personalNumber))
            {
                return;
            }

            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
            await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
            await Task.Delay(2000);

            if (await TryFindFamilyMemberInListAsync(personalNumber, sponsorDisplayName))
            {
                return;
            }
        }

        await PlaywrightPageInteractions.DumpPageDiagnosticsAsync(_page, "family-member-save-failed");
        throw new InvalidOperationException(
            $"Family member with Personal Number '{personalNumber}' was not confirmed after Save.");
    }

    private async Task EnsureFamilyMemberRequiredLookupsBoundAsync(string sponsorDisplayName)
    {
        string nationality = await PlaywrightPageInteractions.TryReadLookupDisplayAsync(
            _page,
            "e2e-person-nationality",
            E2ETestPersonFieldCaptions.Nationality);
        if (string.IsNullOrWhiteSpace(nationality))
        {
            await PlaywrightPageInteractions.EnsureLookupBoundAsync(
                _page,
                "e2e-person-nationality",
                E2ETestFamilyMemberCreateValues.CountryDisplay,
                E2ETestPersonFieldCaptions.Nationality);
        }

        string relationship = await PlaywrightPageInteractions.TryReadLookupDisplayAsync(
            _page,
            "e2e-person-relationship",
            E2ETestFamilyMemberFieldCaptions.Relationship);
        if (string.IsNullOrWhiteSpace(relationship))
        {
            await PlaywrightPageInteractions.FillLookupAsync(
                _page,
                "e2e-person-relationship",
                E2ETestFamilyMemberCreateValues.RelationshipDisplay,
                E2ETestFamilyMemberFieldCaptions.Relationship);
        }

        string sponsor = await PlaywrightPageInteractions.TryReadLookupDisplayAsync(
            _page,
            "e2e-person-sponsoring-employee",
            E2ETestFamilyMemberFieldCaptions.SponsoringEmployee);
        if (string.IsNullOrWhiteSpace(sponsor))
        {
            await PlaywrightPageInteractions.EnsureLookupBoundAsync(
                _page,
                "e2e-person-sponsoring-employee",
                sponsorDisplayName,
                E2ETestFamilyMemberFieldCaptions.SponsoringEmployee);
        }
    }

    private async Task<bool> FamilyMemberDetailShowsPersonalNumberAsync(string personalNumber)
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

    private async Task OpenFamilyMemberFromListAsync(string personalNumber, string sponsorDisplayName)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.FamilyMembersListViewPath);
            await PlaywrightPageInteractions.WaitForFamilyMembersListAsync(_page);
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
            await Task.Delay(2000);

            if (await TryFindFamilyMemberInListAsync(personalNumber, sponsorDisplayName))
                return;
        }

        await PlaywrightPageInteractions.DumpPageDiagnosticsAsync(_page, "family-member-list-missing");
        throw new InvalidOperationException($"Family member list row containing '{personalNumber}' was not found.");
    }

    private async Task<bool> TryFindFamilyMemberInListAsync(string personalNumber, string? sponsorDisplayName = null)
    {
        await PlaywrightPageInteractions.ClearListSearchFilterAsync(_page);

        string[] searchTokens =
        [
            personalNumber,
            E2ETestFamilyMemberCreateValues.FullName,
            E2ETestFamilyMemberCreateValues.FirstName,
            E2ETestFamilyMemberCreateValues.LastName,
            sponsorDisplayName ?? string.Empty,
        ];

        foreach (string token in searchTokens)
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;

            try
            {
                await PlaywrightPageInteractions.ClearListSearchFilterAsync(_page);
                await PlaywrightPageInteractions.ApplyListSearchFilterAsync(_page, token);
                await PlaywrightPageInteractions.ClickListRowContainingAsync(_page, token);
                if (await FamilyMemberDetailShowsPersonalNumberAsync(personalNumber))
                    return true;
            }
            catch (TimeoutException)
            {
                // Try the next token.
            }
        }

        return false;
    }

    /// <summary>
    /// Locked Sign in guide: LoginPage form → Log In → Report Dashboard with Employees.
    /// </summary>
    internal async Task RunSignInToReportDashboardAsync()
    {
        await PlaywrightE2eStepRunner.RunAsync(_page, "sign-in-open-app", async () =>
        {
            await PlaywrightPageInteractions.GotoRelativeCommitAsync(_page, "LoginPage");
            await PlaywrightPageInteractions.WaitForSplashVisibleAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.LoginStep01Open);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "sign-in-open-form", async () =>
        {
            await PlaywrightPageInteractions.WaitForLoginFormAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.LoginStep01Logon);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "sign-in-enter-credentials", async () =>
        {
            await FillLoginFieldsAsync();
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.LoginStep03CredentialsFilled);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "sign-in-log-in-click", async () =>
        {
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.LoginStep04LogIn,
                PlaywrightPageInteractions.LoginSubmitButton(_page));
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "sign-in-log-in", async () =>
        {
            await SubmitLoginAsync();
            await PlaywrightPageInteractions.WaitForApplicationShellAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(_page, UserManualMediaCaptureKeys.LoginStep02ReportDashboard);
        });
    }

    /// <summary>
    /// Locked Register employee guide: Employees list → New → required fields → Save → reopen from list.
    /// Signs in without overwriting Sign in guide captures.
    /// </summary>
    internal async Task RunRegisterEmployeeAsync(string personalNumber, string firstName, string lastName)
    {
        await SignInWithoutManualCapturesAsync();

        await PlaywrightE2eStepRunner.RunAsync(_page, "register-open-employees", async () =>
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
            await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterStep01EmployeesList);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "register-new-click", async () =>
        {
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterStep02New,
                PlaywrightPageInteractions.VisibleToolbarButton(_page, "New"));
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "register-create-employee", async () =>
        {
            await CreateEmployeeAsync(personalNumber, firstName, lastName, skipListNavigation: true);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterStep02SavedDetail);
        });

        await PlaywrightE2eStepRunner.RunAsync(_page, "register-open-from-list", async () =>
        {
            await OpenEmployeeFromListAsync(personalNumber, $"{firstName} {lastName}");
            await PlaywrightPageInteractions.WaitForEmployeeDetailAsync(_page);
            await PlaywrightScreenshotCapture.CaptureAsync(
                _page,
                UserManualMediaCaptureKeys.PersonRegisterStep03OpenFromList);
        });

        Assert.Equal(firstName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(lastName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-last-name", E2ETestPersonFieldCaptions.LastName));
        Assert.Equal(personalNumber, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));
    }

    /// <summary>
    /// Find-and-open: Employees list has decoy rows; search opens the target employee.
    /// </summary>
    internal async Task RunFindEmployeeAsync()
    {
        await SignInWithoutManualCapturesAsync();

        await PlaywrightE2eStepRunner.RunAsync(_page, "find-open-employees", async () =>
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
            await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
            await Task.Delay(1000);

            int rows = await PlaywrightPageInteractions.CountEmployeeListRowsAsync(_page);
            if (rows < 2)
            {
                throw new InvalidOperationException(
                    $"Find-and-open needs more than one employee; Employees list has {rows} row(s).");
            }
        });

        var target = E2ETestFindEmployeeDecoyValues.SearchTarget;
        await PlaywrightE2eStepRunner.RunAsync(_page, "find-search-and-open", async () =>
        {
            await PlaywrightPageInteractions.ClearListSearchFilterAsync(_page);
            await PlaywrightPageInteractions.ClickListRowContainingAsync(_page, target.LastName);
            await PlaywrightPageInteractions.WaitForEmployeeDetailAsync(_page);
        });

        Assert.Equal(target.FirstName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-first-name", E2ETestPersonFieldCaptions.FirstName));
        Assert.Equal(target.LastName, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-last-name", E2ETestPersonFieldCaptions.LastName));
        Assert.Equal(target.PersonalNumber, await PlaywrightPageInteractions.ReadFieldAsync(_page, "e2e-person-personal-number", E2ETestPersonFieldCaptions.PersonalNumber));
    }

    private async Task SignInWithoutManualCapturesAsync()
    {
        await PlaywrightPageInteractions.GotoRelativeCommitAsync(_page, "LoginPage");
        await PlaywrightPageInteractions.WaitForLoginFormAsync(_page);
        await FillLoginFieldsAsync();
        await SubmitLoginAsync();
        await PlaywrightPageInteractions.WaitForApplicationShellAsync(_page);
    }

    private async Task LoginAsync(bool skipNavigation = false)
    {
        if (!skipNavigation)
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, "LoginPage");
        await FillLoginFieldsAsync();
        await SubmitLoginAsync();
    }

    private async Task FillLoginFieldsAsync()
    {
        await PlaywrightPageInteractions.FillTextFieldAsync(_page, "e2e-login-user-name", PlaywrightE2eEnvironment.UserName, "User Name");
        ILocator password = PlaywrightPageInteractions.LoginPasswordField(_page);
        await password.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000,
        });
        string value = PlaywrightE2eEnvironment.Password;
        _loginPasswordWasDummyForCapture = string.IsNullOrEmpty(value);
        if (_loginPasswordWasDummyForCapture)
            value = "Visa2026";

        await password.ClickAsync(new LocatorClickOptions { Force = true });
        await password.FillAsync(string.Empty);
        await password.PressSequentiallyAsync(value, new LocatorPressSequentiallyOptions { Delay = 30 });
        await password.PressAsync("Tab");
        await Task.Delay(400);
    }

    private async Task SubmitLoginAsync()
    {
        if (_loginPasswordWasDummyForCapture)
        {
            ILocator password = PlaywrightPageInteractions.LoginPasswordField(_page);
            await password.FillAsync(string.Empty);
        }

        await PlaywrightPageInteractions.LoginSubmitButton(_page).ClickAsync();
        await _page.WaitForURLAsync(
            url => !url.Contains("LoginPage", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 120_000 });
    }

    private async Task CreateEmployeeAsync(
        string personalNumber,
        string firstName,
        string lastName,
        bool skipListNavigation = false)
    {
        if (!skipListNavigation)
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
            await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
        }

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

        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestVisaFamilyManualUi.FieldCaption);
        await PlaywrightPageInteractions.EnsureFieldRenderedAsync(_page, E2ETestPersonFieldCaptions.FirstName);
        await EnsureEmployeeRequiredLookupsBoundAsync();
        await PlaywrightScreenshotCapture.CaptureAsync(
            _page,
            UserManualMediaCaptureKeys.PersonRegisterStep03FieldsFilled);

        await SaveEmployeeDetailAndConfirmAsync(personalNumber, $"{firstName} {lastName}");
    }

    private async Task SaveEmployeeDetailAndConfirmAsync(string personalNumber, string? documentTabTitle = null)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (!string.IsNullOrWhiteSpace(documentTabTitle))
            {
                await PlaywrightPageInteractions.ActivateMdiDocumentTabAsync(_page, documentTabTitle);
            }

            await EnsureEmployeeRequiredLookupsBoundAsync();

            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Save");
            await Task.Delay(3000);

            if (await PlaywrightPageInteractions.PageShowsDuplicatePersonalNumberAsync(_page))
            {
                await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
                await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
                await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
                await Task.Delay(2000);
                if (await TryFindEmployeeInListAsync(personalNumber, documentTabTitle))
                    return;

                throw new InvalidOperationException(
                    $"Personal Number '{personalNumber}' is reported as duplicate but the Employees list row was not found.");
            }

            if (await PlaywrightPageInteractions.PageShowsValidationErrorAsync(_page))
            {
                continue;
            }

            if (await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
                return;

            if (await TryFindEmployeeInListAsync(personalNumber, documentTabTitle))
                return;
        }

        await PlaywrightPageInteractions.DumpPageDiagnosticsAsync(_page, "employee-save-failed");
        throw new InvalidOperationException(
            $"Employee with Personal Number '{personalNumber}' was not confirmed after Save.");
    }

    private async Task EnsureEmployeeRequiredLookupsBoundAsync()
    {
        (string Css, string Display, string Caption)[] lookups =
        [
            ("e2e-person-country-of-birth", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.CountryOfBirth),
            ("e2e-person-gender", E2ETestEmployeeCreateValues.GenderDisplay, E2ETestPersonFieldCaptions.Gender),
            ("e2e-person-marital-status", E2ETestEmployeeCreateValues.MaritalStatusDisplay, E2ETestPersonFieldCaptions.MaritalStatus),
            ("e2e-person-nationality", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.Nationality),
            ("e2e-person-foreign-address-country", E2ETestEmployeeCreateValues.CountryDisplay, E2ETestPersonFieldCaptions.ForeignAddressCountry),
            ("e2e-person-project-contract", E2ETestEmployeeCreateValues.ProjectContractDisplay, E2ETestPersonFieldCaptions.ProjectContract),
            ("e2e-person-subcontractor", E2ETestEmployeeCreateValues.SubcontractorDisplay, E2ETestPersonFieldCaptions.Subcontractor),
        ];

        foreach ((string css, string display, string caption) in lookups)
        {
            await PlaywrightPageInteractions.EnsureLookupBoundAsync(_page, css, display, caption);
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

    private async Task OpenEmployeeFromListAsync(string personalNumber, string? fullName = null)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await PlaywrightPageInteractions.GotoRelativeAsync(_page, E2ETestLoginValues.EmployeesListViewPath);
            await PlaywrightPageInteractions.WaitForEmployeesListAsync(_page);
            await PlaywrightPageInteractions.ClickToolbarByTitlePrefixAsync(_page, "Refresh");
            await Task.Delay(1000);

            if (await TryFindEmployeeInListAsync(personalNumber, fullName))
                return;
        }

        throw new InvalidOperationException($"Employee list row containing '{personalNumber}' was not found.");
    }

    private async Task<bool> TryFindEmployeeInListAsync(string personalNumber, string? fullName = null)
    {
        string[] tokens =
        [
            personalNumber,
            fullName ?? string.Empty,
        ];

        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;

            try
            {
                await PlaywrightPageInteractions.ClearListSearchFilterAsync(_page);
                await PlaywrightPageInteractions.ClickListRowContainingAsync(_page, token);
                if (await EmployeeDetailShowsPersonalNumberAsync(personalNumber))
                    return true;
            }
            catch (TimeoutException)
            {
                // Try the next token (Employees list shows Full Name, not Personal Number).
            }
        }

        return false;
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

        await SaveEmployeeDetailAndConfirmAsync(
            personalNumber,
            E2ETestPassportCreateOnlyJourneyValues.FullName);

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
