using System;

namespace Visa2026.E2E.Tests;

internal static class EasyTestCITuning
{
    internal static TimeSpan HostStartupTimeout =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? TimeSpan.FromMinutes(12) : TimeSpan.FromMinutes(4);

    internal static int RunApplicationMaxAttempts =>
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

    internal static TimeSpan LayoutTabSettleDelay =>
        IsTruthy(Environment.GetEnvironmentVariable("CI"))
            ? TimeSpan.FromSeconds(2)
            : TimeSpan.FromMilliseconds(750);

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
