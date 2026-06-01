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

    public static IReadOnlyCollection<BoStateAppearance> ApplicationProgressRowStates { get; } =
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
            Entry(ApplicationProgressStateCodes.IsBeingPrepared, "LemonChiffon", "DarkGoldenrod", "#fef9c3", "#854d0e", 110),
            Entry(ApplicationProgressStateCodes.Review1Started, "LightSteelBlue", "DodgerBlue", "#dbeafe", "#1e40af", 110),
            Entry(ApplicationProgressStateCodes.Review2Started, "SkyBlue", "DeepSkyBlue", "#bae6fd", "#0369a1", 110),
            Entry(ApplicationProgressStateCodes.Review1Approved, "Aquamarine", "SeaGreen", "#a7f3d0", "#047857", 90),
            Entry(ApplicationProgressStateCodes.Review2Approved, "MintCream", "DarkGreen", "#ecfccb", "#3f6212", 90),
            Entry(ApplicationProgressStateCodes.Review1Rejected, "PeachPuff", "OrangeRed", "#ffedd5", "#c2410c", 310),
            Entry(ApplicationProgressStateCodes.Review2Rejected, "NavajoWhite", "Chocolate", "#fed7aa", "#9a3412", 310),
            Entry(ApplicationProgressStateCodes.ProcessStarted, "CornflowerBlue", "RoyalBlue", "#93c5fd", "#1d4ed8", 110),
            Entry(ApplicationProgressStateCodes.ProcessCancelled, "RosyBrown", "Firebrick", "#fecaca", "#991b1b", 310),
            Entry(ApplicationProgressStateCodes.ProcessRejected, "Salmon", "IndianRed", "#fca5a5", "#b91c1c", 310),
            Entry(ApplicationProgressStateCodes.ProcessIssued, "SpringGreen", "DarkGreen", "#86efac", "#15803d", 60),
            Entry(ApplicationProgressLocationCodes.AtOffice, "Cornsilk", "Peru", "#fef3c7", "#92400e", 110),
            Entry(ApplicationProgressLocationCodes.AtMinistry1, "CornflowerBlue", "MediumBlue", "#c7d2fe", "#4338ca", 110),
            Entry(ApplicationProgressLocationCodes.AtMinistry2, "LightSteelBlue", "SlateBlue", "#a5b4fc", "#3730a3", 110),
            Entry(ApplicationProgressLocationCodes.AtMigrationService, "LightCyan", "Teal", "#67e8f9", "#0e7490", 110),
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
