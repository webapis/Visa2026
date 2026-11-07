namespace Visa2026.PortalFiling.Worker;

/// <summary>Configuration for the portal-filing Playwright worker (scaffold).</summary>
public sealed class PortalFilingOptions
{
    public const string SectionName = "PortalFiling";

    public Visa2026ApiOptions Visa2026 { get; set; } = new();
    public PortalBrowserOptions Portal { get; set; } = new();
    public int PollSeconds { get; set; } = 5;

    /// <summary>
    /// When true, the worker only validates config and exits (no browser, no API poll).
    /// Used until Visa2026 job API + portal selectors are implemented.
    /// </summary>
    public bool ScaffoldOnly { get; set; } = true;
}

public sealed class Visa2026ApiOptions
{
    public string BaseUrl { get; set; } = "https://localhost:44300/";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class PortalBrowserOptions
{
    public string BaseUrl { get; set; } = "https://rgstr.migration.gov.tm/";
    public string EdgeUserDataDir { get; set; } = @"C:\PortalFiling\edge-profile";
    public bool Headless { get; set; }
    public string ScreenshotDirectory { get; set; } = @"C:\PortalFiling\screenshots";
}