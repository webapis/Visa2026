using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogLoggerProvider : ILoggerProvider
{
    private readonly ApplicationRuntimeLogQueue queue;
    private readonly ApplicationRuntimeLogContextAccessor contextAccessor;
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;
    private readonly IApplicationRuntimeLogUserContext? userContext;

    public ApplicationRuntimeLogLoggerProvider(
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

    public ILogger CreateLogger(string categoryName) =>
        new ApplicationRuntimeLogLogger(categoryName, queue, contextAccessor, optionsMonitor, userContext);

    public void Dispose()
    {
    }

    private sealed class ApplicationRuntimeLogLogger : ILogger
    {
        private readonly string categoryName;
        private readonly ApplicationRuntimeLogQueue queue;
        private readonly ApplicationRuntimeLogContextAccessor contextAccessor;
        private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;
        private readonly IApplicationRuntimeLogUserContext? userContext;

        public ApplicationRuntimeLogLogger(
            string categoryName,
            ApplicationRuntimeLogQueue queue,
            ApplicationRuntimeLogContextAccessor contextAccessor,
            IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor,
            IApplicationRuntimeLogUserContext? userContext)
        {
            this.categoryName = categoryName;
            this.queue = queue;
            this.contextAccessor = contextAccessor;
            this.optionsMonitor = optionsMonitor;
            this.userContext = userContext;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            var options = optionsMonitor.CurrentValue;
            if (!options.Enabled)
                return false;

            if (IsSelfCategory())
                return false;

            if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
                return logLevel >= options.MinLevel;

            if (logLevel == LogLevel.Warning && options.PersistWarnings)
                return true;

            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                return;

            var options = optionsMonitor.CurrentValue;
            if (logLevel < options.MinLevel && logLevel != LogLevel.Critical)
            {
                if (!(logLevel == LogLevel.Warning && options.PersistWarnings))
                    return;
            }

            if (!ShouldPersistCategory(categoryName, logLevel, options))
                return;

            var formattedMessage = formatter(state, exception);
            var scrubbedMessage = ApplicationRuntimeLogTextHelper.ScrubSecrets(formattedMessage);
            var context = contextAccessor.Context;
            var httpUser = userContext?.GetCurrentUserName();
            var userName = !string.IsNullOrWhiteSpace(httpUser) ? httpUser : context?.UserName;

            IReadOnlyList<KeyValuePair<string, object?>>? stateProperties = state as IReadOnlyList<KeyValuePair<string, object?>>;

            var entry = new ApplicationRuntimeLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Severity = ApplicationRuntimeLogEntry.MapSeverity(logLevel),
                Category = ApplicationRuntimeLogTextHelper.Truncate(categoryName, 512),
                Message = ApplicationRuntimeLogTextHelper.Truncate(scrubbedMessage, 4000),
                ExceptionType = ApplicationRuntimeLogTextHelper.Truncate(exception?.GetType().FullName, 512),
                StackTrace = ApplicationRuntimeLogTextHelper.Truncate(
                    ApplicationRuntimeLogTextHelper.ScrubSecrets(exception?.ToString()), 16000),
                ErrorCode = ApplicationRuntimeLogTextHelper.ResolveErrorCode(stateProperties, categoryName, scrubbedMessage),
                UserName = ApplicationRuntimeLogTextHelper.Truncate(userName, 128),
                CorrelationId = ApplicationRuntimeLogTextHelper.Truncate(context?.CorrelationId, 64),
                RequestPath = ApplicationRuntimeLogTextHelper.Truncate(context?.RequestPath, 512),
                MachineName = ApplicationRuntimeLogTextHelper.Truncate(Environment.MachineName, 128),
                DeploymentEnvironment = ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment(),
                ApplicationVersion = ApplicationRuntimeLogTextHelper.Truncate(
                    ApplicationRuntimeLogEnvironmentHelper.ResolveApplicationVersion(), 128),
                RelatedBatchId = ApplicationRuntimeLogTextHelper.TryExtractBatchId(scrubbedMessage, stateProperties)
            };

            queue.TryEnqueue(entry);
        }

        private bool IsSelfCategory() =>
            categoryName.Contains("ApplicationRuntimeLog", StringComparison.Ordinal);

        private static bool ShouldPersistCategory(string category, LogLevel logLevel, ApplicationRuntimeLogOptions options)
        {
            if (category.StartsWith("Visa2026.", StringComparison.Ordinal))
                return true;

            if (logLevel >= LogLevel.Error
                && category.StartsWith("Microsoft.", StringComparison.Ordinal))
                return true;

            if (options.PersistWarnings
                && logLevel == LogLevel.Warning
                && category.StartsWith("Visa2026.", StringComparison.Ordinal))
                return true;

            return false;
        }
    }
}
