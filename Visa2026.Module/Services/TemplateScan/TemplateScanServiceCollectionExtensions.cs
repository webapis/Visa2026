#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateScan.Adapters;

namespace Visa2026.Module.Services.TemplateScan;

public static class TemplateScanServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateScan(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IScanAuthoringPlaybookService, ScanAuthoringPlaybookService>();
        services.AddSingleton<IScanInputNormalizer, ScanInputNormalizer>();
        services.AddSingleton<IScanOcrExtractor, ScanOcrExtractor>();
        services.AddSingleton<IScanSuitabilityEvaluator, ScanSuitabilityEvaluator>();
        services.AddSingleton<IScanIngestService, ScanIngestService>();
        services.AddSingleton<IScanFieldPlanMerger, ScanFieldPlanMerger>();
        services.AddSingleton<IScanOfficeYellowExtractor, ScanOfficeYellowExtractor>();
        services.AddSingleton<IScanAmbiguousYellowRefinementService, ScanAmbiguousYellowRefinementService>();
        services.AddSingleton<IScanFieldPlanService, ScanFieldPlanService>();
        services.AddSingleton<ITemplateScanClarificationService, TemplateScanClarificationService>();
        services.AddSingleton<IScanGapPacketExporter, ScanGapPacketExporter>();
        services.AddScoped<IScanDraftDocxBuilder, ScanDraftDocxBuilder>();
        services.AddScoped<IScanDocxLayoutService, ScanDocxLayoutService>();
        services.AddScoped<ITemplateScanOrchestrator, TemplateScanOrchestrator>();
        services.AddSingleton<NoneTemplateScanAiProvider>();

        services.AddSingleton<AzureOpenAiTemplateScanAiProvider>(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<TemplateAiScanOptions>>();
            var factory = sp.GetService<IHttpClientFactory>();
            return factory != null
                ? new AzureOpenAiTemplateScanAiProvider(options, factory)
                : new AzureOpenAiTemplateScanAiProvider(options, new HttpClient());
        });

        services.AddSingleton<ITemplateScanAiProvider>(static sp =>
        {
            var key = sp.GetRequiredService<IOptions<TemplateAiScanOptions>>().Value.Provider;

            if (string.Equals(key, AzureOpenAiTemplateScanAiProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                return sp.GetRequiredService<AzureOpenAiTemplateScanAiProvider>();

            return sp.GetRequiredService<NoneTemplateScanAiProvider>();
        });

        var feature = services.AddOptions<TemplateAiScanOptions>();
        if (configuration != null)
            feature.Bind(configuration.GetSection(TemplateAiScanOptions.SectionName));

        services.Configure<ScanSuitabilityOptions>(options =>
        {
            if (configuration == null)
                return;

            var section = configuration.GetSection(ScanSuitabilityOptions.SectionName);
            if (section.Exists())
                section.Bind(options);
            else
            {
                var nested = configuration.GetSection($"{TemplateAiScanOptions.SectionName}:Suitability");
                if (nested.Exists())
                    nested.Bind(options);
            }
        });

        services.PostConfigure<TemplateAiScanOptions>(options =>
        {
            if (options.Suitability == null!)
                options.Suitability = new ScanSuitabilityOptions();
        });

        return services;
    }
}
