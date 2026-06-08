using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// XAF <see cref="XafApplication.CheckCompatibility"/> can run in <c>AddBuildStep</c> before
/// <see cref="XafApplication.ServiceProvider"/> exists, so <see cref="UserReportTemplateUpdater"/>
/// may skip embedded template seed during DB update. This gate runs after the host DI container is built.
/// </summary>
internal static class UserReportTemplateSeedGate
{
    public static void EnsureSeeded(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        try
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var osFactory = scopedServices.GetRequiredService<INonSecuredObjectSpaceFactory>();

            using var objectSpace = osFactory.CreateNonSecuredObjectSpace(typeof(UserReportTemplate));

#if DEBUG
            bool shouldSeed = true;
#else
            bool shouldSeed = !objectSpace.GetObjectsQuery<UserReportTemplate>().Any();
#endif
            if (!shouldSeed)
                return;

            var moduleVersion = typeof(Visa2026Module).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
            var updater = new UserReportTemplateUpdater(application: null, objectSpace, moduleVersion);
            updater.EnsureLinkIndexesAndSeedTemplates(scopedServices);

            logger?.LogInformation(
                "User report template seed completed ({Count} template(s) in database).",
                objectSpace.GetObjectsQuery<UserReportTemplate>().Count());
        }
        catch (Exception ex)
        {
            logger?.LogErrorWithCode(
                ApplicationRuntimeLogErrorCodes.InfraTemplateSeed,
                ex,
                "User report template seed failed.");
            throw;
        }
    }
}
