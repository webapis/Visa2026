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
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless,
        });

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(_options.TimeoutMs);

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
                StepResult result = await ExecuteStepAsync(page, baseUrl, scenario, pair.Key, pair.Value, i + 1, cancellationToken);
                stepResults.Add(result);
                if (!result.Ok)
                {
                    return new ScenarioRunResult(scenario.Id, false, stepResults, result.Error);
                }
            }
            catch (Exception ex)
            {
                stepResults.Add(new StepResult(i + 1, pair.Key, false, null, ex.Message));
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
                await page.GotoAsync(ToAbsoluteUrl(baseUrl, path), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await WaitForBlazorAsync(page);
                return new StepResult(index, kind, true, path, null);

            case "fill":
                var fields = ToStringDictionary(value);
                foreach (KeyValuePair<string, string> field in fields)
                {
                    string text = ResolveEnv(scenario, field.Value);
                    ILocator input = await LocateHookAsync(page, field.Key);
                    await input.FillAsync(text);
                }

                return new StepResult(index, kind, true, string.Join(", ", fields.Keys), null);

            case "click":
            case "select-tab":
                string hookId = value.ToString() ?? "";
                ILocator target = await LocateHookAsync(page, hookId);
                await target.ClickAsync();
                await WaitForBlazorAsync(page);
                return new StepResult(index, kind, true, hookId, null);

            case "login":
                var creds = ToStringDictionary(value);
                string user = creds.GetValueOrDefault("user", _options.DefaultUser);
                string pass = creds.GetValueOrDefault("password", _options.DefaultPassword);
                await page.GotoAsync(ToAbsoluteUrl(baseUrl, "/LoginPage"), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await WaitForBlazorAsync(page);
                await (await LocateHookAsync(page, "login-user-name")).FillAsync(user);
                await (await LocateHookAsync(page, "login-password")).FillAsync(pass);
                await (await LocateHookAsync(page, "login-submit")).ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await WaitForBlazorAsync(page);
                return new StepResult(index, kind, true, user, null);

            case "wait-for":
            case "assert-visible":
                hookId = value.ToString() ?? "";
                await LocateHookAsync(page, hookId);
                return new StepResult(index, kind, true, hookId, null);

            default:
                return new StepResult(index, kind, false, null, $"Unknown step kind '{kind}'.");
        }
    }

    private async Task<ILocator> LocateHookAsync(IPage page, string hookId)
    {
        IReadOnlyList<string> selectors = _hooks.GetSelectors(hookId);
        foreach (string selector in selectors)
        {
            ILocator locator = page.Locator(selector);
            try
            {
                await locator.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = _options.TimeoutMs,
                });
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
        await page.WaitForTimeoutAsync(1500);
    }

    private static ScenarioRunResult Fail(string scenarioId, List<StepResult> steps, string error) =>
        new(scenarioId, false, steps, error);
}
