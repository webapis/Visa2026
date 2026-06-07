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
            string? path = _options.StartUrl
                ?? Environment.GetEnvironmentVariable(scenario.StartPathEnv ?? "VISA2026_HOOK_VERIFY_PERSON_URL");

            if (string.IsNullOrWhiteSpace(path))
            {
                url = null;
                skipReason =
                    $"Scenario '{scenario.Id}' needs --start-url or env {scenario.StartPathEnv ?? "VISA2026_HOOK_VERIFY_PERSON_URL"} " +
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
        IElementHandle? element = null;

        foreach (string selector in check.Selectors)
        {
            element = await page.QuerySelectorAsync(selector);
            if (element != null)
            {
                winningSelector = selector;
                break;
            }
        }

        if (element == null || winningSelector == null)
        {
            return new CheckResult(
                scenarioId,
                check.Id,
                check.Target,
                null,
                AccessOk: false,
                BehaviorOk: false,
                Error: "Access failed: no selector matched.");
        }

        try
        {
            bool behaviorOk = check.Type switch
            {
                "exists" => true,
                "text-input" or "password-input" => await VerifyTextInputBehaviorAsync(element),
                "button" => check.ClickOnBehavior
                    ? await VerifyClickBehaviorAsync(element)
                    : await element.IsVisibleAsync(),
                "layout-tab" => check.ClickOnBehavior
                    ? await VerifyClickBehaviorAsync(element)
                    : await element.IsVisibleAsync(),
                _ => await element.IsVisibleAsync(),
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

    private static async Task<bool> VerifyTextInputBehaviorAsync(IElementHandle element)
    {
        await element.FocusAsync();
        const string probe = "__hook_verify__";
        await element.FillAsync(probe);
        string value = await element.InputValueAsync();
        return value == probe;
    }

    private static async Task<bool> VerifyClickBehaviorAsync(IElementHandle element)
    {
        if (!await element.IsVisibleAsync())
        {
            return false;
        }

        await element.ClickAsync();
        return true;
    }
}
