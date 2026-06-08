using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public static class SentryHostExtensions
{
    public static IServiceCollection AddVisaSentry(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SentryRuntimeOptions>(configuration.GetSection(SentryRuntimeOptions.SectionName));
        services.RemoveAll<IApplicationRuntimeLogSentryBridge>();
        services.AddSingleton<IApplicationRuntimeLogSentryBridge, SentryApplicationRuntimeLogBridge>();
        return services;
    }

    public static void ConfigureSentryOptions(WebHostBuilderContext context, SentryAspNetCoreOptions options)
    {
        var configuration = context.Configuration;
        var runtimeOptions = configuration.GetSection(SentryRuntimeOptions.SectionName).Get<SentryRuntimeOptions>()
            ?? new SentryRuntimeOptions();

        if (!runtimeOptions.Enabled)
        {
            options.Dsn = string.Empty;
            options.Debug = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(runtimeOptions.Dsn))
            options.Dsn = runtimeOptions.Dsn;

        options.Environment = !string.IsNullOrWhiteSpace(runtimeOptions.Environment)
            ? runtimeOptions.Environment
            : context.HostingEnvironment.EnvironmentName;

        options.Release = !string.IsNullOrWhiteSpace(runtimeOptions.Release)
            ? runtimeOptions.Release
            : ResolveRelease();

        options.TracesSampleRate = runtimeOptions.TracesSampleRate;
        options.SendDefaultPii = runtimeOptions.SendDefaultPii;
        options.AttachStacktrace = true;

        options.SetBeforeSend(static (@event, _) =>
        {
            if (@event.Message?.Message != null)
            {
                var scrubbed = ApplicationRuntimeLogTextHelper.ScrubSecrets(@event.Message.Message);
                if (!string.Equals(scrubbed, @event.Message.Message, StringComparison.Ordinal))
                    @event.Message = scrubbed;
            }

            if (@event.Request?.Data is string requestData)
                @event.Request.Data = ApplicationRuntimeLogTextHelper.ScrubSecrets(requestData);

            return @event;
        });
    }

    private static string? ResolveRelease()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        var informational = System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(asm)
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational;

        return asm.GetName().Version?.ToString();
    }
}
