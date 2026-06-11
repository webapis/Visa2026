using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;

namespace Visa2026.E2E.Tests;

/// <summary>HTTP probes for diagnosing Blazor host readiness on CI.</summary>
internal static class EasyTestHostReadiness
{
    internal static void LogHttpProbe(string label)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using HttpResponseMessage response = client
                .GetAsync(EasyTestHostEnvironment.BaseUrl)
                .GetAwaiter()
                .GetResult();

            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            string prefix = body.Length > 500 ? body[..500] : body;
            string message =
                $"[EasyTest] {label} HTTP {(int)response.StatusCode} {response.ReasonPhrase}, " +
                $"body length {body.Length}. Prefix: {prefix.ReplaceLineEndings(" ")}";

            Trace.WriteLine(message);
            Console.WriteLine(message);
        }
        catch (Exception ex)
        {
            string message = $"[EasyTest] {label} HTTP probe failed: {ex.Message}";
            Trace.WriteLine(message);
            Console.WriteLine(message);
        }
    }

    internal static void WaitUntilHttpResponds(TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        DateTime nextProgressLog = DateTime.UtcNow;

        while (DateTime.UtcNow < deadline)
        {
            if (DateTime.UtcNow >= nextProgressLog)
            {
                string progress =
                    $"[EasyTest] Waiting for host HTTP at {EasyTestHostEnvironment.BaseUrl} " +
                    $"(port listening: {EasyTestHostLifecycle.IsPortListening(EasyTestHostEnvironment.EasyTestPort)})...";
                Trace.WriteLine(progress);
                Console.WriteLine(progress);
                nextProgressLog = DateTime.UtcNow.AddSeconds(15);
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                using HttpResponseMessage response = client
                    .GetAsync(EasyTestHostEnvironment.BaseUrl)
                    .GetAwaiter()
                    .GetResult();

                if (response.IsSuccessStatusCode)
                {
                    string ready = $"[EasyTest] Host HTTP ready ({(int)response.StatusCode}).";
                    Trace.WriteLine(ready);
                    Console.WriteLine(ready);
                    return;
                }
            }
            catch (Exception)
            {
                // Host still starting.
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
        }

        LogHttpProbe("Host not ready after wait");
        throw new TimeoutException(
            $"EasyTest host did not respond at {EasyTestHostEnvironment.BaseUrl} within {timeout}.{Environment.NewLine}" +
            EasyTestHostProcessLauncher.BuildDiagnostics());
    }
}
