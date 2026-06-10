using Microsoft.Playwright;
using Visa2026.Tools.UiScenarioRunner;

static int PrintUsage()
{
    Console.WriteLine("""
Visa2026 UI scenario runner (Playwright + YAML from tools/UiScenarioRunner/scenarios/).

Usage:
  dotnet run --project tools/UiScenarioRunner -- --scenario <id> [options]
  dotnet run --project tools/UiScenarioRunner -- --all [options]

Options:
  --scenario <id>      Run one scenario (loads scenarios/<id>.yaml)
  --all                  Run every *.yaml in tools/UiScenarioRunner/scenarios/ (skips *-staging.yaml)
  --base-url <url>     Default: https://localhost:5001
  --user <name>        Default login user (default: Admin)
  --password <pwd>     Default login password (default: empty)
  --headed             Show browser window (maximized; full screen width via CDP)
  --slow-mo <ms>       Delay between Playwright actions (default: 500 when flag used alone)
  --screenshot-dir <dir>  Screenshot folder (save milestones + optional step captures)
  --screenshot-steps      Before/after PNG for each YAML step (requires --screenshot-dir)
  --pause-after-save <ms>  Wait after Save before after-save screenshot (default: 5000 when --screenshot-dir set)
  --trace-dir <dir>    Save Playwright trace zip on failure (one file per scenario)
  --junit-report <path>  Write JUnit XML (CI / GitHub Checks)
  --json-report <path>   Write machine-readable JSON summary
  --html-report <path>   Write HTML report (GitHub Pages bundle)
  --timeout <ms>       Per-action timeout (default: 30000)
  --manifest <path>    hooks-manifest.json (default: tools/VerifyUiTestHooks/hooks-manifest.json)

Setup (once):
  dotnet build tools/UiScenarioRunner/UiScenarioRunner.csproj -c Debug
  pwsh tools/UiScenarioRunner/bin/Debug/net8.0/playwright.ps1 install chromium

Example:
  dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke
  dotnet run --project tools/UiScenarioRunner -- --all --base-url http://localhost:5000 `
    --junit-report artifacts/ui-scenario-report/results.junit.xml `
    --html-report artifacts/ui-scenario-report/index.html
""");
    return 1;
}

string? scenarioId = null;
bool runAll = false;
string baseUrl = "https://localhost:5001";
string userName = "Admin";
string password = "";
bool headless = true;
int timeoutMs = 30_000;
int slowMoMs = 0;
string? screenshotDir = null;
bool screenshotEachStep = false;
int pauseAfterSaveMs = 0;
string? traceDir = null;
string? junitReportPath = null;
string? jsonReportPath = null;
string? htmlReportPath = null;
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
        case "--all":
            runAll = true;
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
        case "--slow-mo":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int slowMo))
            {
                i++;
                slowMoMs = slowMo;
            }
            else
            {
                slowMoMs = 500;
            }

            break;
        case "--screenshot-dir" when i + 1 < args.Length:
            screenshotDir = args[++i];
            if (pauseAfterSaveMs == 0)
            {
                pauseAfterSaveMs = 5_000;
            }

            break;
        case "--screenshot-steps":
            screenshotEachStep = true;
            break;
        case "--pause-after-save" when i + 1 < args.Length && int.TryParse(args[++i], out int pauseMs):
            pauseAfterSaveMs = pauseMs;
            break;
        case "--trace-dir" when i + 1 < args.Length:
            traceDir = args[++i];
            break;
        case "--junit-report" when i + 1 < args.Length:
            junitReportPath = args[++i];
            break;
        case "--json-report" when i + 1 < args.Length:
            jsonReportPath = args[++i];
            break;
        case "--html-report" when i + 1 < args.Length:
            htmlReportPath = args[++i];
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

if (runAll && !string.IsNullOrWhiteSpace(scenarioId))
{
    Console.Error.WriteLine("Use either --scenario or --all, not both.");
    return PrintUsage();
}

if (!runAll && string.IsNullOrWhiteSpace(scenarioId))
{
    Console.Error.WriteLine("--scenario or --all is required.");
    return PrintUsage();
}

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {manifestPath}");
    return 2;
}

IReadOnlyList<string> scenarioIds = runAll
    ? RepoPaths.ListScenarioIds()
    : [scenarioId!];

if (scenarioIds.Count == 0)
{
    Console.Error.WriteLine("No scenarios found in tools/UiScenarioRunner/scenarios/.");
    return 2;
}

var hooks = HookResolver.Load(manifestPath);
var options = new RunOptions(
    baseUrl, userName, password, headless, timeoutMs, slowMoMs, screenshotDir, screenshotEachStep, pauseAfterSaveMs, traceDir, manifestPath);
var runner = new ScenarioRunner(hooks, options);

DateTimeOffset startedAt = DateTimeOffset.UtcNow;
var allResults = new List<ScenarioRunResult>();
int exitCode = 0;

foreach (string id in scenarioIds)
{
    (int resultCode, ScenarioRunResult? scenarioResult) = await RunOneScenarioAsync(runner, id, baseUrl);
    if (scenarioResult != null)
    {
        allResults.Add(scenarioResult);
    }

    if (resultCode != 0)
    {
        exitCode = resultCode;
    }

    if (scenarioIds.Count > 1)
    {
        Console.WriteLine();
    }
}

RunReportMetadata metadata = RunReportMetadata.Create(baseUrl, startedAt);
WriteReports(junitReportPath, jsonReportPath, htmlReportPath, allResults, metadata);

if (exitCode == 0 && scenarioIds.Count > 1)
{
    Console.WriteLine($"All {scenarioIds.Count} scenario(s) passed.");
}

return exitCode;

static void WriteReports(
    string? junitReportPath,
    string? jsonReportPath,
    string? htmlReportPath,
    IReadOnlyList<ScenarioRunResult> results,
    RunReportMetadata metadata)
{
    if (!string.IsNullOrWhiteSpace(junitReportPath))
    {
        JunitReportWriter.Write(junitReportPath, results, metadata);
        Console.WriteLine($"JUnit report: {Path.GetFullPath(junitReportPath)}");
    }

    if (!string.IsNullOrWhiteSpace(jsonReportPath))
    {
        JsonRunReportWriter.Write(jsonReportPath, results, metadata);
        Console.WriteLine($"JSON report: {Path.GetFullPath(jsonReportPath)}");
    }

    if (!string.IsNullOrWhiteSpace(htmlReportPath))
    {
        HtmlReportWriter.Write(htmlReportPath, results, metadata);
        Console.WriteLine($"HTML report: {Path.GetFullPath(htmlReportPath)}");
    }
}

static async Task<(int ExitCode, ScenarioRunResult? Result)> RunOneScenarioAsync(
    ScenarioRunner runner,
    string scenarioId,
    string baseUrl)
{
    string scenarioPath = RepoPaths.ScenarioYamlPath(scenarioId);
    if (!File.Exists(scenarioPath))
    {
        Console.Error.WriteLine($"Scenario not found: {scenarioPath}");
        return (2, null);
    }

    UiScenario scenario;
    try
    {
        scenario = UiScenario.LoadYaml(scenarioPath);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to load scenario: {ex.Message}");
        return (2, null);
    }

    if (!string.Equals(scenario.Id, scenarioId, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Scenario file id '{scenario.Id}' does not match expected '{scenarioId}'.");
        return (2, null);
    }

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
        return (3, null);
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
        return (0, result);
    }

    Console.Error.WriteLine($"Scenario '{result.ScenarioId}' failed: {result.Error}");
    return (4, result);
}
