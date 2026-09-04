#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public sealed class TemplateConvertChatServiceRequest
{
    public required string Message { get; init; }

    public required TemplateMappingPlan CurrentPlan { get; init; }

    public required IReadOnlyList<DocumentExtractRegion> Regions { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    /// <summary>Current converted draft. Compared after reject to prove Q11 / Q12 byte identity.</summary>
    public required byte[] CurrentDraftContent { get; init; }
}

public sealed class TemplateConvertChatServiceResult
{
    public required bool Accepted { get; init; }

    public required string ReplyText { get; init; }

    public ChatRejectReason? RejectReason { get; init; }

    /// <summary>Sanitized plan when accepted; otherwise the unchanged current plan.</summary>
    public required TemplateMappingPlan Plan { get; init; }

    public IReadOnlyList<string> SanitizerDropped { get; init; } = Array.Empty<string>();
}

public interface ITemplateConvertChatService
{
    Task<TemplateConvertChatServiceResult> ApplyAsync(
        TemplateConvertChatServiceRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Preview chat orchestration (E9): classify L8 locally, call the provider only for mapping intents,
/// sanitize any accepted plan. The host applies the plan through
/// <see cref="ITemplateConvertOrchestrator.ApplyPlanAsync"/> so diff / residual / validate stay one path.
/// </summary>
public sealed class TemplateConvertChatService : ITemplateConvertChatService
{
    private readonly ITemplateConvertAiProvider _provider;
    private readonly ITemplateMappingPlanSanitizer _sanitizer;

    public TemplateConvertChatService(
        ITemplateConvertAiProvider provider,
        ITemplateMappingPlanSanitizer sanitizer)
    {
        _provider = provider;
        _sanitizer = sanitizer;
    }

    public async Task<TemplateConvertChatServiceResult> ApplyAsync(
        TemplateConvertChatServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CurrentPlan);
        ArgumentNullException.ThrowIfNull(request.PlaceholderSet);
        ArgumentNullException.ThrowIfNull(request.CurrentDraftContent);

        var message = (request.Message ?? string.Empty).Trim();
        var intent = TemplateConvertChatIntentClassifier.Classify(message);

        if (intent == TemplateConvertChatIntent.OutOfScopeContentEdit)
        {
            return Reject(
                request.CurrentPlan,
                TemplateConvertChatIntentClassifier.OutOfScopeReply,
                ChatRejectReason.OutOfScopeContentEdit);
        }

        var allowedTokens = request.PlaceholderSet.Allowed
            .Select(static e => new AllowedToken(e.ShortCode, e.LabelEn, e.Scope))
            .ToList();

        var providerResult = await _provider.ApplyChatAdjustmentAsync(
            new TemplateChatTurnRequest
            {
                Message = message,
                CurrentPlan = request.CurrentPlan,
                Regions = request.Regions,
                AllowedTokens = allowedTokens,
                PlaceholderSetFingerprint = request.PlaceholderSet.Fingerprint,
            },
            cancellationToken).ConfigureAwait(false);

        if (!providerResult.Accepted || providerResult.UpdatedPlan == null)
        {
            return Reject(
                request.CurrentPlan,
                providerResult.ReplyText,
                providerResult.RejectReason ?? ChatRejectReason.NotUnderstood);
        }

        var sanitized = _sanitizer.Sanitize(
            providerResult.UpdatedPlan,
            request.PlaceholderSet,
            request.Regions,
            out var dropped);

        if (sanitized.Substitutions.Count == 0 && sanitized.Loops.Count == 0)
        {
            return Reject(
                request.CurrentPlan,
                string.IsNullOrWhiteSpace(providerResult.ReplyText)
                    ? "No mapping change could be applied within this profile's placeholder set."
                    : providerResult.ReplyText,
                ChatRejectReason.TokenNotInProfileSet,
                dropped);
        }

        return new TemplateConvertChatServiceResult
        {
            Accepted = true,
            ReplyText = providerResult.ReplyText,
            RejectReason = null,
            Plan = sanitized,
            SanitizerDropped = dropped,
        };
    }

    private static TemplateConvertChatServiceResult Reject(
        TemplateMappingPlan plan,
        string reply,
        ChatRejectReason reason,
        IReadOnlyList<string>? dropped = null) =>
        new()
        {
            Accepted = false,
            ReplyText = reply,
            RejectReason = reason,
            Plan = plan,
            SanitizerDropped = dropped ?? Array.Empty<string>(),
        };
}