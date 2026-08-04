using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>Prepares local :5050 host + fresh EasyTest DB for Playwright Local E2E.</summary>
internal static class PlaywrightE2eLocalBootstrap
{
    internal static void Prepare()
    {
        string blazorServerProjectPath = Path.GetFullPath(
            Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.Blazor.Server"));

        Console.WriteLine($"[Playwright] Local bootstrap — DB {EasyTestHostEnvironment.DatabaseName}, host {PlaywrightE2eEnvironment.BaseUrl}");

        EasyTestHostLifecycle.KillHostProcesses();
        WaitForPortFree(EasyTestHostEnvironment.EasyTestPort);

        EasyTestDatabaseProvisioner.DropDatabase();
        EasyTestDatabaseProvisioner.EnsureCreated(blazorServerProjectPath);
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
