using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogEnvironmentHelper
{
    public static ApplicationRuntimeLogDeploymentEnvironment DetectDeploymentEnvironment()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APP_POOL_ID")))
            return ApplicationRuntimeLogDeploymentEnvironment.IisProduction;

        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(aspNetCoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio;

        if (string.Equals(aspNetCoreEnvironment, "Production", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogDeploymentEnvironment.IisProduction;

        return ApplicationRuntimeLogDeploymentEnvironment.Unknown;
    }

    public static string? ResolveApplicationVersion()
    {
        var asm = typeof(Visa2026Module).Assembly;
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational;

        return asm.GetName().Version?.ToString();
    }

    public static string? ResolveConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("ConnectionString");
    }
}
