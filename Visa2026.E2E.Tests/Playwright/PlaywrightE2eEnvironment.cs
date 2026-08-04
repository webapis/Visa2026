using System;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>
/// Playwright E2E target configuration.
/// <list type="bullet">
/// <item><c>VISA2026_E2E_TARGET</c> — <c>Local</c> (default) or <c>Staging</c></item>
/// <item><c>VISA2026_E2E_BASE_URL</c> — override base URL (required for Staging when not using default)</item>
/// <item><c>VISA2026_E2E_USER</c> / <c>VISA2026_E2E_PASSWORD</c> — officer credentials</item>
/// </list>
/// </summary>
internal static class PlaywrightE2eEnvironment
{
    private const string DefaultLocalBaseUrl = EasyTestHostEnvironment.BaseUrl;
    private const string DefaultStagingBaseUrl = "https://10.100.128.25:8080";

    internal static PlaywrightE2eTarget Target => ParseTarget(Environment.GetEnvironmentVariable("VISA2026_E2E_TARGET"));

    internal static bool IsLocal => Target == PlaywrightE2eTarget.Local;

    internal static bool IsStaging => Target == PlaywrightE2eTarget.Staging;

    internal static string BaseUrl
    {
        get
        {
            string? overrideUrl = Environment.GetEnvironmentVariable("VISA2026_E2E_BASE_URL");
            if (!string.IsNullOrWhiteSpace(overrideUrl))
                return overrideUrl.Trim().TrimEnd('/');

            return IsStaging ? DefaultStagingBaseUrl : DefaultLocalBaseUrl;
        }
    }

    internal static string UserName =>
        Environment.GetEnvironmentVariable("VISA2026_E2E_USER")
        ?? Module.DatabaseUpdate.E2ETestLoginValues.StandardUserName;

    internal static string Password =>
        Environment.GetEnvironmentVariable("VISA2026_E2E_PASSWORD")
        ?? Module.DatabaseUpdate.E2ETestLoginValues.StandardUserPassword;

    internal static bool IgnoreHttpsErrors =>
        IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_IGNORE_HTTPS_ERRORS"))
        || IsStaging;

    private static PlaywrightE2eTarget ParseTarget(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PlaywrightE2eTarget.Local;

        if (string.Equals(value, "staging", StringComparison.OrdinalIgnoreCase))
            return PlaywrightE2eTarget.Staging;

        if (string.Equals(value, "local", StringComparison.OrdinalIgnoreCase))
            return PlaywrightE2eTarget.Local;

        throw new InvalidOperationException(
            $"Unknown VISA2026_E2E_TARGET '{value}'. Use Local or Staging.");
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}
