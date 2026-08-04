using System;
using System.Collections.Generic;
using System.IO;
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
        await locator.FillAsync(value);
    }

    internal static async Task FillDateFieldAsync(IPage page, string cssClass, string value, string? caption = null)
    {
        ILocator? xafLocator = TryGetXafItemLocator(page, cssClass);
        if (xafLocator != null)
        {
            await xafLocator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 8_000,
            });
            await FillMaskedInputAsync(xafLocator, value);
            return;
        }

        ILocator locator = await ResolveFieldLocatorAsync(page, cssClass, caption);
        await locator.ScrollIntoViewIfNeededAsync();
        await FillMaskedInputAsync(locator, value);
    }

    private static async Task<ILocator> ResolveFieldLocatorAsync(IPage page, string cssClass, string? caption)
    {
        ILocator? xafLocator = TryGetXafItemLocator(page, cssClass);
        if (xafLocator != null && await TryWaitVisibleAsync(xafLocator, 4_000))
            return xafLocator;

        ILocator[] cssLocators =
        [
            page.Locator($".{cssClass} input, .{cssClass} textarea, .{cssClass} .dxbl-text-edit-input"),
            page.Locator($"[class*='{cssClass}'] input, [class*='{cssClass}'] textarea, [class*='{cssClass}'] .dxbl-text-edit-input"),
        ];

        foreach (ILocator cssLocator in cssLocators)
        {
            if (await TryWaitVisibleAsync(cssLocator.First, 4_000))
                return cssLocator.First;
        }

        if (!string.IsNullOrWhiteSpace(caption))
        {
            foreach (string alias in GetCaptionAliases(caption))
            {
                ILocator labelLocator = page.GetByLabel(alias, new PageGetByLabelOptions { Exact = false });
                if (await TryWaitVisibleAsync(labelLocator, 4_000))
                    return labelLocator;

                ILocator layoutLocator = LayoutInputByCaption(page, alias);
                if (await TryWaitVisibleAsync(layoutLocator, 4_000))
                    return layoutLocator;
            }
        }

        await DumpPageDiagnosticsAsync(page, $"missing-field-{cssClass}");
        throw new TimeoutException($"Could not find visible field '{cssClass}'{(caption != null ? $" ({caption})" : string.Empty)}.");
    }

    private static ILocator? TryGetXafItemLocator(IPage page, string cssClass)
    {
        string? itemSuffix = cssClass switch
        {
            _ when cssClass.StartsWith("e2e-person-", StringComparison.Ordinal) =>
                cssClass["e2e-person-".Length..].Replace("-", "", StringComparison.Ordinal),
            _ when cssClass.StartsWith("e2e-passport-", StringComparison.Ordinal) =>
                cssClass["e2e-passport-".Length..].Replace("-", "", StringComparison.Ordinal),
            _ => null,
        };

        if (string.IsNullOrEmpty(itemSuffix))
            return null;

        return page.Locator(
            $"label[class*='xaf-item-{itemSuffix}'] + div .dxbl-text-edit-input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div input, " +
            $"label[class*='xaf-item-{itemSuffix}'] + div textarea").First;
    }

    internal static async Task DumpPageDiagnosticsAsync(IPage page, string label)
    {
        try
        {
            string path = Path.Combine(Path.GetTempPath(), $"visa-pw-{label}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");
            await File.WriteAllTextAsync(path, await page.ContentAsync());
            Console.WriteLine($"[Playwright] DOM dump: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playwright] DOM dump failed: {ex.Message}");
        }
    }

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
        await input.FillAsync(displayValue);
        await input.PressAsync("Enter");
        await Task.Delay(500);

        string literal = displayValue.Replace("'", "\\'");
        ILocator option = page.Locator(
            $"xpath=//*[contains(@class,'dxbl-list') or contains(@class,'dxbl-dropdown') or contains(@class,'dxbl-popup')]//*[contains(normalize-space(.), '{literal}')]");
        if (await option.CountAsync() > 0)
        {
            await option.First.ClickAsync();
            await Task.Delay(200);
        }
        else
        {
            await input.PressAsync("Tab");
        }
    }

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

            await locator.First.ClickAsync();
            await Task.Delay(500);
            return;
        }

        throw new InvalidOperationException($"Tab not found: {string.Join(" | ", tabTexts)}");
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
}
