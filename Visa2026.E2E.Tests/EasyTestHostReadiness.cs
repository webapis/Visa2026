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

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                using HttpResponseMessage response = client
                    .GetAsync(EasyTestHostEnvironment.BaseUrl)
                    .GetAwaiter()
                    .GetResult();

                if (response.IsSuccessStatusCode)
                {
                    Trace.WriteLine($"[EasyTest] Host HTTP ready ({(int)response.StatusCode}).");
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
    }
}
