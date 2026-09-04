using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Dynamic <see cref="ApplicationState"/> codes for ministry review legs 1…N.
/// </summary>
public static class ApplicationProfileInstanceProgressLegCodes
{
    public const int MaxLegCount = 5;

    /// <summary>
    /// First-leg only for new progress ("Ministrlige iberilen"). Legs 2–5 STARTED are legacy rows only.
    /// </summary>
    public static string ReviewStarted(int leg) => $"{leg}_REVIEW_STARTED";
    public static string ReviewApproved(int leg) => $"{leg}_REVIEW_APPROVED";
    public static string ReviewRejected(int leg) => $"{leg}_REVIEW_REJECTED";
    public static string AtMinistry(int leg) => $"AT_THE_MINISTERY_{leg}";

    public static bool IsLegacyReviewStartedCode(string? stateCode) =>
        IsMinistryReviewStartedStateCode(stateCode)
        && TryParseMinistryLegFromStateCode(stateCode, out var leg)
        && leg > 1;

    public static bool IsActiveMinistryStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        if (trimmed.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase))
            return true;

        // First-leg started is active workflow; later STARTED codes are legacy-only.
        return string.Equals(trimmed, ReviewStarted(1), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsReviewStateCode(string? stateCode) =>
        TryParseMinistryLegFromStateCode(stateCode, out _);

    public static bool IsReviewRejectedStateCode(string? stateCode) =>
        TryParseMinistryLegFromStateCode(stateCode, out _)
        && stateCode!.EndsWith("_REJECTED", StringComparison.OrdinalIgnoreCase);

    public static bool IsMinistryReviewStartedStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        return stateCode.Trim().EndsWith("_REVIEW_STARTED", StringComparison.OrdinalIgnoreCase)
            && TryParseMinistryLegFromStateCode(stateCode, out _);
    }

    public static bool IsMinistryDecisionStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        return trimmed.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseMinistryLegFromStateCode(string? stateCode, out int leg)
    {
        leg = 0;
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        var underscore = trimmed.IndexOf('_', StringComparison.Ordinal);
        if (underscore <= 0)
            return false;

        if (!int.TryParse(trimmed.AsSpan(0, underscore), out leg))
            return false;

        if (leg < 1 || leg > MaxLegCount)
            return false;

        return trimmed.EndsWith("_REVIEW_STARTED", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseMinistryLegFromLocationCode(string? locationCode, out int leg)
    {
        leg = 0;
        if (string.IsNullOrWhiteSpace(locationCode))
            return false;

        const string prefix = "AT_THE_MINISTERY_";
        var trimmed = locationCode.Trim();
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(trimmed.AsSpan(prefix.Length), out leg)
            && leg is >= 1 and <= MaxLegCount;
    }

    /// <summary>Active review states for a leg. Leg 1 includes STARTED; legs 2+ are approval/rejection only.</summary>
    public static IReadOnlyList<string> GetReviewStateCodesForLeg(int leg)
    {
        if (leg == 1)
        {
            return
            [
                ReviewStarted(1),
                ReviewApproved(1),
                ReviewRejected(1)
            ];
        }

        return
        [
            ReviewApproved(leg),
            ReviewRejected(leg)
        ];
    }

    public static IEnumerable<string> GetReviewStateCodesUpToLegCount(int legCount)
    {
        for (var leg = 1; leg <= legCount; leg++)
        {
            foreach (var code in GetReviewStateCodesForLeg(leg))
                yield return code;
        }
    }

    public static IEnumerable<string> GetMinistryLocationCodesUpToLegCount(int legCount)
    {
        for (var leg = 1; leg <= legCount; leg++)
            yield return AtMinistry(leg);
    }

    public static IEnumerable<string> GetReviewRejectedStateCodesUpToLegCount(int legCount)
    {
        for (var leg = 1; leg <= legCount; leg++)
            yield return ReviewRejected(leg);
    }
}