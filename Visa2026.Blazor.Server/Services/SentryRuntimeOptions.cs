namespace Visa2026.Blazor.Server.Services;

public sealed class SentryRuntimeOptions
{
    public const string SectionName = "Sentry";

    /// <summary>Master switch; when false the SDK DSN is cleared even if configured.</summary>
    public bool Enabled { get; set; }

    public string? Dsn { get; set; }

    /// <summary>Overrides ASPNETCORE_ENVIRONMENT when set.</summary>
    public string? Environment { get; set; }

    /// <summary>Overrides assembly informational version when set.</summary>
    public string? Release { get; set; }

    /// <summary>Send rows from <see cref="Visa2026.Module.Services.RuntimeLogging.ApplicationRuntimeLogBackgroundService"/>.</summary>
    public bool BridgeRuntimeLog { get; set; } = true;

    /// <summary>When false, only Error/Critical rows are bridged.</summary>
    public bool BridgeWarnings { get; set; }

    public double TracesSampleRate { get; set; }

    public bool SendDefaultPii { get; set; }
}
