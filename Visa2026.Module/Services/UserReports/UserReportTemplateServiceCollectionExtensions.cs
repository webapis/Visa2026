using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Visa2026.Module.Services.UserReports;

public static class UserReportTemplateServiceCollectionExtensions
{
    public static IServiceCollection AddUserReportTemplateStaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TemplateEditStagingOptions>(
            configuration.GetSection(TemplateEditStagingOptions.SectionName));
        services.AddScoped<IUserReportTemplateMaintenanceService, UserReportTemplateMaintenanceService>();
        services.AddScoped<IUserReportTemplateStagingService, UserReportTemplateStagingService>();
        return services;
    }
}
