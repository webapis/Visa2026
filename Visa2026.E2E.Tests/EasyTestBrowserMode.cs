using System;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Edge visibility for EasyTest: headed on local dev, headless on CI unless overridden.
/// </summary>
internal static class EasyTestBrowserMode
{
    /// <summary>
    /// Headless when <c>CI=true</c> or <c>VISA2026_E2E_HEADLESS=true</c>.
    /// Force headed with <c>VISA2026_E2E_HEADED=true</c> (wins over CI/headless).
    /// </summary>
    internal static bool RunHeadless
    {
        get
        {
            if (IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_HEADED")))
                return false;

            if (IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_HEADLESS")))
                return true;

            return IsTruthy(Environment.GetEnvironmentVariable("CI"));
        }
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
