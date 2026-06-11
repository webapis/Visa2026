using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using DevExpress.EasyTest.Framework;

namespace Visa2026.E2E.Tests;

/// <summary>Starts/stops the EasyTest Blazor host and Edge driver so ports and DLLs are released after each test.</summary>
internal static class EasyTestHostLifecycle
{
    internal static void StopHost(EasyTestFixtureContext? fixture = null)
    {
        Trace.WriteLine("[EasyTest] Stopping Blazor host and browser.");

        try
        {
            fixture?.CloseRunningApplications();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[EasyTest] CloseRunningApplications failed: {ex.Message}");
        }

        KillHostProcesses();
        WaitForPortReleased(EasyTestHostEnvironment.EasyTestPort, TimeSpan.FromSeconds(15));
    }

    internal static void KillHostProcesses()
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

    internal static bool IsPortListening(int port) =>
        IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Any(endpoint => endpoint.Port == port);

    private static void WaitForPortReleased(int port, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsPortListening(port))
            {
                Trace.WriteLine($"[EasyTest] Port {port} released.");
                return;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }

        Trace.WriteLine($"[EasyTest] WARNING: port {port} still listening after host stop.");
    }
}
