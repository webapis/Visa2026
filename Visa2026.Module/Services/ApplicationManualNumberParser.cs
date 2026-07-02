using System.Text.RegularExpressions;

namespace Visa2026.Module.Services;

/// <summary>
/// Parses legacy VISA2014 <c>IRegistration_Data.ManualApplicationNumber</c> values into
/// <see cref="BusinessObjects.Application"/> components while preserving the display string.
/// </summary>
public static partial class ApplicationManualNumberParser
{
    /// <param name="manual">Legacy manual number (e.g. <c>7/-1105</c>, <c>6/-6/-1098</c>).</param>
    /// <param name="fullNumber">Trimmed display value to store in <c>FullApplicationNumber</c>.</param>
    /// <param name="prefix">Leading segment before the sequence (often month).</param>
    /// <param name="applicationNumber">Numeric sequence segment for <c>ApplicationNumber</c>.</param>
    public static void Parse(
        string? manual,
        out string fullNumber,
        out string? prefix,
        out string? applicationNumber)
    {
        fullNumber = string.IsNullOrWhiteSpace(manual) ? "" : manual.Trim();
        prefix = null;
        applicationNumber = null;

        if (string.IsNullOrWhiteSpace(fullNumber))
            return;

        var tripleMinus = TripleMinusPattern().Match(fullNumber);
        if (tripleMinus.Success)
        {
            prefix = tripleMinus.Groups[1].Value;
            applicationNumber = tripleMinus.Groups[3].Value;
            return;
        }

        var doubleMinus = DoubleMinusPattern().Match(fullNumber);
        if (doubleMinus.Success)
        {
            prefix = doubleMinus.Groups[1].Value;
            applicationNumber = doubleMinus.Groups[2].Value;
            return;
        }

        var tripleSlash = TripleSlashPattern().Match(fullNumber);
        if (tripleSlash.Success)
        {
            prefix = tripleSlash.Groups[1].Value;
            applicationNumber = tripleSlash.Groups[3].Value;
            return;
        }

        var slash = fullNumber.IndexOf('/');
        if (slash < 0)
        {
            applicationNumber = fullNumber;
            return;
        }

        prefix = fullNumber[..slash].Trim();
        var suffix = fullNumber[(slash + 1)..].Trim();
        if (suffix.StartsWith("-", StringComparison.Ordinal))
            suffix = suffix[1..].Trim();
        applicationNumber = suffix;
    }

    [GeneratedRegex(@"^(\d+)/-(\d+)/-(\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex TripleMinusPattern();

    [GeneratedRegex(@"^(\d+)/-(\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex DoubleMinusPattern();

    [GeneratedRegex(@"^(\d+)/(\d+)/(\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex TripleSlashPattern();
}
