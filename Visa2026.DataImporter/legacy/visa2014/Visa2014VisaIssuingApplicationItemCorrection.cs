using Visa2026.Blazor.Server.Services.Migration;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Retired (Phase B): Visa.IssuingApplicationItem was removed.
/// Issuing link is Visa.IssuingApplicationProfileInstance only.
/// </summary>
internal static class Visa2014VisaIssuingApplicationItemCorrection
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        _ = args;
        _ = verbose;
        Console.Error.WriteLine(
            "ERR --correct-visa2014-issuing-application-item is retired (Phase B hard-remove). " +
            "Visa.IssuingApplicationItem no longer exists; use IssuingApplicationProfileInstance.");
        return Task.FromResult(1);
    }
}