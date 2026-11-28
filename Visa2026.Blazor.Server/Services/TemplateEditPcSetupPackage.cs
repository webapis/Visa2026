using System.IO.Compression;
using System.Text;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// One zip an officer downloads and runs on this PC: Office trust, the Edit opener, and the Word/Excel Sync add-in.
/// </summary>
public static class TemplateEditPcSetupPackage
{
    public const string FileName = "Visa2026-TemplateEdit-Setup.zip";

    private static readonly string[] Entries =
    {
        "Install.cmd",
        "Install-Visa2026TemplateEdit.ps1",
        "Set-Visa2026TemplateEditOfficeTrust.ps1",
        "Install-Visa2026OfficeSyncAddin.ps1",
        "Invoke-Visa2026TemplateEdit.ps1",
        "office-sync/Visa2026.OfficeSync.dll",
    };

    public static bool TryNormalizeHost(string? host, out string normalized)
    {
        normalized = (host ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > 253)
        {
            return false;
        }

        foreach (var c in normalized)
        {
            var ok = c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '-';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    public static byte[] Create(string serverHost)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "template-edit-setup");
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entryName in Entries)
            {
                var path = Path.Combine(folder, entryName.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Template edit setup file is missing.", entryName);
                }

                var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
                using var input = File.OpenRead(path);
                using var output = entry.Open();
                input.CopyTo(output);
            }

            var hostEntry = zip.CreateEntry("server-host.txt", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(hostEntry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(serverHost);
            }
        }

        return memory.ToArray();
    }
}
