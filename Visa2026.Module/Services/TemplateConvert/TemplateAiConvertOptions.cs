using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public sealed class TemplateAiConvertOptions
{
    public const string SectionName = "TemplateAiConvert";

    /// <summary>Master switch. When false no convert entry point renders anywhere.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Instance-side entry (Resminamalar action row). Product decision L13 makes this opt-in;
    /// until the per-user switch has a home it is a deployment flag.
    /// </summary>
    public bool ShowInstanceEntry { get; set; }

    /// <summary>Rejects oversized uploads before any parsing (default 20 MB, same as the wizard).</summary>
    public long MaxUploadBytes { get; set; } = 20_971_520;

    /// <summary>Provider key. <c>None</c> is the Phase 0 default; <c>AzureOpenAI</c> is the first real adapter (E10).</summary>
    public string Provider { get; set; } = NoneTemplateConvertAiProvider.ProviderKey;

    /// <summary>Provider call timeout. Applied around ProposeMapping / chat completion HTTP calls.</summary>
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>Upper bound on document extract characters sent to a cloud adapter.</summary>
    public int MaxDocumentCharacters { get; set; } = 50_000;

    /// <summary>When true, identifier-shaped region previews are masked before any provider sees them (E-D1).</summary>
    public bool RedactIdentifiersInExtract { get; set; } = true;

    /// <summary>Azure OpenAI settings. ApiKey must come from the environment on Demo/prod - never commit it.</summary>
    public TemplateAiConvertAzureOpenAiOptions AzureOpenAI { get; set; } = new();
}

public sealed class TemplateAiConvertAzureOpenAiOptions
{
    /// <summary>Resource endpoint, e.g. https://contoso.openai.azure.com/</summary>
    public string? Endpoint { get; set; }

    /// <summary>Deployment name (not the model id).</summary>
    public string? Deployment { get; set; }

    public string ApiVersion { get; set; } = "2024-10-21";

    /// <summary>
    /// Prefer the environment variable <c>TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY</c>
    /// (or <c>TemplateAiConvert__AzureOpenAI__ApiKey</c>). Do not store production keys in appsettings.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Convert writes a profile template and its linked master report, so it needs both write rights -
/// the Resminamalar "Edit template" check alone is not enough.
/// </summary>
public static class TemplateConvertAccess
{
    public static bool CanConvertTemplates() =>
        UserReportTemplateEditAccess.CanEditTemplates()
        && SecuritySystem.IsGranted(
            new PermissionRequest(typeof(ApplicationProfileTemplate), SecurityOperations.Write));
}