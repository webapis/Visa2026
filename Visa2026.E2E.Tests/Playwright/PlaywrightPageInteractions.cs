using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>DOM helpers for XAF Blazor — uses injected <c>e2e-*</c> CSS classes and toolbar titles.</summary>
internal static class PlaywrightPageInteractions
{
    internal static string Url(IPage page, string relativePath) =>
        $"{PlaywrightE2eEnvironment.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

    internal static async Task GotoRelativeAsync(IPage page, string relativePath)
    {
        await page.GotoAsync(Url(page, relativePath), new PageGotoOptions
        {
            // Blazor Server keeps SignalR open — NetworkIdle never settles.
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 120_000,
        });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    internal static async Task FillTextFieldAsync(IPage page, string cssClass, string value, string? caption = null)
    {
        if (LooksLikeMaskedDate(value))
        {
            await FillDateFieldAsync(page, cssClass, value, caption);
            return;
        }

        ILocator locator = await ResolveFieldLocatorAsync(page, cssClass, caption);
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync(new LocatorClickOptions { Force = true });
        await locator.FillAsync(string.Empty);
        await locator.FillAsync(value);

        string actual = (await locator.InputValueAsync()).Trim();
        if (!string.Equals(actual, value, StringComparison.Ordinal))
        {
            await locator.PressAsync("Control+A");
            await locator.PressSequentiallyAsync(value);
            await locator.PressAsync("Tab");
        }
    }

    internal static async Task FillDateFieldAsync(IPage page, string cssClass, string value, string? caption = null)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            ILocator? visibleXaf = await TryGetVisibleXafItemLocatorAsync(page, cssClass);
            if (visibleXaf != null)
            {
                await visibleXaf.ScrollIntoViewIfNeededAsync();
                await FillMaskedInputAsync(visibleXaf, value);
                return;
            }
        }

        ILocator locator = await ResolveFieldLocatorAsync(page, cssClass, caption);
        await locator.ScrollIntoViewIfNeededAsync();
        await FillMaskedInputAsync(locator, value);
    }

    private static async Task<ILocator> ResolveFieldLocatorAsync(IPage page, string cssClass, string? caption)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            ILocator? xafLocator = await TryGetVisibleXafItemLocatorAsync(page, cssClass);
            if (xafLocator != null)
                return xafLocator;

            ILocator[] cssLocators =
            [
                page.Locator($".{cssClass} input, .{cssClass} textarea, .{cssClass} .dxbl-text-edit-input, .{cssClass} .dxbl-text-edit input"),
                page.Locator($"[class*='{cssClass}'] input, [class*='{cssClass}'] textarea, [class*='{cssClass}'] .dxbl-text-edit-input, [class*='{cssClass}'] .dxbl-text-edit input"),
            ];

            foreach (ILocator cssLocator in cssLocators)
            {
                ILocator? visible = await FindFirstVisibleLocatorAsync(cssLocator);
                if (visible != null)
                    return visible;
            }
        }

        if (!string.IsNullOrWhiteSpace(caption))
        {
            foreach (string alias in GetCaptionAliases(caption))
            {
                ILocator labelLocator = page.GetByLabel(alias, new PageGetByLabelOptions { Exact = false });
                ILocator? visibleLabel = await FindFirstVisibleLocatorAsync(labelLocator);
                if (visibleLabel != null)
                    return visibleLabel;

                ILocator layoutLocator = LayoutInputByCaption(page, alias);
                ILocator? visibleLayout = await FindFirstVisibleLocatorAsync(layoutLocator);
                if (visibleLayout != null)
                    return visibleLayout;
            }
        }

        await DumpPageDiagnosticsAsync(page, $"missing-field-{cssClass}");
        throw new TimeoutException(
            $"Could not find visible field '{cssClass}'{(caption != null ? $" ({caption})" : string.Empty)} (URL: {page.Url}).");
    }

    internal static async Task<ILocator> WaitForPassportNumberFieldAsync(IPage page)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await ActivateMdiPassportTabAsync(page);
            ILocator? visible = await FindFirstVisibleLocatorAsync(
                page.Locator(
                    "label[class*='xaf-item-passportnumber'] + div input, " +
                    "label[class*='xaf-item-passportnumber'] + div .dxbl-text-edit input"));
            if (visible != null)
            {
                await visible.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5_000,
                });
                return visible;
            }

            await Task.Delay(1_000);
        }

        throw new TimeoutException($"Passport Number field not visible (URL: {page.Url}).");
    }

    private static async Task<ILocator?> FindFirstVisibleLocatorAsync(ILocator candidates)
    {
        int count = await candidates.CountAsync();
        for (var i = 0; i < count; i++)
        {
            ILocator candidate = candidates.Nth(i);
            try
            {
                if (await candidate.IsVisibleAsync())
                    return candidate;
            }
            catch (PlaywrightException)
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private static async Task<ILocator?> TryGetVisibleXafItemLocatorAsync(IPage page, string cssClass)
    {
        string? itemSuffix = GetXafItemSuffix(cssClass);
        if (string.IsNullOrEmpty(itemSuffix))
            return null;

        ILocator candidates = page.Locator(
            $"label[class*='xaf-item-{itemSuffix}'] + div input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div .dxbl-text-edit-input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div .dxbl-text-edit input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div textarea");
        return await FindFirstVisibleLocatorAsync(candidates);
    }

    private static string? GetXafItemSuffix(string cssClass) =>
        cssClass switch
        {
            _ when cssClass.StartsWith("e2e-person-", StringComparison.Ordinal) =>
                cssClass["e2e-person-".Length..].Replace("-", "", StringComparison.Ordinal),
            _ when cssClass.StartsWith("e2e-passport-", StringComparison.Ordinal) =>
                cssClass["e2e-passport-".Length..].Replace("-", "", StringComparison.Ordinal),
            _ when cssClass.StartsWith("e2e-visa-", StringComparison.Ordinal) =>
                cssClass["e2e-visa-".Length..].Replace("-", "", StringComparison.Ordinal),
            _ => null,
        };

    private static ILocator? TryGetXafItemLocator(IPage page, string cssClass)
    {
        string? itemSuffix = GetXafItemSuffix(cssClass);
        if (string.IsNullOrEmpty(itemSuffix))
            return null;

        return page.Locator(
            $"label[class*='xaf-item-{itemSuffix}'] + div .dxbl-text-edit-input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div .dxbl-text-edit input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div textarea").First;
    }

    internal static async Task DumpPageDiagnosticsAsync(IPage page, string label)
    {
        try
        {
            string html = await page.ContentAsync();
            string failuresDir = Path.Combine(
                Path.GetDirectoryName(ResolveScreenshotRunDirectory()) ?? Path.GetTempPath(),
                "failures");
            Directory.CreateDirectory(failuresDir);
            string path = Path.Combine(failuresDir, $"visa-pw-{SanitizeDiagnosticLabel(label)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");
            await File.WriteAllTextAsync(path, html, Encoding.UTF8);
            Console.WriteLine($"[Playwright] DOM dump: {path} (URL: {page.Url})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playwright] DOM dump failed: {ex.Message}");
        }
    }

    private static string ResolveScreenshotRunDirectory()
    {
        string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
            ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory,
            @"..\..\..\recordings\screenshots",
            runId));
    }

    private static string SanitizeDiagnosticLabel(string label) =>
        string.Join("_", (label ?? "diag").Split(Path.GetInvalidFileNameChars()));

    private static async Task<bool> TryWaitVisibleAsync(ILocator locator, int timeoutMs)
    {
        try
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs,
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static ILocator LayoutItemByCaption(IPage page, string caption)
    {
        string literal = caption.Replace("'", "\\'");
        return page.Locator(
            "xpath=//*[contains(@class,'dxbl-form-layout-item-caption') and contains(normalize-space(.),'" + literal + "')]" +
            "/ancestor::div[contains(@class,'dxbl-fl')][1]");
    }

    private static ILocator LayoutInputByCaption(IPage page, string caption)
    {
        return LayoutItemByCaption(page, caption).Locator("input:not([type='hidden']), textarea, .dxbl-text-edit-input");
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
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.PassportNumber, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.PassportNumber;
            yield return "Passport Number";
        }
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.PassportType, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.PassportType;
            yield return "Passport Type";
        }
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.IssueDate, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.IssueDate;
            yield return "Issue Date";
        }
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.ExpirationDate, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.ExpirationDate;
            yield return "Expiration Date";
        }
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.Authority, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.Authority;
            yield return "Authority";
        }
        else if (string.Equals(caption, E2ETestPassportFieldCaptions.IssuedCountry, StringComparison.OrdinalIgnoreCase))
        {
            yield return E2ETestPassportFieldCaptions.IssuedCountry;
            yield return "Issued Country";
        }
    }

    private static bool LooksLikeMaskedDate(string value) =>
        value.Length == 10 && value[2] == '.' && value[5] == '.';

    private static async Task FillMaskedInputAsync(ILocator locator, string value)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync(new LocatorClickOptions { Force = true });
        await locator.PressAsync("Control+A");
        await locator.PressSequentiallyAsync(value);
        await locator.PressAsync("Tab");
    }

    internal static async Task FillLookupAsync(IPage page, string cssClass, string displayValue, string? caption = null)
    {
        ILocator input = await ResolveFieldLocatorAsync(page, cssClass, caption);
        await input.ScrollIntoViewIfNeededAsync();
        await input.ClickAsync(new LocatorClickOptions { Force = true });
        await input.FillAsync(string.Empty);
        await input.FillAsync(displayValue);
        await input.PressAsync("Enter");
        await Task.Delay(400);

        if (await TrySelectLookupOptionAsync(page, displayValue))
            return;

        await input.PressAsync("ArrowDown");
        await input.PressAsync("Enter");
        await Task.Delay(300);
        if (await TrySelectLookupOptionAsync(page, displayValue))
            return;

        foreach (string token in GetLookupSearchTokens(displayValue))
        {
            if (string.Equals(token, displayValue, StringComparison.Ordinal))
                continue;

            await input.ClickAsync(new LocatorClickOptions { Force = true });
            await input.FillAsync(string.Empty);
            await input.FillAsync(token);
            await input.PressAsync("Enter");
            await Task.Delay(400);
            if (await TrySelectLookupOptionAsync(page, token))
                return;
        }

        await input.PressAsync("Tab");
        await Task.Delay(200);
    }

    /// <summary>
    /// Lookup editors can keep filter text without selecting a row — retry until the display value binds.
    /// </summary>
    internal static async Task EnsureLookupBoundAsync(
        IPage page,
        string cssClass,
        string displayValue,
        string? caption = null,
        int maxAttempts = 6)
    {
        string actual = await TryReadLookupDisplayAsync(page, cssClass, caption);
        if (LookupDisplayMatches(actual, displayValue))
            return;

        await FillLookupUntilBoundAsync(page, cssClass, displayValue, caption, maxAttempts);
    }

    internal static async Task FillLookupUntilBoundAsync(
        IPage page,
        string cssClass,
        string displayValue,
        string? caption = null,
        int maxAttempts = 6)
    {
        string bound = await TryReadLookupDisplayAsync(page, cssClass, caption);
        if (LookupDisplayMatches(bound, displayValue))
            return;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await FillLookupAsync(page, cssClass, displayValue, caption);
            await Task.Delay(300);

            string actual = await TryReadLookupDisplayAsync(page, cssClass, caption);
            if (LookupDisplayMatches(actual, displayValue))
                return;
        }

        await DumpPageDiagnosticsAsync(page, $"lookup-not-bound-{cssClass}");
        throw new TimeoutException(
            $"Lookup '{cssClass}' did not bind to '{displayValue}' (last: '{await TryReadLookupDisplayAsync(page, cssClass, caption)}').");
    }

    internal static async Task<string> TryReadLookupDisplayAsync(IPage page, string cssClass, string? caption = null)
    {
        try
        {
            ILocator? xafLocator = TryGetXafItemLocator(page, cssClass);
            if (xafLocator != null && await TryWaitVisibleAsync(xafLocator, 3_000))
            {
                string xafValue = (await xafLocator.InputValueAsync()).Trim();
                if (!string.IsNullOrWhiteSpace(xafValue))
                    return xafValue;
            }

            ILocator input = page.Locator(
                $".{cssClass} input, .{cssClass} .dxbl-text-edit-input, [class*='{cssClass}'] input, [class*='{cssClass}'] .dxbl-text-edit-input")
                .First;
            if (await TryWaitVisibleAsync(input, 3_000))
            {
                string inputValue = (await input.InputValueAsync()).Trim();
                if (!string.IsNullOrWhiteSpace(inputValue))
                    return inputValue;
            }

            ILocator container = page.Locator($".{cssClass}, [class*='{cssClass}']").First;
            if (await container.CountAsync() > 0)
            {
                string inner = (await container.InnerTextAsync())?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(inner))
                    return inner;
            }

            if (!string.IsNullOrWhiteSpace(caption))
            {
                foreach (string alias in GetCaptionAliases(caption))
                {
                    ILocator labelLocator = page.GetByLabel(alias, new PageGetByLabelOptions { Exact = false });
                    if (await TryWaitVisibleAsync(labelLocator, 2_000))
                        return (await labelLocator.InputValueAsync()).Trim();
                }
            }

            return string.Empty;
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }

    internal static bool LookupDisplayMatches(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
            return false;

        if (actual.Contains(expected, StringComparison.OrdinalIgnoreCase)
            || expected.Contains(actual, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string token in GetLookupSearchTokens(expected))
        {
            if (actual.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> GetLookupSearchTokens(string displayValue)
    {
        yield return displayValue;

        int dash = displayValue.IndexOf('—', StringComparison.Ordinal);
        if (dash > 0)
        {
            string prefix = displayValue[..dash].Trim();
            string suffix = displayValue[(dash + 1)..].Trim();
            if (!string.IsNullOrEmpty(prefix))
                yield return prefix;
            if (!string.IsNullOrEmpty(suffix))
                yield return suffix;
        }

        int hyphen = displayValue.IndexOf('-', StringComparison.Ordinal);
        if (hyphen > 0 && hyphen != dash)
        {
            string prefix = displayValue[..hyphen].Trim();
            if (!string.IsNullOrEmpty(prefix))
                yield return prefix;
        }
    }

    private static async Task<bool> TrySelectLookupOptionAsync(IPage page, string optionText)
    {
        foreach (string token in GetLookupSearchTokens(optionText))
        {
            string literal = token.Replace("'", "\\'");
            ILocator candidates = page.Locator(
                $"xpath=//*[contains(@class,'dxbl-list-box-item') or contains(@class,'dxbl-list-item') or @role='option']" +
                $"[contains(normalize-space(.), '{literal}')]");

            int count = await candidates.CountAsync();
            for (var i = 0; i < count; i++)
            {
                ILocator candidate = candidates.Nth(i);
                if (!await candidate.IsVisibleAsync())
                    continue;

                try
                {
                    await candidate.ClickAsync(new LocatorClickOptions { Timeout = 3_000 });
                    await Task.Delay(200);
                    return true;
                }
                catch (TimeoutException)
                {
                    // Try the next visible match.
                }
            }
        }

        return false;
    }

    internal static async Task WaitForPassportsNestedListAsync(IPage page)
    {
        ILocator newPassport = PassportsNestedNewButton(page);
        await newPassport.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    internal static async Task ClickPassportsNestedNewAsync(IPage page)
    {
        if (await IsPassportDetailOpenAsync(page))
            return;

        await ClickTabAsync(page, "Passports");
        await Task.Delay(500);
        await WaitForPassportsNestedListAsync(page);

        await PassportsNestedNewButton(page).ClickAsync();
        await Task.Delay(1_500);
        await ActivateMdiPassportTabAsync(page);
        await WaitForPassportNumberFieldAsync(page);
    }

    internal static async Task<ILocator> WaitForVisaNumberFieldAsync(IPage page)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await ActivateMdiVisaTabAsync(page);
            ILocator? visible = await FindFirstVisibleLocatorAsync(
                page.Locator(
                    "label[class*='xaf-item-visanumber'] + div input, " +
                    "label[class*='xaf-item-visanumber'] + div .dxbl-text-edit input"));
            if (visible != null)
            {
                await visible.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5_000,
                });
                return visible;
            }

            await Task.Delay(1_000);
        }

        throw new TimeoutException($"Visa Number field not visible (URL: {page.Url}).");
    }

    internal static async Task ClickPassportVisasNestedNewAsync(IPage page)
    {
        if (await IsVisaDetailOpenAsync(page))
            return;

        await ActivateMdiPassportTabAsync(page);
        await ClickTabAsync(page, "Visas");
        await Task.Delay(500);

        ILocator newVisa = page.Locator(
            "button[data-action-name='New'][title^='New Visa']:not([dxbl-virtual-el]):visible").First;
        await newVisa.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await newVisa.ClickAsync();
        await Task.Delay(1_500);
        await ActivateMdiVisaTabAsync(page);
        await WaitForVisaNumberFieldAsync(page);
    }

    internal static async Task ActivateMdiVisaTabAsync(IPage page)
    {
        ILocator tabs = page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Visa", Exact = true });
        int count = await tabs.CountAsync();
        for (var i = 0; i < count; i++)
        {
            ILocator tab = tabs.Nth(i);
            if (!await tab.IsVisibleAsync())
                continue;

            string? selected = await tab.GetAttributeAsync("aria-selected");
            if (string.Equals(selected, "true", StringComparison.OrdinalIgnoreCase))
                return;

            await tab.ClickAsync(new LocatorClickOptions { Force = true });
            await Task.Delay(500);
            return;
        }
    }

    private static async Task<bool> IsVisaDetailOpenAsync(IPage page)
    {
        try
        {
            return await FindFirstVisibleLocatorAsync(
                page.Locator("label[class*='xaf-item-visanumber'] + div input")) != null;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    internal static async Task ActivateMdiPassportTabAsync(IPage page)
    {
        ILocator tabs = page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Passport", Exact = true });
        int count = await tabs.CountAsync();
        for (var i = 0; i < count; i++)
        {
            ILocator tab = tabs.Nth(i);
            if (!await tab.IsVisibleAsync())
                continue;

            string? selected = await tab.GetAttributeAsync("aria-selected");
            if (string.Equals(selected, "true", StringComparison.OrdinalIgnoreCase))
                return;

            await tab.ClickAsync(new LocatorClickOptions { Force = true });
            await Task.Delay(500);
            return;
        }
    }

    private static async Task<bool> IsPassportDetailOpenAsync(IPage page)
    {
        try
        {
            return await FindFirstVisibleLocatorAsync(
                page.Locator("label[class*='xaf-item-passportnumber'] + div input")) != null;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    private static ILocator PassportsNestedNewButton(IPage page) =>
        page.Locator(
            "[class*='e2e-person-employee-tab-passports-list'] button[data-action-name='New'][title^='New Passport']:not([dxbl-virtual-el]):visible, " +
            "button[data-action-name='New'][title^='New Passport']:not([dxbl-virtual-el]):visible").First;

    internal static async Task ClickToolbarByTitlePrefixAsync(IPage page, string titlePrefix)
    {
        ILocator button = page.Locator(
            $"button[title^='{titlePrefix}']:not([dxbl-virtual-el]), " +
            $"div.dxbl-btn-split[title^='{titlePrefix}'] button:not([dxbl-virtual-el])");
        await button.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
        await button.First.ClickAsync();
    }

    internal static async Task ClickTabAsync(IPage page, params string[] tabTexts)
    {
        foreach (string tab in tabTexts)
        {
            ILocator locator = page.Locator($"[role='tab']:has-text('{tab}')");
            if (await locator.CountAsync() == 0)
                continue;

            ILocator tabItem = locator.First;
            string? selected = await tabItem.GetAttributeAsync("aria-selected");
            if (string.Equals(selected, "true", StringComparison.OrdinalIgnoreCase))
                return;

            await tabItem.ScrollIntoViewIfNeededAsync();
            await tabItem.ClickAsync(new LocatorClickOptions { Force = true });
            await Task.Delay(500);
            return;
        }

        throw new InvalidOperationException($"Tab not found: {string.Join(" | ", tabTexts)}");
    }

    /// <summary>
    /// Post-login shell — splash dismissed, Report Dashboard / navigation visible (not loading panel).
    /// </summary>
    internal static async Task WaitForApplicationShellAsync(IPage page)
    {
        await page.WaitForURLAsync(
            url => !url.Contains("LoginPage", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 120_000 });

        for (var attempt = 0; attempt < 240; attempt++)
        {
            if (await IsApplicationShellReadyAsync(page))
                break;

            await Task.Delay(500);
        }

        await page.GetByText("Report Dashboard", new PageGetByTextOptions { Exact = false }).First
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 120_000,
            });

        ILocator shellReady = page.Locator("button[title^='Refresh']")
            .Or(page.GetByText("Employees", new PageGetByTextOptions { Exact = true }));
        await shellReady.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    private static async Task<bool> IsApplicationShellReadyAsync(IPage page)
    {
        ILocator splash = page.Locator(".visa-splash-screen");
        if (await splash.CountAsync() > 0 && await splash.First.IsVisibleAsync())
            return false;

        ILocator panel = page.Locator("#applicationLoadingPanel");
        if (await panel.CountAsync() > 0)
        {
            try
            {
                string? panelClass = await panel.First.GetAttributeAsync("class", new LocatorGetAttributeOptions
                {
                    Timeout = 3_000,
                });
                if (panelClass == null || !panelClass.Contains("loading-hide", StringComparison.Ordinal))
                    return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        ILocator dashboard = page.GetByText("Report Dashboard", new PageGetByTextOptions { Exact = false });
        return await dashboard.CountAsync() > 0 && await dashboard.First.IsVisibleAsync();
    }

    internal static async Task WaitForEmployeesListAsync(IPage page)
    {
        await page.Locator("button[data-action-name='New'], button[title='New']").First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }

    /// <summary>After <c>New</c> on employees list — TabbedMDI may keep URL at <c>/</c>.</summary>
    internal static async Task WaitForEmployeeDetailAsync(IPage page)
    {
        await page.Locator("button[title^='Save']:not([dxbl-virtual-el])").First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
        await page.GetByLabel(E2ETestPersonFieldCaptions.FirstName, new PageGetByLabelOptions { Exact = false })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    /// <summary>Scrolls the detail form until a captioned field is rendered (virtualized Blazor layout).</summary>
    internal static async Task EnsureFieldRenderedAsync(IPage page, string caption)
    {
        foreach (string alias in GetCaptionAliases(caption))
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                ILocator field = LayoutInputByCaption(page, alias);
                if (await field.CountAsync() > 0 && await field.First.IsVisibleAsync())
                    return;

                ILocator viewport = page.Locator(".dxbl-fl-viewport, .dxbl-scroll-viewer, .dxbl-form-layout").First;
                if (await viewport.CountAsync() > 0)
                {
                    await viewport.EvaluateAsync("el => { el.scrollTop += 180; }");
                }
                else
                {
                    await page.Mouse.WheelAsync(0, 180);
                }

                await Task.Delay(200);
            }
        }
    }

    internal static async Task ClickListRowContainingAsync(IPage page, string text)
    {
        ILocator row = page.Locator(
            $"xpath=//table[contains(@class,'dxbl-grid')]//tr[contains(@class,'dxbl-grid-data-row') and contains(., '{text}')]");
        await row.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
        await row.First.ClickAsync();
        await Task.Delay(500);
    }

    internal static async Task<string> ReadFieldAsync(IPage page, string cssClass, string? caption = null)
    {
        ILocator input = await ResolveFieldLocatorAsync(page, cssClass, caption);
        return await input.InputValueAsync();
    }

    internal static ILocator LoginSubmitButton(IPage page) =>
        page.Locator(".e2e-login-submit button, button[title='Log In'], button:has-text('Log In')").First;

    internal static ILocator ToolbarButton(IPage page, string titlePrefix) =>
        page.Locator(
            $"button[title^='{titlePrefix}']:not([dxbl-virtual-el]), " +
            $"div.dxbl-btn-split[title^='{titlePrefix}'] button:not([dxbl-virtual-el])").First;

    internal static ILocator VisibleToolbarButton(IPage page, string titlePrefix) =>
        page.Locator(
            $"button[title^='{titlePrefix}']:not([dxbl-virtual-el]):visible, " +
            $"div.dxbl-btn-split[title^='{titlePrefix}']:visible button:not([dxbl-virtual-el])").First;

    internal static ILocator NavigationMenuItem(IPage page, string text) =>
        page.Locator(
                ".dxbl-tree-view, .dxbl-menu, nav, .dxbl-side-panel, [class*='xaf-navigation'], [class*='Navigation']")
            .GetByText(text, new LocatorGetByTextOptions { Exact = true })
            .First;

    internal static ILocator TabItem(IPage page, string tabText) =>
        page.Locator($"[role='tab']:has-text('{tabText}')").First;

    internal static ILocator VisaFamilyManualFieldContainer(IPage page) =>
        page.Locator(
            ".e2e-person-visa-application-family-members-text, " +
            "[class*='e2e-person-visa-application-family-members-text'], " +
            ".visa-family-lines-inline").First;

    internal static async Task OpenVisaFamilyManualPopupAsync(IPage page)
    {
        await EnsureFieldRenderedAsync(page, E2ETestVisaFamilyManualUi.FieldCaption);
        ILocator container = VisaFamilyManualFieldContainer(page);
        await container.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await container.ScrollIntoViewIfNeededAsync();
        ILocator openButton = container.Locator(".e2e-visa-family-manual-open button, .e2e-visa-family-manual-open, button").First;
        await openButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await openButton.ClickAsync();
        await page.Locator(".visa-family-lines-popup").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    internal static async Task FillVisaFamilyManualMemberFormAsync(IPage page)
    {
        ILocator edit = page.Locator(".visa-family-lines-edit");
        await edit.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        ILocator fullName = VisaFamilyManualEditInputByLabel(edit, E2ETestVisaFamilyManualUi.FullName);
        await fullName.FillAsync(E2ETestVisaFamilyManualValues.MemberFullName);

        ILocator birthDate = VisaFamilyManualEditInputByLabel(edit, E2ETestVisaFamilyManualUi.BirthDate);
        await FillMaskedInputAsync(birthDate, E2ETestVisaFamilyManualValues.MemberBirthDate);

        await FillVisaFamilyManualComboAsync(edit, E2ETestVisaFamilyManualUi.Relationship, E2ETestVisaFamilyManualValues.MemberRelationshipDisplay);
        await FillVisaFamilyManualComboAsync(edit, E2ETestVisaFamilyManualUi.Country, E2ETestVisaFamilyManualValues.MemberCountryDisplay);
    }

    private static ILocator VisaFamilyManualEditInputByLabel(ILocator editScope, string label)
    {
        string literal = label.Replace("'", "\\'");
        return editScope.Locator(
            $"xpath=.//label[contains(normalize-space(),'{literal}')]/following-sibling::*[1]//input[not(@type='hidden')][1]");
    }

    private static async Task FillVisaFamilyManualComboAsync(ILocator editScope, string label, string displayValue)
    {
        ILocator input = VisaFamilyManualEditInputByLabel(editScope, label);
        await input.ScrollIntoViewIfNeededAsync();
        await input.ClickAsync(new LocatorClickOptions { Force = true });
        await input.FillAsync(string.Empty);
        await input.FillAsync(displayValue);
        await input.PressAsync("Enter");
        await Task.Delay(400);

        IPage page = editScope.Page;
        if (await TrySelectLookupOptionAsync(page, displayValue))
            return;

        await input.PressAsync("ArrowDown");
        await input.PressAsync("Enter");
        await Task.Delay(300);
        if (await TrySelectLookupOptionAsync(page, displayValue))
            return;

        foreach (string token in GetLookupSearchTokens(displayValue))
        {
            await input.ClickAsync(new LocatorClickOptions { Force = true });
            await input.FillAsync(string.Empty);
            await input.FillAsync(token);
            await input.PressAsync("Enter");
            await Task.Delay(400);
            if (await TrySelectLookupOptionAsync(page, token))
                return;
        }

        await input.PressAsync("Tab");
        await Task.Delay(200);
    }

    internal static async Task ClickVisaFamilyManualMainOkAsync(IPage page)
    {
        ILocator mainPopup = page.Locator(".dxbl-popup:visible")
            .Filter(new LocatorFilterOptions { Has = page.Locator(".visa-family-lines-popup") })
            .Last;
        await mainPopup.Locator(".cs-multi-select-popup__footer button")
            .Filter(new LocatorFilterOptions { HasText = E2ETestVisaFamilyManualUi.Ok })
            .First.ClickAsync();
    }

    internal static async Task ClickVisaFamilyManualEditSaveAsync(IPage page)
    {
        ILocator editPopup = page.Locator(".dxbl-popup:visible")
            .Filter(new LocatorFilterOptions { Has = page.Locator(".visa-family-lines-edit") })
            .Last;
        ILocator saveButton = editPopup.Locator(".cs-multi-select-popup__footer button").Filter(new LocatorFilterOptions { HasText = E2ETestVisaFamilyManualUi.SaveMember }).First;

        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (await saveButton.IsEnabledAsync())
            {
                await saveButton.ClickAsync();
                return;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException(
            "Visa family manual member Save stayed disabled — relationship or country may not be bound.");
    }

    internal static ILocator VisaFamilyManualPopupButton(IPage page, string cssClass) =>
        page.Locator($".{cssClass} button, [class*='{cssClass}']").First;

    private static async Task FillPopupComboByLabelAsync(IPage page, string label, string displayValue)
    {
        ILocator input = page.GetByLabel(label, new PageGetByLabelOptions { Exact = true });
        await input.ScrollIntoViewIfNeededAsync();
        await input.ClickAsync(new LocatorClickOptions { Force = true });
        await input.FillAsync(string.Empty);
        await input.FillAsync(displayValue);
        await input.PressAsync("Enter");
        await Task.Delay(400);

        if (await TrySelectLookupOptionAsync(page, displayValue))
            return;

        await input.PressAsync("Tab");
    }
}
