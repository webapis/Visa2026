using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Visa2026.Blazor.Server.Hubs;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public sealed class SignalRApplicationRuntimeLogNotifier : IApplicationRuntimeLogNotifier
{
    private readonly IHubContext<ApplicationRuntimeLogHub> hubContext;
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;

    public SignalRApplicationRuntimeLogNotifier(
        IHubContext<ApplicationRuntimeLogHub> hubContext,
        IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor)
    {
        this.hubContext = hubContext;
        this.optionsMonitor = optionsMonitor;
    }

    public async Task NotifyAsync(ApplicationRuntimeLogNotification notification, CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.RealtimeNotifyEnabled)
            return;

        if (!ShouldNotify(notification.Severity, options))
            return;

        await hubContext.Clients.All.SendAsync(
            ApplicationRuntimeLogHubPaths.RuntimeLogRecorded,
            notification,
            cancellationToken).ConfigureAwait(false);
    }

    private static bool ShouldNotify(ApplicationRuntimeLogSeverity severity, ApplicationRuntimeLogOptions options)
    {
        var minSeverity = MapMinSeverity(options.RealtimeNotifyMinLevel);
        return severity >= minSeverity;
    }

    private static ApplicationRuntimeLogSeverity MapMinSeverity(LogLevel level) => level switch
    {
        LogLevel.Critical => ApplicationRuntimeLogSeverity.Critical,
        LogLevel.Error => ApplicationRuntimeLogSeverity.Error,
        LogLevel.Warning => ApplicationRuntimeLogSeverity.Warning,
        _ => ApplicationRuntimeLogSeverity.Error
    };
}
