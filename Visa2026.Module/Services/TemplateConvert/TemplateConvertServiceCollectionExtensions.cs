using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateConvert.Adapters;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public static class TemplateConvertServiceCollectionExtensions
{
    /// <summary>Registers template conversion services, the AI provider seam, and the Azure OpenAI adapter (E10).</summary>
    public static IServiceCollection AddTemplateConvert(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<ITemplateTokenWriter, TemplateTokenWriter>();
        services.AddSingleton<ITemplateConversionDiffGate, TemplateConversionDiffGate>();
        services.AddSingleton<ITemplateResidualValueScanner, TemplateResidualValueScanner>();
        services.AddSingleton<IApplicationProfilePlaceholderSetService, ApplicationProfilePlaceholderSetService>();
        services.AddSingleton<IApplicationProfileInstanceValueMapService, ApplicationProfileInstanceValueMapService>();
        services.AddSingleton<ITemplateCandidateAnalyzer, TemplateCandidateAnalyzer>();
        services.AddSingleton<ITemplateDocumentOutlineReader, TemplateDocumentOutlineReader>();
        services.AddSingleton<ITemplateMappingPlanSanitizer, TemplateMappingPlanSanitizer>();
        services.AddSingleton<ITemplateConvertChatService, TemplateConvertChatService>();
        services.AddSingleton<NoneTemplateConvertAiProvider>();

        // Host Startup already calls AddHttpClient(); unit tests may omit IHttpClientFactory.
        services.AddSingleton<AzureOpenAiTemplateConvertAiProvider>(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<TemplateAiConvertOptions>>();
            var factory = sp.GetService<IHttpClientFactory>();
            return factory != null
                ? new AzureOpenAiTemplateConvertAiProvider(options, factory)
                : new AzureOpenAiTemplateConvertAiProvider(options, new HttpClient());
        });

        // Resolve by TemplateAiConvert:Provider. Unknown keys fall back to None (Q14).
        services.AddSingleton<ITemplateConvertAiProvider>(static sp =>
        {
            var key = sp.GetRequiredService<IOptions<TemplateAiConvertOptions>>().Value.Provider;

            if (string.Equals(key, AzureOpenAiTemplateConvertAiProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                return sp.GetRequiredService<AzureOpenAiTemplateConvertAiProvider>();

            return sp.GetRequiredService<NoneTemplateConvertAiProvider>();
        });

        services.AddScoped<IEphemeralTemplateValidationService, EphemeralTemplateValidationService>();
        services.AddScoped<ITemplateConvertOrchestrator, TemplateConvertOrchestrator>();

        var suitability = services.AddOptions<TemplateSuitabilityOptions>();
        if (configuration != null)
            suitability.Bind(configuration.GetSection(TemplateSuitabilityOptions.SectionName));

        var feature = services.AddOptions<TemplateAiConvertOptions>();
        if (configuration != null)
            feature.Bind(configuration.GetSection(TemplateAiConvertOptions.SectionName));

        return services;
    }
}