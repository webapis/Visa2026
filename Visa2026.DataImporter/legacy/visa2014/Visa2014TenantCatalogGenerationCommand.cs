using System.Diagnostics;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014TenantCatalogGenerationCommand
{
    public static int Run(string dataImporterRoot, IReadOnlyList<string> args, bool verbose)
    {
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot()
            ?? throw new InvalidOperationException("Could not locate solution root.");

        var section = Visa2014OrderCatalog.LoadTenantCatalogGeneration(dataImporterRoot);
        var legacySource = GetOptionValue(args, "--legacy-source")
            ?? Environment.GetEnvironmentVariable("VISA2014_LEGACY_SOURCE")
            ?? "calik-energi";

        var force = HasArg(args, "--force");
        var allowedSources = section.LegacySources ?? [];
        if (!force
            && allowedSources.Count > 0
            && !allowedSources.Any(s => string.Equals(s, legacySource, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(
                $"INF Skip tenant catalog generation for --legacy-source '{legacySource}' " +
                $"(not listed in order.yaml tenantCatalogGeneration.legacySources). Use --force to run anyway.");
            return 0;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD")))
        {
            Console.Error.WriteLine(
                "ERR VISA2014_SQL_PASSWORD is required for tenant catalog generation (VISA2015 read-only SQL).");
            return 1;
        }

        ApplyLegacySqlConnectionEnv(dataImporterRoot, args, solutionRoot);

        var steps = section.Steps ?? [];
        if (steps.Count == 0)
        {
            Console.Error.WriteLine("ERR order.yaml tenantCatalogGeneration.steps is empty.");
            return 1;
        }

        var ordered = Visa2014OrderCatalog.TopologicalSortSteps(steps);
        Console.WriteLine($"=== VISA2014 tenant catalog generation ({ordered.Count} step(s)) ===");
        Console.WriteLine($"INF Legacy source: {legacySource}");
        if (!string.IsNullOrWhiteSpace(section.RunBeforeImportPhase))
            Console.WriteLine($"INF Runs before import phase: {section.RunBeforeImportPhase}");

        foreach (var step in ordered)
        {
            if (string.IsNullOrWhiteSpace(step.Script))
            {
                Console.Error.WriteLine($"ERR Step '{step.Id}' has no script path.");
                return 1;
            }

            var scriptPath = Path.IsPathRooted(step.Script)
                ? step.Script
                : Path.Combine(solutionRoot, step.Script.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(scriptPath))
            {
                Console.Error.WriteLine($"ERR Script not found for step '{step.Id}': {scriptPath}");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine($">>> {step.Id}");
            Console.WriteLine($"    {step.Script}");
            if (step.Outputs is { Count: > 0 })
                Console.WriteLine($"    -> {string.Join(", ", step.Outputs)}");

            var exitCode = RunPowerShellScript(scriptPath, verbose);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"ERR Step '{step.Id}' failed (exit {exitCode}).");
                return exitCode;
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Tenant catalog generation complete ===");
        return 0;
    }

    private static void ApplyLegacySqlConnectionEnv(string dataImporterRoot, IReadOnlyList<string> args, string solutionRoot)
    {
        var overrideConnection = GetOptionValue(args, "--connection");
        var profile = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        var fullConnection = Visa2014ContentRoot.ResolveConnectionString(overrideConnection, profile.ConnectionString);
        Environment.SetEnvironmentVariable("VISA2014_SQL_CONNECTION", fullConnection);
    }

    private static int RunPowerShellScript(string scriptPath, bool verbose)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start powershell.exe.");

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                Console.Error.WriteLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (verbose && process.ExitCode != 0)
            Console.Error.WriteLine($"ERR powershell exit {process.ExitCode} for {scriptPath}");

        return process.ExitCode;
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }
}
