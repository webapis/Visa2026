using Microsoft.Playwright;

namespace Visa2026.Tools.VerifyUiTestHooks;

public sealed class HookVerifier
{
    private readonly VerifyOptions _options;
    private readonly HookManifest _manifest;

    public HookVerifier(HookManifest manifest, VerifyOptions options)
    {
        _manifest = manifest;
        _options = options;
    }

    public async Task<IReadOnlyList<ScenarioResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ScenarioResult>();
        var scenarios = ResolveScenarios();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless,
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(_options.TimeoutMs);

        bool loggedIn = false;

        foreach (HookScenario scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryResolveStartUrl(scenario, out string? startUrl, out string? skipReason))
            {
                results.Add(new ScenarioResult(scenario.Id, true, skipReason, []));
                continue;
            }

            if (scenario.RequiresAuth && !loggedIn)
            {
                await LoginAsync(page, cancellationToken);
                loggedIn = true;
            }

            await page.GotoAsync(startUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitForBlazorAsync(page);

            var checkResults = new List<CheckResult>();
            foreach (HookCheck check in scenario.Checks)
            {
                checkResults.Add(await VerifyCheckAsync(page, scenario.Id, check, cancellationToken));
            }

            results.Add(new ScenarioResult(scenario.Id, false, null, checkResults));
        }

        return results;
    }

    private IReadOnlyList<HookScenario> ResolveScenarios()
    {
        if (_options.ScenarioIds.Count == 0)
        {
            return _manifest.Scenarios;
        }

        var set = new HashSet<string>(_options.ScenarioIds, StringComparer.OrdinalIgnoreCase);
        return _manifest.Scenarios.Where(s => set.Contains(s.Id)).ToList();
    }

    private bool TryResolveStartUrl(HookScenario scenario, out string? url, out string? skipReason)
    {
        if (scenario.RequiresAuth)
        {
            string? path = _options.StartUrl;
            if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(scenario.StartPathEnv))
            {
                path = Environment.GetEnvironmentVariable(scenario.StartPathEnv);
            }

            path ??= scenario.StartPath;

            if (string.IsNullOrWhiteSpace(path))
            {
                url = null;
                skipReason =
                    $"Scenario '{scenario.Id}' needs --start-url, scenario startPath, or env {scenario.StartPathEnv ?? "VISA2026_HOOK_VERIFY_PERSON_URL"} " +
                    "(e.g. /Person_DetailView_Employee/{{guid}}).";
                return false;
            }

            url = ToAbsoluteUrl(path);
            skipReason = null;
            return true;
        }

        url = ToAbsoluteUrl(scenario.StartPath ?? "/LoginPage");
        skipReason = null;
        return true;
    }

    private string ToAbsoluteUrl(string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUrl;
        }

        string baseUrl = _options.BaseUrl.TrimEnd('/');
        return pathOrUrl.StartsWith('/') ? baseUrl + pathOrUrl : baseUrl + "/" + pathOrUrl;
    }

    private async Task LoginAsync(IPage page, CancellationToken cancellationToken)
    {
        await page.GotoAsync(ToAbsoluteUrl("/LoginPage"), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForBlazorAsync(page);

        var user = page.Locator("#login-user-name");
        await user.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await user.FillAsync(_options.UserName);

        if (!string.IsNullOrEmpty(_options.Password))
        {
            await page.Locator("#login-password").FillAsync(_options.Password);
        }

        await page.Locator("[data-testid=\"login-submit\"]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForBlazorAsync(page);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task WaitForBlazorAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForTimeoutAsync(1500);
    }

    private async Task<CheckResult> VerifyCheckAsync(IPage page, string scenarioId, HookCheck check, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? winningSelector = null;
        ILocator? locator = null;

        foreach (string selector in check.Selectors)
        {
            ILocator candidate = page.Locator(selector);
            if (await candidate.CountAsync() > 0)
            {
                locator = candidate.First;
                winningSelector = selector;
                break;
            }
        }

        if (locator == null || winningSelector == null)
        {
            return new CheckResult(
                scenarioId,
                check.Id,
                check.Target,
                null,
                AccessOk: false,
                BehaviorOk: false,
                Error: "Access failed: no selector matched (note: DevExpress accordion nav uses open shadow roots — locators pierce shadow DOM; document.querySelector does not).");
        }

        try
        {
            bool behaviorOk = check.Type switch
            {
                "exists" => true,
                "text-input" or "password-input" => await VerifyTextInputBehaviorAsync(locator),
                "button" => check.ClickOnBehavior
                    ? await VerifyClickBehaviorAsync(locator)
                    : await locator.IsVisibleAsync(),
                "layout-tab" => check.ClickOnBehavior
                    ? await VerifyClickBehaviorAsync(locator)
                    : await locator.IsVisibleAsync(),
                "combo" => check.ClickOnBehavior
                    ? await VerifyComboBehaviorAsync(page, locator)
                    : await VerifyComboExistsAsync(locator),
                _ => await locator.IsVisibleAsync(),
            };

            return new CheckResult(
                scenarioId,
                check.Id,
                check.Target,
                winningSelector,
                AccessOk: true,
                BehaviorOk: behaviorOk,
                Error: behaviorOk ? null : "Behavior check failed.");
        }
        catch (Exception ex)
        {
            return new CheckResult(
                scenarioId,
                check.Id,
                check.Target,
                winningSelector,
                AccessOk: true,
                BehaviorOk: false,
                Error: ex.Message);
        }
    }

    private static async Task<bool> VerifyTextInputBehaviorAsync(ILocator locator)
    {
        await locator.FocusAsync();
        const string probe = "__hook_verify__";
        await locator.FillAsync(probe);
        string value = await locator.InputValueAsync();
        return value == probe;
    }

    private static async Task<bool> VerifyClickBehaviorAsync(ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
        {
            return false;
        }

        await locator.ClickAsync();
        return true;
    }

    private static async Task<bool> VerifyComboExistsAsync(ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
        {
            return false;
        }

        ILocator input = locator.Locator("input").First;
        return await input.CountAsync() > 0 && await input.IsVisibleAsync();
    }

    private static async Task<bool> VerifyComboBehaviorAsync(IPage page, ILocator locator)
    {
        if (!await VerifyComboExistsAsync(locator))
        {
            return false;
        }

        ILocator input = locator.Locator("input").First;
        await input.ClickAsync();
        await page.WaitForTimeoutAsync(400);
        ILocator listItem = page.Locator(".dxbl-listbox-item");
        return await listItem.CountAsync() >= 2;
    }
}
