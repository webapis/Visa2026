using System;

namespace Visa2026.E2E.Tests;

internal static class EasyTestCITuning
{
    internal static TimeSpan HostStartupTimeout =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? TimeSpan.FromMinutes(12) : TimeSpan.FromMinutes(4);

    internal static int RunApplicationMaxAttempts =>
        IsTruthy(Environment.GetEnvironmentVariable("CI")) ? 3 : 1;

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
