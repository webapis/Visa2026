using System.Text.Json;
using Microsoft.Playwright;

namespace Visa2026.Tools.UiScenarioRunner;

internal sealed class ScenarioRunner
{
    private readonly HookResolver _hooks;
    private readonly RunOptions _options;

    public ScenarioRunner(HookResolver hooks, RunOptions options)
    {
        _hooks = hooks;
        _options = options;
    }

    public async Task<ScenarioRunResult> RunAsync(UiScenario scenario, CancellationToken cancellationToken = default)
    {
        string baseUrl = scenario.BaseUrl ?? _options.BaseUrl;
        var stepResults = new List<StepResult>();

        using var playwright = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless,
            SlowMo = _options.SlowMoMs > 0 ? _options.SlowMoMs : null,
        };
        var chromiumArgs = new List<string> { "--incognito" };
        if (!_options.Headless)
        {
            (int screenWidth, int screenHeight) = GetPrimaryScreenSize();
            chromiumArgs.AddRange(
            [
                "--start-maximized",
                "--window-position=0,0",
                $"--window-size={screenWidth},{screenHeight}",
                "--disable-infobars",
            ]);
        }

        launchOptions.Args = chromiumArgs.ToArray();
        await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

        BrowserNewContextOptions contextOptions = new()
        {
            IgnoreHTTPSErrors = true,
            // Isolated context per run — no cookies, localStorage, or culture cookie from prior runs.
        };

        if (_options.Headless)
        {
            contextOptions.ViewportSize = new ViewportSize { Width = 1280, Height = 720 };
        }
        else
        {
            (int screenWidth, int screenHeight) = GetPrimaryScreenSize();
            contextOptions.ViewportSize = ViewportSize.NoViewport;
            contextOptions.ScreenSize = new ScreenSize { Width = screenWidth, Height = screenHeight };
        }

        await using var context = await browser.NewContextAsync(contextOptions);
        await context.ClearCookiesAsync();

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(_options.TimeoutMs);

        if (!_options.Headless)
        {
            await MaximizeHeadedWindowAsync(page);
        }

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<string, object> step = scenario.Steps[i];
            if (step.Count != 1)
            {
                return Fail(scenario.Id, stepResults, $"Step {i + 1}: expected one key per step, got {step.Count}.");
            }

            KeyValuePair<string, object> pair = step.First();
            try
            {
                await TryCaptureScreenshotAsync(
                    page,
                    ResolveStepScreenshotPath(scenario.Id, i + 1, pair.Key, "before"));
                StepResult result = await ExecuteStepAsync(page, baseUrl, scenario, pair.Key, pair.Value, i + 1, cancellationToken);
                await TryCaptureScreenshotAsync(
                    page,
                    ResolveStepScreenshotPath(scenario.Id, i + 1, pair.Key, "after"));
                stepResults.Add(result);
                if (!result.Ok)
                {
                    await TryCaptureScreenshotAsync(
                        page,
                        ResolveScreenshotPath(scenario.Id, "failure"));
                    return new ScenarioRunResult(scenario.Id, false, stepResults, result.Error);
                }
            }
            catch (Exception ex)
            {
                stepResults.Add(new StepResult(i + 1, pair.Key, false, null, ex.Message));
                await TryCaptureScreenshotAsync(
                    page,
                    ResolveScreenshotPath(scenario.Id, "failure"));
                return new ScenarioRunResult(scenario.Id, false, stepResults, ex.Message);
            }
        }

        return new ScenarioRunResult(scenario.Id, true, stepResults, null);
    }

    private async Task<StepResult> ExecuteStepAsync(
        IPage page,
        string baseUrl,
        UiScenario scenario,
        string kind,
        object value,
        int index,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (kind)
        {
            case "goto":
                string path = ResolveEnv(scenario, value.ToString() ?? "");
                await page.GotoAsync(ToAbsoluteUrl(baseUrl, path), new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                await WaitForBlazorAsync(page);
                await WaitForAppShellAsync(page);
                await WaitForBusyOverlayAsync(page);
                return new StepResult(index, kind, true, path, null);

            case "fill":
                var fields = ToStringDictionary(value);
                foreach (KeyValuePair<string, string> field in fields)
                {
                    string text = ResolveEnv(scenario, field.Value);
                    ILocator input = await LocateHookAsync(page, field.Key);
                    if (string.Equals(field.Key, "person-visa-application-family-members-text", StringComparison.Ordinal))
                    {
                        await FillVisaFamilyMembersTextAsync(input, text);
                    }
                    else
                    {
                        await FillHookValueAsync(page, input, text);
                    }
                }

                return new StepResult(index, kind, true, string.Join(", ", fields.Keys), null);

            case "click":
            case "select-tab":
                string hookId = value.ToString() ?? "";
                if (kind == "click" && string.Equals(hookId, "login-submit", StringComparison.Ordinal))
                {
                    await WaitForBusyOverlayAsync(page);
                    ILocator submit = await LocateHookAsync(page, hookId);
                    await submit.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.Load);
                    await WaitForBlazorAsync(page);
                    await WaitForAppShellAsync(page);
                    await WaitForBusyOverlayAsync(page);
                    return new StepResult(index, kind, true, hookId, null);
                }

                ILocator target = await LocateHookAsync(page, hookId);
                if (kind == "click"
                    && (string.Equals(hookId, "person-detail-employee-save", StringComparison.Ordinal)
                        || string.Equals(hookId, "person-detail-employee-save-and-close", StringComparison.Ordinal)))
                {
                    await TryCaptureScreenshotAsync(
                        page,
                        ResolveScreenshotPath(scenario.Id, "before-save"));
                    await target.ClickAsync();
                    await WaitForBlazorAsync(page);
                    if (_options.PauseAfterSaveMs > 0)
                    {
                        await page.WaitForTimeoutAsync(_options.PauseAfterSaveMs);
                    }

                    await TryCaptureScreenshotAsync(
                        page,
                        ResolveScreenshotPath(scenario.Id, "after-save"));
                    return new StepResult(index, kind, true, hookId, null);
                }

                await WaitForBusyOverlayAsync(page);
                await target.ClickAsync();
                await WaitForBusyOverlayAsync(page);
                await WaitForBlazorAsync(page);
                if (hookId.EndsWith("-new", StringComparison.Ordinal))
                {
                    await page.WaitForTimeoutAsync(1500);
                    await WaitForBusyOverlayAsync(page);
                }

                return new StepResult(index, kind, true, hookId, null);

            case "login":
                var creds = ToStringDictionary(value);
                string user = creds.GetValueOrDefault("user", _options.DefaultUser);
                string pass = creds.GetValueOrDefault("password", _options.DefaultPassword);
                await page.GotoAsync(ToAbsoluteUrl(baseUrl, "/LoginPage"), new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                await WaitForBlazorAsync(page);
                await (await LocateHookAsync(page, "login-user-name")).FillAsync(user);
                await (await LocateHookAsync(page, "login-password")).FillAsync(pass);
                await (await LocateHookAsync(page, "login-submit")).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.Load);
                await WaitForBlazorAsync(page);
                await WaitForAppShellAsync(page);
                return new StepResult(index, kind, true, user, null);

            case "wait-for":
            case "assert-visible":
                hookId = value.ToString() ?? "";
                await LocateHookAsync(page, hookId);
                return new StepResult(index, kind, true, hookId, null);

            case "select-listbox-item":
                string itemText = ResolveEnv(scenario, value.ToString() ?? "");
                await SelectDevExpressListboxItemAsync(page, itemText);
                await page.WaitForLoadStateAsync(LoadState.Load);
                await WaitForBlazorAsync(page);
                await WaitForBusyOverlayAsync(page);
                return new StepResult(index, kind, true, itemText, null);

            default:
                return new StepResult(index, kind, false, null, $"Unknown step kind '{kind}'.");
        }
    }

    private async Task<ILocator> LocateHookAsync(IPage page, string hookId)
    {
        IReadOnlyList<string> selectors = _hooks.GetSelectors(hookId);
        int perSelectorMs = _options.TimeoutMs;

        foreach (string selector in selectors)
        {
            ILocator locator = page.Locator(selector);
            if (await locator.CountAsync() == 0)
            {
                continue;
            }

            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = perSelectorMs,
                });
                await locator.First.ScrollIntoViewIfNeededAsync();
                return locator.First;
            }
            catch (TimeoutException)
            {
                // try next selector
            }
        }

        throw new InvalidOperationException(
            $"Hook '{hookId}' not visible. Tried: {string.Join(", ", selectors)}");
    }

    private static string ResolveEnv(UiScenario scenario, string value)
    {
        if (!value.StartsWith("${", StringComparison.Ordinal) || !value.EndsWith('}'))
        {
            return value;
        }

        string key = value[2..^1];
        if (scenario.Env != null && scenario.Env.TryGetValue(key, out string? envValue))
        {
            return envValue;
        }

        throw new InvalidOperationException($"Missing env key '{key}' for substitution {value}.");
    }

    private static Dictionary<string, string> ToStringDictionary(object value)
    {
        if (value is Dictionary<object, object> objMap)
        {
            return objMap.ToDictionary(
                static p => p.Key.ToString() ?? "",
                static p => p.Value?.ToString() ?? "");
        }

        if (value is Dictionary<string, object> strObjMap)
        {
            return strObjMap.ToDictionary(static p => p.Key, static p => p.Value?.ToString() ?? "");
        }

        if (value is Dictionary<string, string> strMap)
        {
            return strMap;
        }

        throw new InvalidOperationException("Expected a mapping for step value.");
    }

    private static string ToAbsoluteUrl(string baseUrl, string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUrl;
        }

        string baseNormalized = baseUrl.TrimEnd('/');
        return pathOrUrl.StartsWith('/') ? baseNormalized + pathOrUrl : baseNormalized + "/" + pathOrUrl;
    }

    private static async Task WaitForBlazorAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForTimeoutAsync(800);
    }

    private static async Task WaitForBusyOverlayAsync(IPage page)
    {
        ILocator busy = page.Locator(".dxbl-loading-panel, .dx-loadingpanel");
        if (await busy.CountAsync() == 0)
        {
            return;
        }

        try
        {
            await busy.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000,
            });
        }
        catch (TimeoutException)
        {
            // Overlay class may differ; continue after one Blazor beat.
        }

        await WaitForBlazorAsync(page);
    }

    private static async Task WaitForAppShellAsync(IPage page)
    {
        ILocator shell = page.Locator("[data-testid='nav-people'], .xaf-navigation");
        try
        {
            await shell.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 15_000,
            });
        }
        catch (TimeoutException)
        {
            // Shell hooks may apply after first paint — one more Blazor beat.
            await WaitForBlazorAsync(page);
        }
    }

    private static async Task FillHookValueAsync(IPage page, ILocator locator, string text)
    {
        await locator.ScrollIntoViewIfNeededAsync();
        string tagName = await locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        if (tagName is "dxbl-combo-box" or "dxbl-lookup")
        {
            await FillDevExpressComboAsync(page, locator, text);
            return;
        }

        await locator.FillAsync(text);
    }

    private static async Task FillVisaFamilyMembersTextAsync(ILocator root, string text)
    {
        string tagName = await root.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        ILocator summary = tagName == "input"
            ? root
            : root.Locator("input").First;
        await summary.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        string current = await summary.InputValueAsync();
        if (VisaFamilyTextMatches(current, text))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Visa family manual editor is read-only inline; expected display '{text}' but found '{current}'. "
            + "Popup line editing is not automated yet — use default Ýok on new employees.");
    }

    private static bool VisaFamilyTextMatches(string? current, string? expected) =>
        string.Equals(current?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase)
        || (IsVisaFamilyNoneValue(current) && IsVisaFamilyNoneValue(expected));

    private static bool IsVisaFamilyNoneValue(string? text) =>
        string.Equals(text?.Trim(), "Ýok", StringComparison.OrdinalIgnoreCase)
        || string.Equals(text?.Trim(), "Yok", StringComparison.OrdinalIgnoreCase);

    private static async Task FillDevExpressComboAsync(IPage page, ILocator combo, string text)
    {
        await combo.ClickAsync();
        ILocator input = combo.Locator("input");
        await input.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await input.First.ClickAsync();
        await input.First.FillAsync(string.Empty);
        await input.First.PressSequentiallyAsync(text, new LocatorPressSequentiallyOptions { Delay = 50 });
        await page.WaitForTimeoutAsync(500);
        try
        {
            await SelectDevExpressListboxItemAsync(page, text);
        }
        catch (InvalidOperationException)
        {
            await input.First.PressAsync("Enter");
        }

        await page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Clicks a visible DevExpress dropdown row (combo listbox, toolbar SingleChoice menu, language switcher).
    /// </summary>
    private static async Task SelectDevExpressListboxItemAsync(IPage page, string text)
    {
        await WaitForDevExpressDropdownAsync(page);

        ILocator[] itemSets =
        [
            page.Locator(".dxbl-listbox-item"),
            page.GetByRole(AriaRole.Menuitem),
            page.Locator(".dxbl-dropdown-body button"),
            page.Locator(".dxbl-dropdown-body [role='menuitem']"),
        ];

        foreach (ILocator items in itemSets)
        {
            if (await TryClickDropdownItemAsync(items, text))
            {
                return;
            }
        }

        ILocator popupText = page.Locator(".dxbl-dropdown-body, .dxbl-popup, [role='menu']");
        if (await popupText.CountAsync() > 0)
        {
            ILocator exactInPopup = popupText.GetByText(text, new LocatorGetByTextOptions { Exact = true });
            if (await exactInPopup.CountAsync() > 0)
            {
                await exactInPopup.First.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException(
            $"No dropdown item matching '{text}'. Visible labels: [{await CollectVisibleDropdownLabelsAsync(page)}]");
    }

    private static async Task WaitForDevExpressDropdownAsync(IPage page)
    {
        ILocator popup = page.Locator(
            ".dxbl-listbox-item, .dxbl-dropdown-body, .dxbl-popup, [role='menu']");
        await popup.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000,
        });
    }

    private static async Task<bool> TryClickDropdownItemAsync(ILocator items, string text)
    {
        if (await items.CountAsync() == 0)
        {
            return false;
        }

        ILocator exact = items.GetByText(text, new LocatorGetByTextOptions { Exact = true });
        if (await exact.CountAsync() > 0)
        {
            await exact.First.ClickAsync();
            return true;
        }

        int count = await items.CountAsync();
        string needle = text.Trim();
        for (int i = 0; i < count; i++)
        {
            ILocator item = items.Nth(i);
            if (!await item.IsVisibleAsync())
            {
                continue;
            }

            string? itemText = (await item.InnerTextAsync())?.Trim();
            if (string.IsNullOrEmpty(itemText))
            {
                continue;
            }

            if (itemText.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || needle.Contains(itemText, StringComparison.OrdinalIgnoreCase))
            {
                await item.ClickAsync();
                return true;
            }
        }

        return false;
    }

    private static async Task<string> CollectVisibleDropdownLabelsAsync(IPage page)
    {
        ILocator[] itemSets =
        [
            page.Locator(".dxbl-listbox-item"),
            page.GetByRole(AriaRole.Menuitem),
            page.Locator(".dxbl-dropdown-body button"),
        ];

        var labels = new List<string>();
        foreach (ILocator items in itemSets)
        {
            int count = await items.CountAsync();
            for (int i = 0; i < count; i++)
            {
                if (!await items.Nth(i).IsVisibleAsync())
                {
                    continue;
                }

                string? label = (await items.Nth(i).InnerTextAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(label) && !labels.Contains(label, StringComparer.OrdinalIgnoreCase))
                {
                    labels.Add(label);
                }
            }
        }

        return labels.Count == 0 ? "(none)" : string.Join(", ", labels);
    }

    private string? ResolveScreenshotPath(string scenarioId, string suffix) =>
        string.IsNullOrWhiteSpace(_options.ScreenshotDir)
            ? null
            : Path.Combine(_options.ScreenshotDir, $"{scenarioId}-{suffix}.png");

    private string? ResolveStepScreenshotPath(string scenarioId, int stepIndex, string stepKind, string phase) =>
        string.IsNullOrWhiteSpace(_options.ScreenshotDir) || !_options.ScreenshotEachStep
            ? null
            : Path.Combine(_options.ScreenshotDir, $"{scenarioId}-step-{stepIndex:D2}-{stepKind}-{phase}.png");

    private static (int Width, int Height) GetPrimaryScreenSize()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("VISA2026_SCENARIO_SCREEN");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            string[] parts = fromEnv.Split('x', 'X');
            if (parts.Length == 2
                && int.TryParse(parts[0].Trim(), out int width)
                && int.TryParse(parts[1].Trim(), out int height)
                && width > 0
                && height > 0)
            {
                return (width, height);
            }
        }

        return (1920, 1080);
    }

    private static async Task MaximizeHeadedWindowAsync(IPage page)
    {
        try
        {
            ICDPSession cdp = await page.Context.NewCDPSessionAsync(page);
            JsonElement? windowForTarget = await cdp.SendAsync("Browser.getWindowForTarget");
            if (windowForTarget == null || windowForTarget.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Browser.getWindowForTarget returned no window.");
            }

            int windowId = windowForTarget.Value.GetProperty("windowId").GetInt32();
            await cdp.SendAsync("Browser.setWindowBounds", new Dictionary<string, object>
            {
                ["windowId"] = windowId,
                ["bounds"] = new Dictionary<string, object> { ["windowState"] = "maximized" },
            });
        }
        catch
        {
            await page.EvaluateAsync(@"() => {
                window.moveTo(0, 0);
                window.resizeTo(screen.availWidth, screen.availHeight);
            }");
        }
    }

    private static async Task TryCaptureScreenshotAsync(IPage page, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fullPath,
            FullPage = true,
        });
        Console.WriteLine($"Screenshot saved: {fullPath}");
    }

    private static ScenarioRunResult Fail(string scenarioId, List<StepResult> steps, string error) =>
        new(scenarioId, false, steps, error);
}
