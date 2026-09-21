using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Local-only <c>pg_dump</c> / <c>pg_restore</c> of a seeded <c>visa2026_easytest</c>
/// catalog so Playwright can skip XAF <c>--updateDatabase</c>. Dump is gitignored
/// and keyed to <c>Visa2026.Module</c> AssemblyVersion.
/// </summary>
internal static class EasyTestDatabaseSnapshot
{
    private const string DumpFileName = "visa2026_easytest.dump";
    private const string VersionFileName = "visa2026_easytest.module-version";

    internal static bool IsEnabled =>
        !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_SNAPSHOT"));

    internal static bool RefreshRequested =>
        IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_REFRESH_SNAPSHOT"));

    /// <summary>
    /// CI cache already keys the dump to schema/seed files. Skip Module AssemblyVersion
    /// (bumped every commit) so a cache hit can restore.
    /// </summary>
    internal static bool TrustSnapshot =>
        IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_SNAPSHOT_TRUST"));

    internal static string DirectoryPath =>
        Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\.easytest-snapshots"));

    private static string DumpPath => Path.Combine(DirectoryPath, DumpFileName);

    private static string VersionPath => Path.Combine(DirectoryPath, VersionFileName);

    internal static bool TryRestore(string blazorServerProjectPath)
    {
        if (!IsEnabled)
            return false;

        if (RefreshRequested)
        {
            Log("RefreshSnapshot set — skipping restore.");
            return false;
        }

        if (!TryGetPostgresBin(out string? bin, out string? binReason))
        {
            Log($"No pg_restore ({binReason}). Falling back to --updateDatabase.");
            return false;
        }

        if (!File.Exists(DumpPath))
        {
            Log($"No snapshot at {DirectoryPath}. First run will capture after --updateDatabase.");
            return false;
        }

        string currentVersion = ReadModuleVersion(blazorServerProjectPath);
        if (!TrustSnapshot)
        {
            if (!File.Exists(VersionPath))
            {
                Log($"No snapshot version stamp at {VersionPath}. First Local run will capture after --updateDatabase.");
                return false;
            }

            string snapshotVersion = File.ReadAllText(VersionPath).Trim();
            if (!string.Equals(currentVersion, snapshotVersion, StringComparison.Ordinal))
            {
                Log($"Snapshot Module {snapshotVersion} != {currentVersion}. Falling back to --updateDatabase.");
                return false;
            }
        }
        else
        {
            Log("VISA2026_E2E_SNAPSHOT_TRUST — skipping Module version check (CI cache key is the schema stamp).");
        }

        try
        {
            Log($"Restoring {DumpPath} (Module {currentVersion}).");
            EasyTestDatabaseProvisioner.DropDatabase();
            EasyTestDatabaseProvisioner.EnsureEmptyDatabaseExists();
            RunTool(
                Path.Combine(bin!, "pg_restore.exe"),
                $"-h {EasyTestHostEnvironment.PgHost} -p {EasyTestHostEnvironment.PgPort} -U {EasyTestHostEnvironment.PgUser} -d {EasyTestHostEnvironment.DatabaseName} --no-owner --no-acl \"{DumpPath}\"");
            EasyTestDatabaseProvisioner.WaitUntilDatabaseOnline(TimeSpan.FromMinutes(2));
            Log("Snapshot restore succeeded.");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Snapshot restore failed ({ex.Message}). Falling back to --updateDatabase.");
            return false;
        }
    }

    internal static void TryCapture(string blazorServerProjectPath)
    {
        if (!IsEnabled)
            return;

        if (!TryGetPostgresBin(out string? bin, out string? binReason))
        {
            Log($"Skipping snapshot capture — {binReason}.");
            return;
        }

        try
        {
            Directory.CreateDirectory(DirectoryPath);
            string version = ReadModuleVersion(blazorServerProjectPath);
            Log($"Capturing snapshot to {DumpPath} (Module {version}).");
            RunTool(
                Path.Combine(bin!, "pg_dump.exe"),
                $"-Fc -h {EasyTestHostEnvironment.PgHost} -p {EasyTestHostEnvironment.PgPort} -U {EasyTestHostEnvironment.PgUser} -d {EasyTestHostEnvironment.DatabaseName} --no-owner -f \"{DumpPath}\"");
            File.WriteAllText(VersionPath, version + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Log("Snapshot capture succeeded.");
        }
        catch (Exception ex)
        {
            Log($"Snapshot capture failed ({ex.Message}). Next run will use --updateDatabase.");
        }
    }

    internal static string ReadModuleVersion(string blazorServerProjectPath)
    {
        string dll = Path.Combine(blazorServerProjectPath, "bin", "EasyTest", "net8.0", "Visa2026.Module.dll");
        if (!File.Exists(dll))
            return "unknown";

        Version? version = AssemblyName.GetAssemblyName(dll).Version;
        return version?.ToString() ?? "unknown";
    }

    private static bool TryGetPostgresBin(out string? bin, out string? reason)
    {
        bin = null;
        string? overrideDir = Environment.GetEnvironmentVariable("VISA2026_E2E_PG_BIN")
            ?? Environment.GetEnvironmentVariable("PG_BIN");
        string[] candidates =
        [
            overrideDir ?? string.Empty,
            @"C:\PostgreSQL\16\bin",
            @"C:\PostgreSQL\15\bin",
            @"C:\Program Files\PostgreSQL\17\bin",
            @"C:\Program Files\PostgreSQL\16\bin",
            @"C:\Program Files\PostgreSQL\15\bin",
        ];

        foreach (string dir in candidates)
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;
            if (File.Exists(Path.Combine(dir, "pg_dump.exe")) && File.Exists(Path.Combine(dir, "pg_restore.exe")))
            {
                bin = dir;
                reason = null;
                return true;
            }
        }

        string? fromPath = TryFindOnPath("pg_dump.exe") ?? TryFindOnPath("pg_dump");
        if (!string.IsNullOrWhiteSpace(fromPath))
        {
            string? dir = Path.GetDirectoryName(fromPath);
            if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, "pg_restore.exe")))
            {
                bin = dir;
                reason = null;
                return true;
            }
        }

        reason = "pg_dump.exe not found (set VISA2026_E2E_PG_BIN)";
        return false;
    }

    private static string? TryFindOnPath(string fileName)
    {
        try
        {
            var startInfo = new ProcessStartInfo("where.exe", fileName)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process == null)
                return null;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            if (process.ExitCode != 0)
                return null;
            string[] lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0].Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void RunTool(string exe, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.Environment["PGPASSWORD"] = EasyTestHostEnvironment.PgPassword;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {exe}.");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"{Path.GetFileName(exe)} timed out after 5 minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(exe)} exited {process.ExitCode}. stderr: {stderr.Trim()} stdout: {stdout.Trim()}");
        }
    }

    private static void Log(string message)
    {
        string line = $"[EasyTest] Snapshot: {message}";
        Trace.WriteLine(line);
        Console.WriteLine(line);
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

    private static bool IsFalsy(string? value) =>
        string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
}