using Microsoft.Playwright;
using Visa2026.Tools.VerifyUiTestHooks;

static int PrintUsage()
{
    Console.WriteLine("""
Visa2026 UI test hook verifier (Playwright headless Chromium).

Runs the same access + behavior checks as DevTools console, driven by hooks-manifest.json.
Does not update docs/UI_TEST_HOOKS.md — promote rows manually after a green run.

Usage:
  dotnet run --project tools/VerifyUiTestHooks -- [options]

Options:
  --base-url <url>     App base URL (default: https://localhost:5001)
  --user <name>        Logon user when a scenario requires auth (default: Admin)
  --password <pwd>     Logon password (default: empty)
  --start-url <path>   Path or URL for authenticated scenarios (Person detail)
  --scenario <id>      Repeatable; default: all scenarios in manifest
  --headed             Show browser window
  --timeout <ms>       Per-action timeout (default: 30000)
  --manifest <path>    hooks-manifest.json path

First-time setup (Chromium):
  cd tools/VerifyUiTestHooks
  dotnet build
  pwsh bin/Debug/net8.0/playwright.ps1 install chromium

Examples:
  dotnet run --project tools/VerifyUiTestHooks -- --scenario login
  dotnet run --project tools/VerifyUiTestHooks -- --scenario person-employee-tabs ^
    --start-url /Person_DetailView_Employee/00000000-0000-0000-0000-000000000001
""");
    return 1;
}

string baseUrl = "https://localhost:5001";
string userName = "Admin";
string password = "";
string? startUrl = null;
bool headless = true;
int timeoutMs = 30_000;
string manifestPath = Path.Combine(AppContext.BaseDirectory, "hooks-manifest.json");
var scenarioIds = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    switch (arg)
    {
        case "--help":
        case "-h":
            return PrintUsage();
        case "--base-url" when i + 1 < args.Length:
            baseUrl = args[++i];
            break;
        case "--user" when i + 1 < args.Length:
            userName = args[++i];
            break;
        case "--password" when i + 1 < args.Length:
            password = args[++i];
            break;
        case "--start-url" when i + 1 < args.Length:
            startUrl = args[++i];
            break;
        case "--scenario" when i + 1 < args.Length:
            scenarioIds.Add(args[++i]);
            break;
        case "--headed":
            headless = false;
            break;
        case "--timeout" when i + 1 < args.Length && int.TryParse(args[++i], out int t):
            timeoutMs = t;
            break;
        case "--manifest" when i + 1 < args.Length:
            manifestPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {arg}");
            return PrintUsage();
    }
}

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {manifestPath}");
    return 2;
}

var manifest = HookManifest.Load(manifestPath);
var options = new VerifyOptions(
    baseUrl,
    userName,
    password,
    headless,
    timeoutMs,
    startUrl,
    scenarioIds);

Console.WriteLine($"Base URL: {baseUrl}");
Console.WriteLine($"Manifest: {manifestPath}");
Console.WriteLine($"Scenarios: {(scenarioIds.Count == 0 ? "all" : string.Join(", ", scenarioIds))}");
Console.WriteLine();

var verifier = new HookVerifier(manifest, options);
IReadOnlyList<ScenarioResult> results;
try
{
    results = await verifier.RunAsync();
}
catch (PlaywrightException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("If Chromium is missing, run:");
    Console.Error.WriteLine("  pwsh tools/VerifyUiTestHooks/bin/Debug/net8.0/playwright.ps1 install chromium");
    return 3;
}

int failures = 0;

foreach (ScenarioResult scenario in results)
{
    if (scenario.Skipped)
    {
        Console.WriteLine($"[SKIP] {scenario.ScenarioId}: {scenario.SkipReason}");
        failures++;
        continue;
    }

    Console.WriteLine($"Scenario: {scenario.ScenarioId}");
    foreach (CheckResult check in scenario.Checks)
    {
        string access = check.AccessOk ? "access OK" : "access FAIL";
        string behavior = check.BehaviorOk ? "behavior OK" : "behavior FAIL";
        string selector = check.WinningSelector ?? "(none)";
        Console.WriteLine($"  {(check.AccessOk && check.BehaviorOk ? "PASS" : "FAIL")} {check.CheckId} ({check.Target})");
        Console.WriteLine($"         selector: {selector} — {access}, {behavior}");
        if (check.Error != null)
        {
            Console.WriteLine($"         {check.Error}");
        }

        if (!check.AccessOk || !check.BehaviorOk)
        {
            failures++;
        }
    }

    Console.WriteLine();
}

if (failures == 0)
{
    Console.WriteLine("All checks passed. Update docs/UI_TEST_HOOKS.md and registry.md if promoting new hooks.");
    return 0;
}

Console.WriteLine($"{failures} check(s) failed or skipped.");
return failures > 0 ? 4 : 0;
