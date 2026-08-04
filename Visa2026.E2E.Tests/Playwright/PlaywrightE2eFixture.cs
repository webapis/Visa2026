using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
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

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = PlaywrightE2eEnvironment.IgnoreHttpsErrors,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
        });

        Page = await context.NewPageAsync();
        await Page.SetViewportSizeAsync(1920, 1080);
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
            await Page.CloseAsync();

        if (_browser != null)
            await _browser.CloseAsync();

        _playwright?.Dispose();

        if (PlaywrightE2eEnvironment.IsLocal)
            EasyTestHostLifecycle.StopHost();
    }
}
