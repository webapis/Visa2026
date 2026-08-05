using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Visa2026.E2E.Tests.Playwright;

internal static class PlaywrightE2eStepRunner
{
    internal static async Task RunAsync(IPage page, string stepName, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            PlaywrightFailureCapture.RegisterFailure(stepName, ex);
            await PlaywrightFailureCapture.CaptureAsync(page, stepName, ex);
            throw;
        }
    }
}
