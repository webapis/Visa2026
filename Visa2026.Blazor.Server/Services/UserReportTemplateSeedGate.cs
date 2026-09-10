using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.Configuration;
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
/// may skip embedded template seed during DB update. This gate runs after <c>app.UseXaf()</c>
/// so ValueManager is Blazor-ready. Headless <c>--inprocess</c> import skips seed
/// (<c>VISA2026_HEADLESS_IMPORT</c>).
/// </summary>
internal static class UserReportTemplateSeedGate
{
    public static void EnsureSeeded(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (EasyTestHostMode.IsEnabled)
        {
            logger?.LogInformation("Skipping user report template seed for EasyTest host.");
            return;
        }

        // Headless --inprocess import starts Configure before UseXaf(). Template OnSaving
        // touches SecuritySystem.CurrentUserName and initializes SimpleValueManager, then
        // UseXaf() cannot switch to AsyncValueManager (fresh Debug DB after DROP DATABASE).
        if (IsEnvFlagSet("VISA2026_HEADLESS_IMPORT"))
        {
            logger?.LogInformation("Skipping user report template seed for headless VISA2014 import.");
            return;
        }

        try
        {
            var configuration = services.GetService<IConfiguration>();
            var connectionString = configuration?.GetConnectionString("DefaultConnection")
                ?? configuration?.GetConnectionString("ConnectionString");
            if (!PostgresRelationExists.All(connectionString, "ApplicationTypes", "UserReportTemplates"))
            {
                logger?.LogInformation(
                    "User report template seed skipped — schema not created yet (CheckCompatibility still pending).");
                return;
            }

            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var osFactory = scopedServices.GetRequiredService<INonSecuredObjectSpaceFactory>();

            using var objectSpace = osFactory.CreateNonSecuredObjectSpace(typeof(UserReportTemplate));

            // Groups must exist even when full template seed is skipped (Release with existing rows).
            ApplicationTypeGroupSchemaSql.EnsureTables(objectSpace);
            ApplicationTypeGroupSeed.EnsureRegistrationGroup(objectSpace);
            objectSpace.CommitChanges();

            if (!UserReportTemplateUpdater.SeedEmbeddedTemplatesEnabled)
            {
                logger?.LogInformation(
                    "User report template seed skipped (SeedEmbeddedTemplatesEnabled=false); ApplicationTypeGroup Registration ensured.");
                return;
            }

#if DEBUG
            bool shouldSeed = true;
#else
            bool shouldSeed = !objectSpace.GetObjectsQuery<UserReportTemplate>().Any();
#endif
            if (!shouldSeed)
            {
                logger?.LogInformation(
                    "User report template seed skipped (templates already present); ApplicationTypeGroup Registration ensured.");
                return;
            }

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

    private static bool IsEnvFlagSet(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }
}
