using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Npgsql;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Drops/creates <see cref="EasyTestHostEnvironment.DatabaseName"/> and runs XAF <c>--updateDatabase</c>.
/// </summary>
internal static class EasyTestDatabaseProvisioner
{
    internal static void DropDatabase()
    {
        string databaseName = EasyTestHostEnvironment.DatabaseName;
        using var connection = new NpgsqlConnection(EasyTestHostEnvironment.MaintenanceConnectionString);
        connection.Open();

        using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @name AND pid <> pg_backend_pid();
                """;
            terminate.Parameters.AddWithValue("name", databaseName);
            terminate.ExecuteNonQuery();
        }

        using (var drop = connection.CreateCommand())
        {
            drop.CommandText = $"""DROP DATABASE IF EXISTS "{databaseName}";""";
            drop.ExecuteNonQuery();
        }

        Trace.WriteLine($"[EasyTest] Dropped PostgreSQL database '{databaseName}' (if it existed).");
    }

    internal static void EnsureCreated(string blazorServerProjectPath)
    {
        EnsureEmptyDatabaseExists();

        string hostExe = EasyTestHostLaunch.ResolveHostExecutable(blazorServerProjectPath);
        Trace.WriteLine($"[EasyTest] Provisioning database via: {hostExe} {EasyTestHostLaunch.UpdateDatabaseArguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = hostExe,
            Arguments = EasyTestHostLaunch.UpdateDatabaseArguments,
            WorkingDirectory = Path.GetDirectoryName(hostExe)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        EasyTestHostLaunch.ApplyHostEnvironment(startInfo);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Visa2026.Blazor.Server for --updateDatabase.");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Trace.WriteLine($"[EasyTest] --updateDatabase exit code: {process.ExitCode}");
        if (!string.IsNullOrWhiteSpace(stdout))
            Trace.WriteLine($"[EasyTest] --updateDatabase stdout: {stdout.Trim()}");
        if (!string.IsNullOrWhiteSpace(stderr))
            Trace.WriteLine($"[EasyTest] --updateDatabase stderr: {stderr.Trim()}");

        // 0 = completed, 2 = not needed (already current) — both are acceptable.
        if (process.ExitCode is not (0 or 2))
        {
            throw new InvalidOperationException(
                $"EasyTest database provisioning failed (exit {process.ExitCode}). " +
                $"Build the host with 'dotnet build Visa2026.slnx -c EasyTest' and ensure PostgreSQL is running on {EasyTestHostEnvironment.PgHost}:{EasyTestHostEnvironment.PgPort}.\n" +
                $"stderr: {stderr.Trim()}\nstdout: {stdout.Trim()}");
        }

        WaitUntilDatabaseOnline(timeout: TimeSpan.FromMinutes(3));
    }

    private static void EnsureEmptyDatabaseExists()
    {
        string databaseName = EasyTestHostEnvironment.DatabaseName;

        using var connection = new NpgsqlConnection(EasyTestHostEnvironment.MaintenanceConnectionString);
        connection.Open();

        using var existsCmd = connection.CreateCommand();
        existsCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        existsCmd.Parameters.AddWithValue("name", databaseName);
        var exists = existsCmd.ExecuteScalar() != null;
        if (!exists)
        {
            using var create = connection.CreateCommand();
            create.CommandText = $"""CREATE DATABASE "{databaseName}";""";
            create.ExecuteNonQuery();
        }

        Trace.WriteLine($"[EasyTest] Ensured empty PostgreSQL database '{databaseName}' exists on {EasyTestHostEnvironment.PgHost}:{EasyTestHostEnvironment.PgPort}.");
    }

    private static void WaitUntilDatabaseOnline(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (TryQueryDatabaseState(out string? state))
            {
                Trace.WriteLine(
                    $"[EasyTest] Database '{EasyTestHostEnvironment.DatabaseName}' is online (state={state}).");
                return;
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException(
            $"Timed out waiting for database '{EasyTestHostEnvironment.DatabaseName}' after --updateDatabase.");
    }

    internal static bool TryQueryDatabaseState(out string? state)
    {
        state = null;

        try
        {
            using var connection = new NpgsqlConnection(EasyTestHostEnvironment.MaintenanceConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            command.Parameters.AddWithValue("name", EasyTestHostEnvironment.DatabaseName);

            object? result = command.ExecuteScalar();
            if (result is null)
                return false;

            state = "ONLINE";
            return true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[EasyTest] Database readiness check failed: {ex.Message}");
            return false;
        }
    }
}