using System;
using System.Threading.Tasks;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>
/// Wraps Playwright Facts — captures PNG + HTML on failure, then rethrows.
/// Fixture <see cref="PlaywrightE2eFixture"/> performs a final safety-net capture before browser close.
/// </summary>
internal static class PlaywrightE2eTestRunner
{
    internal static async Task RunAsync(PlaywrightE2eFixture fixture, string testName, Func<Task> testBody)
    {
        PlaywrightFailureCapture.ResetForTest();
        try
        {
            await testBody();
        }
        catch (Exception ex)
        {
            PlaywrightFailureCapture.RegisterFailure(testName, ex);
            await PlaywrightFailureCapture.CaptureAsync(fixture.Page, testName, ex);
            throw;
        }
    }
}
