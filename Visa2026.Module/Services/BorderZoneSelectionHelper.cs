using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>Backward-compatible alias for border-zone comma-separated values.</summary>
public static class BorderZoneSelectionHelper
{
    public const string NoneValue = CommaSeparatedSelectionHelper.NoneValue;

    public static IReadOnlyList<string> ParseSelected(string? stored) =>
        CommaSeparatedSelectionHelper.ParseSelected(stored);

    public static string FormatSelected(IEnumerable<string>? selected) =>
        CommaSeparatedSelectionHelper.FormatSelected(selected);

    public static bool IsNoneValue(string? stored) =>
        CommaSeparatedSelectionHelper.IsNoneValue(stored);

    public static void ApplyDefaultIfEmpty(Visa? visa)
    {
        if (visa == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(visa.BorderZoneLocation))
        {
            visa.BorderZoneLocation = NoneValue;
        }
    }

    public static void ApplyDefaultIfEmpty(Invitation? invitation)
    {
        if (invitation == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(invitation.BorderZoneLocation))
        {
            invitation.BorderZoneLocation = NoneValue;
        }
    }

    /// <summary>
    /// Invitation value first (same as visa category/period), then the case, then <see cref="NoneValue"/>.
    /// </summary>
    public static string ResolveForIssuedVisa(Invitation? invitation, ApplicationProfileInstance? instance)
    {
        if (!string.IsNullOrWhiteSpace(invitation?.BorderZoneLocation))
            return invitation.BorderZoneLocation.Trim();
        if (!string.IsNullOrWhiteSpace(instance?.BorderZoneLocation))
            return instance.BorderZoneLocation.Trim();
        return NoneValue;
    }
}
