using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Visa2026.E2E.Tests.UserManual;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;
/// <summary>One Playwright browser per test assembly (Local or Staging per <c>VISA2026_E2E_TARGET</c>).</summary>
public sealed class PlaywrightE2eFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IPage Page { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        PlaywrightFailureCapture.ResetForTest();
        Console.WriteLine($"[Playwright] Target={PlaywrightE2eEnvironment.Target}, BaseUrl={PlaywrightE2eEnvironment.BaseUrl}");

        if (PlaywrightE2eEnvironment.IsLocal)
            PlaywrightE2eLocalBootstrap.Prepare();
        else
            PlaywrightE2eLocalBootstrap.VerifyStagingReachable();

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = EasyTestBrowserMode.RunHeadless,
            Channel = "msedge",
        });

        string? videoDir = null;
        if (UserManualVideoMarkerCapture.VideoRecordingEnabled)
        {
            string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
                ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            videoDir = Path.GetFullPath(Path.Combine(
                Environment.CurrentDirectory,
                @"..\..\..\recordings\videos",
                runId));
            Directory.CreateDirectory(videoDir);
        }

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = PlaywrightE2eEnvironment.IgnoreHttpsErrors,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
            RecordVideoDir = videoDir,
            RecordVideoSize = videoDir == null ? null : new RecordVideoSize { Width = 1920, Height = 1080 },
        });

        Page = await context.NewPageAsync();
        await Page.SetViewportSizeAsync(1920, 1080);
        if (videoDir != null)
            UserManualVideoMarkerCapture.SetRecordingStart();
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
        {
            await PlaywrightFailureCapture.CaptureBeforeExitAsync(Page);

            if (Page.Video != null)
            {
                try
                {
                    string videoPath = await Page.Video.PathAsync();
                    UserManualVideoMarkerCapture.SetSourceVideoPath(videoPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Playwright] Video path read failed: {ex.Message}");
                }
            }

            await Page.CloseAsync();
        }

        if (_browser != null)
            await _browser.CloseAsync();

        _playwright?.Dispose();

        if (PlaywrightE2eEnvironment.IsLocal)
            EasyTestHostLifecycle.StopHost();

        UserManualVideoMarkerCapture.FlushToDisk(final: true);
    }
}
