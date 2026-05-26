namespace Visa2026.Blazor.Server;

/// <summary>Visa2026 usage / trial license shown on the login page. Host-only — see appsettings UsageLicense.</summary>
public sealed class UsageLicenseOptions
{
    /// <summary>When false, the login banner is not rendered.</summary>
    public bool Enabled { get; set; }

    /// <summary>UTC end of the licensed usage window. Preferred over TrialStartUtc + TrialDays.</summary>
    public DateTime? TrialEndUtc { get; set; }

    /// <summary>UTC start when TrialEndUtc is not set.</summary>
    public DateTime? TrialStartUtc { get; set; }

    public int TrialDays { get; set; } = 90;

    public string? ContactEmail { get; set; }

    public string? ContactUrl { get; set; }
}
