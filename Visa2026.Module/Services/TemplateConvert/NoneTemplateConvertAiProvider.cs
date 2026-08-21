#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Default adapter. Deterministic matching already ran locally; this returns that plan unchanged
/// and refuses chat adjustments. Phase 0 ships with this alone (E8 / Q14).
/// </summary>
public sealed class NoneTemplateConvertAiProvider : ITemplateConvertAiProvider
{
    public const string ProviderKey = "None";

    public string Key => ProviderKey;

    /// <summary>
    /// Always false. The convert UI may still be enabled - "AI off" means no cloud assistance,
    /// not "convert is unavailable".
    /// </summary>
    public bool IsEnabled => false;

    public Task<TemplateMappingPlan> ProposeMappingAsync(
        TemplateMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var plan = TemplateMappingPlan.FromDeterministic(
            request.PreMatched,
            rationale: "Deterministic local matches only - AI assistance is turned off.");

        return Task.FromResult(plan);
    }

    public Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
        TemplateChatTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new TemplateChatTurnResult(
            Accepted: false,
            ReplyText: "AI assistance is turned off. Adjust the mapping by converting again with a different file, or edit the template in Word or Excel after save.",
            UpdatedPlan: null,
            RejectReason: ChatRejectReason.NotUnderstood));
    }
}