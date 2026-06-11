namespace Visa2026.Blazor.Server;

/// <summary>
/// When enabled (EasyTest host on :5050 / <c>Visa2026EasyTest</c> DB), disables TabbedMDI layout restore and ephemeral user
/// model differences so logon does not reopen Family Members / other tabs from prior runs.
/// </summary>
internal static class EasyTestHostMode
{
    internal static bool IsEnabled =>
        IsEnvFlagSet("VISA2026_EASYTEST")
        || UsesEasyTestDatabaseConnection()
        || string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "EasyTest",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsEnvFlagSet(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }

    private static bool UsesEasyTestDatabaseConnection()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        return connectionString != null
            && connectionString.Contains("Visa2026EasyTest", StringComparison.OrdinalIgnoreCase);
    }
}
