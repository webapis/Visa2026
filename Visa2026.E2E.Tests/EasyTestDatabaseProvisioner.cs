using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Creates and updates <see cref="EasyTestHostEnvironment.DatabaseName"/> via XAF <c>--updateDatabase</c>
/// after EasyTest <see cref="DevExpress.EasyTest.Framework.EasyTestFixtureContext.DropDB"/>.
/// </summary>
internal static class EasyTestDatabaseProvisioner
{
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
                $"Build the host with 'dotnet build Visa2026.slnx -c EasyTest' and ensure LocalDB is running.\n" +
                $"stderr: {stderr.Trim()}\nstdout: {stdout.Trim()}");
        }

        WaitUntilDatabaseOnline(timeout: TimeSpan.FromMinutes(3));
    }

    /// <summary>
    /// XAF <c>--updateDatabase</c> and Startup schema gates connect to the target DB name;
    /// after <see cref="DevExpress.EasyTest.Framework.EasyTestFixtureContext.DropDB"/> the catalog must exist first.
    /// </summary>
    private static void EnsureEmptyDatabaseExists()
    {
        string databaseName = EasyTestHostEnvironment.DatabaseName;

        using var connection = new SqlConnection(EasyTestHostEnvironment.MasterConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName}') IS NULL
                CREATE DATABASE [{databaseName}];
            """;
        command.ExecuteNonQuery();

        Trace.WriteLine($"[EasyTest] Ensured empty database catalog '{databaseName}' exists on {EasyTestHostEnvironment.LocalDbServer}.");
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
            using var connection = new SqlConnection(EasyTestHostEnvironment.MasterConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT d.state_desc
                FROM sys.databases d
                WHERE d.name = @name
                """;
            command.Parameters.AddWithValue("@name", EasyTestHostEnvironment.DatabaseName);

            object? result = command.ExecuteScalar();
            if (result is not string stateDesc)
                return false;

            state = stateDesc;
            return string.Equals(stateDesc, "ONLINE", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[EasyTest] Database readiness check failed: {ex.Message}");
            return false;
        }
    }
}
