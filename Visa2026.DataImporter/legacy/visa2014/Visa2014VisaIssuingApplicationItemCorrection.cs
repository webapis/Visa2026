namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Retired alias — use <see cref="Visa2014VisaIssuingApplicationProfileInstanceCorrection"/>.
/// </summary>
internal static class Visa2014VisaIssuingApplicationItemCorrection
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose) =>
        Visa2014VisaIssuingApplicationProfileInstanceCorrection.RunCommandAsync(args, verbose);
}
