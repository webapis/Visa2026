using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Appearance;

public readonly record struct BoStateAppearance(
    string StateCode,
    string BackColor,
    string FontColor,
    string CssBackgroundHex,
    string CssTextHex,
    int DisplayPriority)
{
    public string RowCssClass => BoStateAppearanceColors.ToRowCssClass(StateCode);
}

/// <summary>
/// Row/column colors keyed by workflow state codes (<see cref="docs/BO_STATE_COLORS.md"/>).
/// </summary>
public static class BoStateAppearanceColors
{
    private static readonly IReadOnlyDictionary<string, BoStateAppearance> Registry = BuildRegistry();

    public static IReadOnlyCollection<BoStateAppearance> ApplicationProfileInstanceProgressRowStates { get; } =
        Registry.Values.ToArray();

    public static bool TryGet(string? stateCode, out BoStateAppearance appearance)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
        {
            appearance = default;
            return false;
        }

        return Registry.TryGetValue(stateCode.Trim(), out appearance);
    }

    public static string ToRowCssClass(string stateCode) =>
        $"visa-progress-row--state-{stateCode.Trim()}";

    private static IReadOnlyDictionary<string, BoStateAppearance> BuildRegistry()
    {
        var entries = new[]
        {
            Entry(ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, "LemonChiffon", "DarkGoldenrod", "#fef9c3", "#854d0e", 110),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review1Started, "LightSteelBlue", "DodgerBlue", "#dbeafe", "#1e40af", 110),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review2Started, "SkyBlue", "DeepSkyBlue", "#bae6fd", "#0369a1", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(3), "PowderBlue", "SteelBlue", "#bfdbfe", "#1e3a8a", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(4), "LightBlue", "RoyalBlue", "#dbeafe", "#1d4ed8", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(5), "AliceBlue", "MidnightBlue", "#eff6ff", "#172554", 110),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review1Approved, "Aquamarine", "SeaGreen", "#a7f3d0", "#047857", 90),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review2Approved, "MintCream", "DarkGreen", "#ecfccb", "#3f6212", 90),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(3), "Honeydew", "ForestGreen", "#d9f99d", "#365314", 90),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(4), "PaleGreen", "DarkOliveGreen", "#bbf7d0", "#166534", 90),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(5), "LightGreen", "Green", "#86efac", "#14532d", 90),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review1Rejected, "PeachPuff", "OrangeRed", "#ffedd5", "#c2410c", 310),
            Entry(ApplicationProfileInstanceProgressStateCodes.Review2Rejected, "NavajoWhite", "Chocolate", "#fed7aa", "#9a3412", 310),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(3), "MistyRose", "Crimson", "#fecdd3", "#be123c", 310),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(4), "LightCoral", "DarkRed", "#fca5a5", "#7f1d1d", 310),
            Entry(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(5), "IndianRed", "Maroon", "#f87171", "#450a0a", 310),
            Entry(ApplicationProfileInstanceProgressStateCodes.ProcessStarted, "CornflowerBlue", "RoyalBlue", "#93c5fd", "#1d4ed8", 110),
            Entry(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, "RosyBrown", "Firebrick", "#fecaca", "#991b1b", 310),
            Entry(ApplicationProfileInstanceProgressStateCodes.ProcessRejected, "Salmon", "IndianRed", "#fca5a5", "#b91c1c", 310),
            Entry(ApplicationProfileInstanceProgressStateCodes.ProcessIssued, "SpringGreen", "DarkGreen", "#86efac", "#15803d", 60),
            Entry(ApplicationProfileInstanceProgressLocationCodes.AtOffice, "Cornsilk", "Peru", "#fef3c7", "#92400e", 110),
            Entry(ApplicationProfileInstanceProgressLocationCodes.AtMinistry1, "CornflowerBlue", "MediumBlue", "#c7d2fe", "#4338ca", 110),
            Entry(ApplicationProfileInstanceProgressLocationCodes.AtMinistry2, "LightSteelBlue", "SlateBlue", "#a5b4fc", "#3730a3", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.AtMinistry(3), "Plum", "Purple", "#ddd6fe", "#5b21b6", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.AtMinistry(4), "Thistle", "Indigo", "#c4b5fd", "#4338ca", 110),
            Entry(ApplicationProfileInstanceProgressLegCodes.AtMinistry(5), "Lavender", "DarkSlateBlue", "#e9d5ff", "#312e81", 110),
            Entry(ApplicationProfileInstanceProgressLocationCodes.AtMigrationService, "LightCyan", "Teal", "#67e8f9", "#0e7490", 110),
            Entry(ApplicationProfileInstanceProgressSlaCodes.Warning, "Moccasin", "DarkOrange", "#fef08a", "#a16207", 320),
            Entry(ApplicationProfileInstanceProgressSlaCodes.Overdue, "LightSalmon", "DarkRed", "#fecaca", "#991b1b", 330),
        };

        return entries.ToDictionary(e => e.StateCode, e => e, StringComparer.OrdinalIgnoreCase);
    }

    private static BoStateAppearance Entry(
        string stateCode,
        string backColor,
        string fontColor,
        string cssBg,
        string cssText,
        int displayPriority) =>
        new(stateCode, backColor, fontColor, cssBg, cssText, displayPriority);
}
