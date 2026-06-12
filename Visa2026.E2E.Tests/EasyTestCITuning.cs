using System;

namespace Visa2026.E2E.Tests;

internal static class EasyTestCITuning
{
    internal static TimeSpan HostStartupTimeout =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? TimeSpan.FromMinutes(12) : TimeSpan.FromMinutes(4);

    internal static int RunApplicationMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 3 : 1;

    /// <summary>
    /// How many times to (re)launch the Blazor host process before giving up, so an
    /// intermittent startup crash (DevExpress TypesInfo / WebApi OData warm-up race)
    /// is recovered on a fresh process. Each attempt fails fast when the host exits.
    /// </summary>
    internal static int HostStartMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 3 : 1;

    internal static int FormFieldMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 30 : 10;

    internal static TimeSpan FormFieldRetryDelay =>
        IsTruthy(Environment.GetEnvironmentVariable("CI"))
            ? TimeSpan.FromMilliseconds(1000)
            : TimeSpan.FromMilliseconds(500);

    internal static TimeSpan PassportDetailOpenTimeout =>
        IsTruthy(Environment.GetEnvironmentVariable("CI"))
            ? TimeSpan.FromSeconds(90)
            : TimeSpan.FromSeconds(45);

    internal static int NestedListActionMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 30 : 15;

    /// <summary>
    /// How many times to (re-)activate the Passports tab and click nested <c>New</c>,
    /// re-verifying the passport detail actually opened between clicks. Bounded so the
    /// worst case stays within <see cref="PassportDetailOpenTimeout"/>-scale time.
    /// </summary>
    internal static int NestedNewClickMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 6 : 3;

    /// <summary>Per-click wait for the nested passport detail to open before re-clicking.</summary>
    internal static TimeSpan NestedNewProbeTimeout =>
        IsTruthy(Environment.GetEnvironmentVariable("CI"))
            ? TimeSpan.FromSeconds(15)
            : TimeSpan.FromSeconds(8);

    internal static TimeSpan LayoutTabSettleDelay =>
        IsTruthy(Environment.GetEnvironmentVariable("CI"))
            ? TimeSpan.FromSeconds(2)
            : TimeSpan.FromMilliseconds(750);

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
