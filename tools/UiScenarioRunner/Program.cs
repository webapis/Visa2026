using Microsoft.Playwright;
using Visa2026.Tools.UiScenarioRunner;

static int PrintUsage()
{
    Console.WriteLine("""
Visa2026 UI scenario runner (Playwright + YAML from tools/UiScenarioRunner/scenarios/).

Usage:
  dotnet run --project tools/UiScenarioRunner -- --scenario <id> [options]

Options:
  --scenario <id>      Required — loads scenarios/<id>.yaml
  --base-url <url>     Default: https://localhost:5001
  --user <name>        Default login user (default: Admin)
  --password <pwd>     Default login password (default: empty)
  --headed             Show browser window
  --timeout <ms>       Per-action timeout (default: 30000)
  --manifest <path>    hooks-manifest.json (default: tools/VerifyUiTestHooks/hooks-manifest.json)

Setup (once):
  dotnet build tools/UiScenarioRunner/UiScenarioRunner.csproj -c Debug
  pwsh tools/UiScenarioRunner/bin/Debug/net8.0/playwright.ps1 install chromium

Example:
  dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke
""");
    return 1;
}

string? scenarioId = null;
string baseUrl = "https://localhost:5001";
string userName = "Admin";
string password = "";
bool headless = true;
int timeoutMs = 30_000;
string manifestPath = RepoPaths.DefaultManifestPath();

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    switch (arg)
    {
        case "--help":
        case "-h":
            return PrintUsage();
        case "--scenario" when i + 1 < args.Length:
            scenarioId = args[++i];
            break;
        case "--base-url" when i + 1 < args.Length:
            baseUrl = args[++i];
            break;
        case "--user" when i + 1 < args.Length:
            userName = args[++i];
            break;
        case "--password" when i + 1 < args.Length:
            password = args[++i];
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

if (string.IsNullOrWhiteSpace(scenarioId))
{
    Console.Error.WriteLine("--scenario is required.");
    return PrintUsage();
}

string scenarioPath = RepoPaths.ScenarioYamlPath(scenarioId);
if (!File.Exists(scenarioPath))
{
    Console.Error.WriteLine($"Scenario not found: {scenarioPath}");
    return 2;
}

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {manifestPath}");
    return 2;
}

UiScenario scenario;
try
{
    scenario = UiScenario.LoadYaml(scenarioPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load scenario: {ex.Message}");
    return 2;
}

if (!string.Equals(scenario.Id, scenarioId, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Scenario file id '{scenario.Id}' does not match --scenario '{scenarioId}'.");
    return 2;
}

var hooks = HookResolver.Load(manifestPath);
var options = new RunOptions(baseUrl, userName, password, headless, timeoutMs, manifestPath);
var runner = new ScenarioRunner(hooks, options);

Console.WriteLine($"Scenario: {scenario.Id} — {scenario.Description}");
Console.WriteLine($"YAML: {scenarioPath}");
Console.WriteLine($"Base URL: {baseUrl}");
Console.WriteLine();

ScenarioRunResult result;
try
{
    result = await runner.RunAsync(scenario);
}
catch (PlaywrightException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Install Chromium: pwsh tools/UiScenarioRunner/bin/Debug/net8.0/playwright.ps1 install chromium");
    return 3;
}

foreach (StepResult step in result.Steps)
{
    string status = step.Ok ? "PASS" : "FAIL";
    Console.WriteLine($"  [{status}] step {step.Index} {step.StepKind} {step.Detail ?? ""}".Trim());
    if (step.Error != null)
    {
        Console.WriteLine($"         {step.Error}");
    }
}

Console.WriteLine();
if (result.Ok)
{
    Console.WriteLine($"Scenario '{result.ScenarioId}' passed.");
    return 0;
}

Console.Error.WriteLine($"Scenario '{result.ScenarioId}' failed: {result.Error}");
return 4;
