using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public static class ApplicationRuntimeLogHostExtensions
{
    public static IServiceCollection AddVisaApplicationRuntimeLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddApplicationRuntimeLogging(configuration);
        services.AddSingleton<IApplicationRuntimeLogUserContext, HttpApplicationRuntimeLogUserContext>();
        services.AddSingleton<ApplicationRuntimeLogNavigationHelper>();
        services.RemoveAll<IApplicationRuntimeLogNotifier>();
        services.AddSingleton<IApplicationRuntimeLogNotifier, SignalRApplicationRuntimeLogNotifier>();
        services.RemoveAll<IApplicationErrorReporter>();
        services.AddSingleton<IApplicationErrorReporter, ApplicationRuntimeLogErrorReporter>();
        services.RemoveAll<IApplicationRuntimeLogCursorInboxWriter>();
        services.AddSingleton<IApplicationRuntimeLogCursorInboxWriter, FileCursorRuntimeErrorInboxWriter>();
        return services;
    }
}
