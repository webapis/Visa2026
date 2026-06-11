using System.IO;

namespace Visa2026.E2E.Tests;

internal static class EasyTestWebDriverPath
{
    internal static string Resolve()
    {
        string[] candidates =
        [
            Path.Combine(Environment.CurrentDirectory, ".webdrivers"),
            Path.Combine(Environment.CurrentDirectory, @"..\..\..\.webdrivers"),
            Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Visa2026.E2E.Tests\.webdrivers"),
        ];

        foreach (string path in candidates)
        {
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(Path.Combine(fullPath, "msedgedriver.exe")))
                return fullPath;
        }

        return string.Empty;
    }
}
