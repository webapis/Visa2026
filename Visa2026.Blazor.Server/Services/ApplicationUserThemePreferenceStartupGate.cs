using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Staging/prod can skip XAF ModuleUpdaters when ModuleInfo is already current. Ensures theme preference
/// columns and Default-role self-write permissions exist on every host start (mirrors batch schema gate).
/// </summary>
internal static class ApplicationUserThemePreferenceStartupGate
{
    public static void EnsureReady(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var connectionString = services.GetService<IConfiguration>()?.GetConnectionString("DefaultConnection")
            ?? services.GetService<IConfiguration>()?.GetConnectionString("ConnectionString");
        if (!string.IsNullOrWhiteSpace(connectionString)
            && DatabaseProviderDetector.IsSqlServer(connectionString))
        {
            ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing(connectionString);
        }

        try
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var objectSpaceFactory = scopedServices.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(ApplicationUser));
            ApplicationUserThemePreferencePermissions.EnsureDefaultRoleSelfWrite(objectSpace);

            logger?.LogInformation("ApplicationUser theme preference schema and Default-role permissions verified.");
        }
        catch (Exception ex)
        {
            logger?.LogWarningWithCode(
                ApplicationRuntimeLogErrorCodes.InfraDbUpdate,
                ex,
                "ApplicationUser theme preference startup gate failed.");
        }
    }
}
