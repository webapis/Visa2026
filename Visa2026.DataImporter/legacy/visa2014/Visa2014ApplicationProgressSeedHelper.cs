using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Detects <see cref="ApplicationProgressInitializer"/> seed rows vs synthesized migration steps.
/// </summary>
internal static class Visa2014ApplicationProgressSeedHelper
{
    public const string InitialStateCode = "IS_BEING_PREPARED";
    public const string PrepareStepCode = "prepare";

    public static bool IsInitializerSeed(ApplicationProgress progress)
    {
        if (progress.State == null)
            return false;

        if (!string.Equals(progress.State.Code, InitialStateCode, StringComparison.OrdinalIgnoreCase))
            return false;

        return string.IsNullOrWhiteSpace(progress.Description);
    }

    public static bool IsPrepareSyntheticStep(Dictionary<string, object?> row) =>
        string.Equals(row.GetValueOrDefault("State") as string, InitialStateCode, StringComparison.OrdinalIgnoreCase)
        && string.Equals(ExtractStepCode(row), PrepareStepCode, StringComparison.OrdinalIgnoreCase);

    public static string? ExtractStepCode(Dictionary<string, object?> row)
    {
        var syntheticKey = row.GetValueOrDefault("_syntheticStepKey") as string;
        if (string.IsNullOrWhiteSpace(syntheticKey))
            return null;

        var colon = syntheticKey.LastIndexOf(':');
        return colon >= 0 && colon < syntheticKey.Length - 1
            ? syntheticKey[(colon + 1)..]
            : null;
    }

    public static bool DatesMatch(DateTime left, DateTime right) =>
        left.Date == right.Date;
}
