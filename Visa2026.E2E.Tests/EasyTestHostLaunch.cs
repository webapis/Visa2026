using System.IO;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Command-line args for <see cref="Visa2026.Blazor.Server.Program"/> when EasyTest launches the built <c>.exe</c>.
/// <c>--launch-profile</c> only applies to <c>dotnet run</c>; the standalone host ignores it and would listen on :5000.
/// </summary>
internal static class EasyTestHostLaunch
{
    private const string HostConfiguration = "EasyTest";

    /// <summary>Kestrel URL + <c>appsettings.EasyTest.json</c> (connection string, test DB).</summary>
    public const string HostArguments = "--urls http://localhost:5050 --environment EasyTest";

    public const string UpdateDatabaseArguments = "--updateDatabase --silent --environment EasyTest";

    /// <summary>
    /// EasyTest must launch the built <c>.exe</c> (not the project folder / <c>dotnet run</c>) so
    /// <see cref="HostArguments"/> are honored and Kestrel binds to <c>:5050</c>.
    /// </summary>
    internal static string ResolveHostExecutable(string blazorServerProjectPath)
    {
        string[] candidates =
        [
            Path.Combine(blazorServerProjectPath, "bin", HostConfiguration, "net8.0", "Visa2026.Blazor.Server.exe"),
            Path.Combine(blazorServerProjectPath, "bin", "Debug", "net8.0", "Visa2026.Blazor.Server.exe"),
            Path.Combine(blazorServerProjectPath, "bin", "Release", "net8.0", "Visa2026.Blazor.Server.exe"),
        ];

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
                return fullPath;
        }

        throw new FileNotFoundException(
            "Visa2026.Blazor.Server.exe not found for EasyTest. Run: dotnet build Visa2026.slnx -c EasyTest",
            candidates[0]);
    }
}
