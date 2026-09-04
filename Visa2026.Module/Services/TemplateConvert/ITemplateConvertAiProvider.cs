#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Pluggable mapping assistant (L11). Matching stays local (E-D1); adapters only propose region→token plans.
/// </summary>
public interface ITemplateConvertAiProvider
{
    /// <summary>Config key: <c>None</c>, later <c>AzureOpenAI</c>, <c>xAI</c>, …</summary>
    string Key { get; }

    bool IsEnabled { get; }

    Task<TemplateMappingPlan> ProposeMappingAsync(
        TemplateMappingRequest request,
        CancellationToken cancellationToken = default);

    Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
        TemplateChatTurnRequest request,
        CancellationToken cancellationToken = default);
}