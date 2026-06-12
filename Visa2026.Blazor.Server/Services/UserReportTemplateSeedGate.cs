using System;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Data.SqlClient;
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
/// may skip embedded template seed during DB update. This gate runs after the host DI container is built.
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

        if (!UserReportTemplatesTableExists(services))
        {
            logger?.LogWarning(
                "Skipping user report template seed: dbo.UserReportTemplates does not exist yet. " +
                "Run XAF database update (attach debugger in DEBUG, or set FORCE_XAF_DB_UPDATE=true once).");
            return;
        }

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
        catch (Exception ex) when (IsMissingUserReportTemplatesTableException(ex))
        {
            logger?.LogWarning(
                "Skipping user report template seed: dbo.UserReportTemplates is not available yet.");
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

    private static bool UserReportTemplatesTableExists(IServiceProvider services)
    {
        var connectionString = services.GetService<IConfiguration>()?.GetConnectionString("DefaultConnection")
            ?? services.GetService<IConfiguration>()?.GetConnectionString("ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT CASE WHEN OBJECT_ID(N'dbo.UserReportTemplates', N'U') IS NOT NULL THEN 1 ELSE 0 END";
            var scalar = command.ExecuteScalar();
            return Convert.ToInt32(scalar, CultureInfo.InvariantCulture) == 1;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private static bool IsMissingUserReportTemplatesTableException(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number == 208)
                return true;
        }

        return false;
    }
}
