using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using DevExpress.EasyTest.Framework;
using Microsoft.Data.SqlClient;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Logs EasyTest DB + port state, clears stale hosts, then drops the test database before each session.
/// </summary>
internal static class EasyTestPreflight
{
    internal static void PrepareForTestSession(
        EasyTestFixtureContext fixture,
        string databaseAlias,
        string blazorServerProjectPath)
    {
        LogSessionBanner();
        LogDatabaseState();
        LogPortState(EasyTestHostEnvironment.EasyTestPort, "EasyTest host");
        LogPortState(EasyTestHostEnvironment.LegacyUiScenarioPort, "legacy UI-scenario host (removed — should be idle)");

        fixture.CloseRunningApplications();
        KillStaleEasyTestProcesses();

        EnsurePortFree(
            EasyTestHostEnvironment.EasyTestPort,
            "Stop Visual Studio F5 on Visa2026 - EasyTest (LocalDB), other dotnet hosts, or stale msedgedriver.");

        if (IsPortListening(EasyTestHostEnvironment.LegacyUiScenarioPort))
        {
            Trace.WriteLine(
                $"[EasyTest] WARNING: port {EasyTestHostEnvironment.LegacyUiScenarioPort} is in use " +
                "(old UI-scenario profile). EasyTest uses :5050 only; stop the :5052 host to avoid confusion.");
        }

        Trace.WriteLine($"[EasyTest] Dropping database alias '{databaseAlias}' ({EasyTestHostEnvironment.DatabaseName}) for a clean session.");
        fixture.DropDB(databaseAlias);
        LogDatabaseState();

        EasyTestDatabaseProvisioner.EnsureCreated(blazorServerProjectPath);
        LogDatabaseState();
    }

    private static void LogSessionBanner() =>
        Trace.WriteLine($"[EasyTest] Preflight — target {EasyTestHostEnvironment.BaseUrl}, DB {EasyTestHostEnvironment.DatabaseName} on {EasyTestHostEnvironment.LocalDbServer}");

    private static void LogDatabaseState()
    {
        if (EasyTestDatabaseProvisioner.TryQueryDatabaseState(out string? state))
        {
            Trace.WriteLine($"[EasyTest] Database '{EasyTestHostEnvironment.DatabaseName}': state={state}.");
            return;
        }

        Trace.WriteLine($"[EasyTest] Database '{EasyTestHostEnvironment.DatabaseName}': not present or not ONLINE.");
    }

    private static void LogPortState(int port, string label)
    {
        bool listening = IsPortListening(port);
        string httpHint = string.Empty;

        if (listening && port == EasyTestHostEnvironment.EasyTestPort)
            httpHint = ProbeHttpState();

        Trace.WriteLine(
            $"[EasyTest] Port {port} ({label}): {(listening ? "IN USE" : "free")}{httpHint}");
    }

    private static string ProbeHttpState()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = client.GetAsync(EasyTestHostEnvironment.BaseUrl).GetAwaiter().GetResult();
            return $", HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $", HTTP probe failed ({ex.GetType().Name})";
        }
    }

    private static void EnsurePortFree(int port, string remediationHint)
    {
        if (!IsPortListening(port))
            return;

        throw new InvalidOperationException(
            $"EasyTest port {port} is still in use after cleanup. {remediationHint}");
    }

    private static bool IsPortListening(int port) =>
        IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Any(endpoint => endpoint.Port == port);

    private static void KillStaleEasyTestProcesses()
    {
        foreach (Process process in new[] { "Visa2026.Blazor.Server", "msedgedriver" }
                     .SelectMany(Process.GetProcessesByName))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch (Exception)
            {
                // Ignore already-exited or permission-limited processes.
            }
            finally
            {
                process.Dispose();
            }
        }

        Thread.Sleep(TimeSpan.FromMilliseconds(500));
    }
}
