using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Visa2026.PortalFiling.Worker;

/// <summary>
/// Playwright automation against rgstr.migration.gov.tm.
/// Scaffold: opens browser with persistent Edge profile only — no form fill yet.
/// </summary>
public sealed class PortalFormFiller
{
    private readonly PortalFilingOptions _options;
    private readonly ILogger<PortalFormFiller> _logger;

    public PortalFormFiller(
        IOptions<PortalFilingOptions> options,
        ILogger<PortalFormFiller> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> FillDraftAsync(FilingJobDto job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        Directory.CreateDirectory(_options.Portal.EdgeUserDataDir);
        Directory.CreateDirectory(_options.Portal.ScreenshotDirectory);

        _logger.LogInformation(
            "Scaffold: launching Edge (headless={Headless}) profile={Profile}",
            _options.Portal.Headless,
            _options.Portal.EdgeUserDataDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchPersistentContextAsync(
            _options.Portal.EdgeUserDataDir,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Channel = "msedge",
                Headless = _options.Portal.Headless,
                Args = new[] { "--start-maximized" }
            });

        var page = browser.Pages.FirstOrDefault() ?? await browser.NewPageAsync();
        await page.GotoAsync(_options.Portal.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var shotPath = Path.Combine(
            _options.Portal.ScreenshotDirectory,
            $"scaffold-{job.Id:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = shotPath, FullPage = true });
        _logger.LogInformation("Scaffold screenshot saved: {Path}", shotPath);
        _logger.LogWarning(
            "Form fill / Add citizen / Save are not implemented yet. See docs/prototypes/playwright-rpa-visa2026-integration-README.md");

        return shotPath;
    }
}