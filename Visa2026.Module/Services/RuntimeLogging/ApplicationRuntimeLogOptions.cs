using Microsoft.Extensions.Logging;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogOptions
{
    public const string SectionName = "ApplicationRuntimeLog";

    public bool Enabled { get; set; } = true;

    /// <summary>When true, <see cref="IApplicationErrorReporter"/> persists UI-only failures.</summary>
    public bool ReportUiErrors { get; set; } = true;

    public bool PersistWarnings { get; set; }

    public LogLevel MinLevel { get; set; } = LogLevel.Error;

    public int QueueCapacity { get; set; } = 1000;

    public int RetentionDays { get; set; } = 90;

    /// <summary>How often the retention worker runs (default: every 24 hours).</summary>
    public int RetentionCleanupIntervalHours { get; set; } = 24;

    /// <summary>Rows deleted per SQL batch during retention purge.</summary>
    public int RetentionBatchSize { get; set; } = 500;

    public bool RealtimeNotifyEnabled { get; set; } = true;

    public LogLevel RealtimeNotifyMinLevel { get; set; } = LogLevel.Error;

    /// <summary>Write persisted errors to <c>.cursor/runtime-errors/inbox/</c> for Cursor agent triage (dev).</summary>
    public bool CursorBridgeEnabled { get; set; }

    /// <summary>When true, only rows with <see cref="BusinessObjects.Operations.ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio"/> are bridged.</summary>
    public bool CursorBridgeLocalDevOnly { get; set; } = true;

    /// <summary>Optional absolute inbox directory; default is repo <c>.cursor/runtime-errors/inbox</c>.</summary>
    public string? CursorBridgeInboxPath { get; set; }

    public LogLevel CursorBridgeMinLevel { get; set; } = LogLevel.Error;
}
