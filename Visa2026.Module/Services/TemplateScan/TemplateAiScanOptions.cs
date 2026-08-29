#nullable enable

using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public sealed class TemplateAiScanOptions
{
    public const string SectionName = "TemplateAiScan";

    public bool Enabled { get; set; }

    public bool ShowInstanceEntry { get; set; }

    public long MaxUploadBytes { get; set; } = 20_971_520;

    public long HardMaxUploadBytes { get; set; } = 52_428_800;

    public int MaxPdfPages { get; set; } = 5;

    public string Provider { get; set; } = NoneTemplateScanAiProvider.ProviderKey;

    public int RequestTimeoutSeconds { get; set; } = 90;

    public int MaxPromptCharacters { get; set; } = 50_000;

    public bool RedactIdentifiersInExtract { get; set; } = true;

    public TemplateAiScanAzureOpenAiOptions AzureOpenAI { get; set; } = new();

    public ScanSuitabilityOptions Suitability { get; set; } = new();
}

public sealed class TemplateAiScanAzureOpenAiOptions
{
    public string? Endpoint { get; set; }

    public string? Deployment { get; set; }

    public string ApiVersion { get; set; } = "2024-10-21";

    /// <summary>Prefer env <c>TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY</c>.</summary>
    public string? ApiKey { get; set; }
}

/// <summary>SD-D2 thresholds — bound to <c>TemplateAiScan:Suitability</c>.</summary>
public sealed class ScanSuitabilityOptions
{
    public const string SectionName = "TemplateAiScan:Suitability";

    public double FailBelowTextConfidence { get; set; } = 0.40;

    public double WarnBelowTextConfidence { get; set; } = 0.70;

    public int MinPageDimensionPx { get; set; } = 600;
}

public static class TemplateScanAccess
{
    public static bool CanCreateFromScan() => TemplateConvertAccess.CanConvertTemplates();
}
