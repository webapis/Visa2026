#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface ITemplateScanClarificationService
{
    Task<ScanClarificationTurnResult> ApplyAsync(
        ScanClarificationTurnRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// S4 clarification orchestration: classify S8 locally, call vision provider only for mapping intents,
/// merge accepted proposals through <see cref="IScanFieldPlanMerger"/>.
/// </summary>
public sealed class TemplateScanClarificationService : ITemplateScanClarificationService
{
    private readonly ITemplateScanAiProvider _provider;
    private readonly IScanFieldPlanMerger _merger;

    public TemplateScanClarificationService(
        ITemplateScanAiProvider provider,
        IScanFieldPlanMerger merger)
    {
        _provider = provider;
        _merger = merger;
    }

    public async Task<ScanClarificationTurnResult> ApplyAsync(
        ScanClarificationTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CurrentPlan);
        ArgumentNullException.ThrowIfNull(request.PlaceholderSet);

        var message = (request.OfficerMessage ?? string.Empty).Trim();
        var intent = TemplateScanChatIntentClassifier.Classify(message);

        if (intent == TemplateScanChatIntent.OutOfScopeContentEdit)
        {
            return Reject(
                request.CurrentPlan,
                TemplateScanChatIntentClassifier.OutOfScopeReply,
                ScanClarificationRejectReason.OutOfScopeContentEdit);
        }

        if (!_provider.IsEnabled)
        {
            return Reject(
                request.CurrentPlan,
                "AI assistance is turned off. Review the field list locally or enable a vision provider in configuration.",
                ScanClarificationRejectReason.NotUnderstood);
        }

        var providerResult = await _provider.ClarifyAsync(
            new ScanClarificationRequest
            {
                OfficerMessage = message,
                CurrentPlan = request.CurrentPlan,
                Playbook = request.Playbook,
                PlaceholderSet = request.PlaceholderSet,
            },
            cancellationToken).ConfigureAwait(false);

        if (!providerResult.Accepted)
        {
            return Reject(
                request.CurrentPlan,
                providerResult.ReplyText,
                ScanClarificationRejectReason.NotUnderstood);
        }

        var merged = _merger.Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = providerResult.Plan,
            PlaceholderSet = request.PlaceholderSet,
            ScanKind = request.ScanKind,
            ValueHints = request.ValueHints,
        });

        if (!merged.HasMappedFields)
        {
            return Reject(
                request.CurrentPlan,
                string.IsNullOrWhiteSpace(providerResult.ReplyText)
                    ? "No mapping change could be applied within this profile's placeholder set."
                    : providerResult.ReplyText,
                ScanClarificationRejectReason.TokenNotInProfileSet);
        }

        if (ScanFieldPlanComparer.IsEquivalent(request.CurrentPlan, merged))
        {
            return Reject(
                request.CurrentPlan,
                string.IsNullOrWhiteSpace(providerResult.ReplyText)
                    ? "That did not change the field plan. Try naming a specific label or token."
                    : providerResult.ReplyText,
                ScanClarificationRejectReason.NoMappingChange);
        }

        return new ScanClarificationTurnResult
        {
            Accepted = true,
            ReplyText = providerResult.ReplyText,
            RejectReason = null,
            Plan = merged,
        };
    }

    private static ScanClarificationTurnResult Reject(
        ScanFieldPlan plan,
        string reply,
        ScanClarificationRejectReason reason) =>
        new()
        {
            Accepted = false,
            ReplyText = reply,
            RejectReason = reason,
            Plan = plan,
        };
}

internal static class ScanFieldPlanComparer
{
    internal static bool IsEquivalent(ScanFieldPlan left, ScanFieldPlan right)
    {
        if (left.Fields.Count != right.Fields.Count || left.Gaps.Count != right.Gaps.Count)
            return false;

        var leftFields = left.Fields
            .OrderBy(static f => f.FieldId, StringComparer.Ordinal)
            .ToList();
        var rightFields = right.Fields
            .OrderBy(static f => f.FieldId, StringComparer.Ordinal)
            .ToList();

        for (var i = 0; i < leftFields.Count; i++)
        {
            var a = leftFields[i];
            var b = rightFields[i];
            if (!string.Equals(a.FieldId, b.FieldId, StringComparison.Ordinal)
                || !string.Equals(a.ProposedToken, b.ProposedToken, StringComparison.OrdinalIgnoreCase)
                || a.Confidence != b.Confidence
                || a.Scope != b.Scope
                || !string.Equals(a.LabelText, b.LabelText, StringComparison.Ordinal))
                return false;
        }

        var leftGaps = left.Gaps.OrderBy(static g => g.FieldId, StringComparer.Ordinal).ToList();
        var rightGaps = right.Gaps.OrderBy(static g => g.FieldId, StringComparer.Ordinal).ToList();
        for (var i = 0; i < leftGaps.Count; i++)
        {
            if (!string.Equals(leftGaps[i].FieldId, rightGaps[i].FieldId, StringComparison.Ordinal)
                || !string.Equals(leftGaps[i].LabelText, rightGaps[i].LabelText, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
