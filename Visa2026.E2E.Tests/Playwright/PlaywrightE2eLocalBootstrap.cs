using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>Prepares local :5050 host for Playwright Local E2E (snapshot restore by default; KeepDb/KeepHost opt-in).</summary>
internal static class PlaywrightE2eLocalBootstrap
{
    internal static void Prepare()
    {
        string blazorServerProjectPath = Path.GetFullPath(
            Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.Blazor.Server"));

        bool keepHost = PlaywrightE2eEnvironment.KeepHost;
        bool keepDb = PlaywrightE2eEnvironment.KeepDb;

        Console.WriteLine(
            $"[Playwright] Local bootstrap — DB {EasyTestHostEnvironment.DatabaseName}, host {PlaywrightE2eEnvironment.BaseUrl}, KeepDb={keepDb}, KeepHost={keepHost}, Snapshot={PlaywrightE2eEnvironment.UseSnapshot}, RefreshSnapshot={PlaywrightE2eEnvironment.RefreshSnapshot}");

        if (keepHost && EasyTestHostReadiness.IsHttpReady())
        {
            Console.WriteLine("[Playwright] Reusing existing :5050 host (VISA2026_E2E_KEEP_HOST).");
            return;
        }

        if (keepHost && EasyTestHostLifecycle.IsPortListening(EasyTestHostEnvironment.EasyTestPort))
        {
            Console.WriteLine("[Playwright] Port 5050 is listening but HTTP is not ready — restarting EasyTest host.");
            EasyTestHostLifecycle.KillHostProcesses();
            WaitForPortFree(EasyTestHostEnvironment.EasyTestPort);
        }
        else if (!keepHost)
        {
            EasyTestHostLifecycle.KillHostProcesses();
            WaitForPortFree(EasyTestHostEnvironment.EasyTestPort);
        }

        if (keepDb)
        {
            if (!EasyTestDatabaseProvisioner.DatabaseExists())
            {
                Console.WriteLine("[Playwright] KeepDb requested but database is missing — provisioning visa2026_easytest.");
                EasyTestDatabaseProvisioner.EnsureCreated(blazorServerProjectPath);
                EasyTestDatabaseSnapshot.TryCapture(blazorServerProjectPath);
            }
            else
            {
                Console.WriteLine("[Playwright] Reusing existing visa2026_easytest (VISA2026_E2E_KEEP_DB).");
            }
        }
        else if (EasyTestDatabaseSnapshot.TryRestore(blazorServerProjectPath))
        {
            // Seeded catalog restored from local pg_dump.
        }
        else
        {
            EasyTestDatabaseProvisioner.DropDatabase();
            EasyTestDatabaseProvisioner.EnsureCreated(blazorServerProjectPath);
            EasyTestDatabaseSnapshot.TryCapture(blazorServerProjectPath);
        }

        EasyTestHostProcessLauncher.EnsureHostRunning(blazorServerProjectPath);
        EasyTestHostReadiness.WaitUntilHttpResponds(TimeSpan.FromMinutes(3));
    }

    private static void WaitForPortFree(int port)
    {
        if (!EasyTestHostLifecycle.IsPortListening(port))
            return;

        Thread.Sleep(TimeSpan.FromSeconds(2));
        if (!EasyTestHostLifecycle.IsPortListening(port))
            return;

        throw new InvalidOperationException(
            $"Port {port} is still in use. Stop F5 / other EasyTest hosts before Playwright Local E2E.");
    }

    internal static void VerifyStagingReachable()
    {
        string loginUrl = $"{PlaywrightE2eEnvironment.BaseUrl.TrimEnd('/')}/LoginPage";
        Console.WriteLine($"[Playwright] Staging probe — {loginUrl}");

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = PlaywrightE2eEnvironment.IgnoreHttpsErrors
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null,
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        using var response = client.GetAsync(loginUrl).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Staging E2E probe failed: HTTP {(int)response.StatusCode} for {loginUrl}");
        }
    }
}
