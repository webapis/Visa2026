using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Ensures Default-role self-write permissions for theme preference exist on every host start.
/// Theme preference columns are created by EF/XAF database update on PostgreSQL.
/// </summary>
internal static class ApplicationUserThemePreferenceStartupGate
{
    public static void EnsureReady(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        try
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var objectSpaceFactory = scopedServices.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(ApplicationUser));
            ApplicationUserThemePreferencePermissions.EnsureDefaultRoleSelfWrite(objectSpace);

            logger?.LogInformation("ApplicationUser theme preference Default-role permissions verified.");
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