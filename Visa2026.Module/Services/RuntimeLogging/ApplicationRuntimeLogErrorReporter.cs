using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogErrorReporter : IApplicationErrorReporter
{
    private readonly ApplicationRuntimeLogQueue queue;
    private readonly ApplicationRuntimeLogContextAccessor contextAccessor;
    private readonly IApplicationRuntimeLogUserContext? userContext;
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;

    public ApplicationRuntimeLogErrorReporter(
        ApplicationRuntimeLogQueue queue,
        ApplicationRuntimeLogContextAccessor contextAccessor,
        IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor,
        IApplicationRuntimeLogUserContext? userContext = null)
    {
        this.queue = queue;
        this.contextAccessor = contextAccessor;
        this.optionsMonitor = optionsMonitor;
        this.userContext = userContext;
    }

    public void Report(ApplicationErrorReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));

        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.ReportUiErrors)
            return;

        if (string.IsNullOrWhiteSpace(report.ErrorCode) || string.IsNullOrWhiteSpace(report.Message))
            return;

        var context = contextAccessor.Context;
        var httpUser = userContext?.GetCurrentUserName();
        var userName = !string.IsNullOrWhiteSpace(httpUser) ? httpUser : context?.UserName;
        var exception = report.Exception;
        var scrubbedMessage = ApplicationRuntimeLogTextHelper.ScrubSecrets(report.Message);

        var entry = new ApplicationRuntimeLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Severity = report.Severity,
            Category = ApplicationRuntimeLogTextHelper.Truncate(report.Category, 512),
            Message = ApplicationRuntimeLogTextHelper.Truncate(scrubbedMessage, 4000),
            ExceptionType = ApplicationRuntimeLogTextHelper.Truncate(exception?.GetType().FullName, 512),
            StackTrace = ApplicationRuntimeLogTextHelper.Truncate(
                ApplicationRuntimeLogTextHelper.ScrubSecrets(exception?.ToString()), 16000),
            ErrorCode = ApplicationRuntimeLogTextHelper.Truncate(report.ErrorCode.Trim(), 64),
            UserName = ApplicationRuntimeLogTextHelper.Truncate(userName, 128),
            CorrelationId = ApplicationRuntimeLogTextHelper.Truncate(context?.CorrelationId, 64),
            RequestPath = ApplicationRuntimeLogTextHelper.Truncate(
                report.RequestPath ?? context?.RequestPath, 512),
            MachineName = ApplicationRuntimeLogTextHelper.Truncate(Environment.MachineName, 128),
            DeploymentEnvironment = ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment(),
            ApplicationVersion = ApplicationRuntimeLogTextHelper.Truncate(
                ApplicationRuntimeLogEnvironmentHelper.ResolveApplicationVersion(), 128),
            RelatedBatchId = report.RelatedBatchId
        };

        queue.TryEnqueue(entry);
    }
}
