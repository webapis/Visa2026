using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRuntimeLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApplicationRuntimeLogOptions>(configuration.GetSection(ApplicationRuntimeLogOptions.SectionName));

        services.AddSingleton<ApplicationRuntimeLogContextAccessor>();
        services.AddSingleton<ApplicationRuntimeLogQueue>();
        services.AddSingleton<IApplicationRuntimeLogPersistence, EfApplicationRuntimeLogPersistence>();
        services.TryAddSingleton<IApplicationRuntimeLogNotifier, NullApplicationRuntimeLogNotifier>();
        services.TryAddSingleton<IApplicationErrorReporter, NullApplicationErrorReporter>();
        services.TryAddSingleton<IApplicationRuntimeLogSentryBridge, NullApplicationRuntimeLogSentryBridge>();
        services.TryAddSingleton<IApplicationRuntimeLogCursorInboxWriter, NullApplicationRuntimeLogCursorInboxWriter>();
        services.AddSingleton<IApplicationRuntimeLogResolution, EfApplicationRuntimeLogResolution>();
        services.AddSingleton<IApplicationRuntimeLogRetention, EfApplicationRuntimeLogRetention>();
        services.AddHostedService<ApplicationRuntimeLogBackgroundService>();
        services.AddHostedService<ApplicationRuntimeLogRetentionBackgroundService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ApplicationRuntimeLogLoggerProvider>());

        return services;
    }
}
