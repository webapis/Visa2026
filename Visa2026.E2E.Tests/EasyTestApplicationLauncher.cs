using System;
using System.Diagnostics;
using System.Threading;
using DevExpress.EasyTest.Framework;
using OpenQA.Selenium;

namespace Visa2026.E2E.Tests;

/// <summary>Starts the Blazor host + Edge with CI-tuned retries when script loading is slow.</summary>
internal static class EasyTestApplicationLauncher
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    internal static void RunApplication(EasyTestFixtureContext fixture, IApplicationContext appContext)
    {
        int maxAttempts = EasyTestCITuning.RunApplicationMaxAttempts;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    Trace.WriteLine($"[EasyTest] RunApplication retry {attempt}/{maxAttempts}.");
                    EasyTestHostLifecycle.StopHost(fixture);
                    Thread.Sleep(RetryDelay);
                }

                Trace.WriteLine(
                    $"[EasyTest] RunApplication attempt {attempt}/{maxAttempts} " +
                    $"(headless={EasyTestBrowserMode.RunHeadless}).");

                appContext.RunApplication();
                Trace.WriteLine($"[EasyTest] RunApplication succeeded (attempt {attempt}).");
                return;
            }
            catch (WebDriverTimeoutException ex) when (attempt < maxAttempts)
            {
                Trace.WriteLine($"[EasyTest] RunApplication WebDriver timeout (attempt {attempt}): {ex.Message}");
            }
        }
    }
}
