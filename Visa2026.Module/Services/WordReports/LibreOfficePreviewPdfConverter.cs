using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Word/Excel → PDF via LibreOffice headless (MPL-2.0). Used for Resminamalar Preview
/// when DevExpress Office File API would stamp an evaluation banner. Not XAF.
/// </summary>
internal static class LibreOfficePreviewPdfConverter
{
    internal static bool IsAvailable() => !string.IsNullOrWhiteSpace(ResolveSofficePath());

    internal static byte[]? TryConvertToPdf(byte[] officeContent, string fileName)
    {
        var soffice = ResolveSofficePath();
        if (soffice == null || officeContent == null || officeContent.Length == 0)
            return null;

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".docx";

        var work = Path.Combine(Path.GetTempPath(), "visa2026-lo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var inputPath = Path.Combine(work, "input" + ext);
            File.WriteAllBytes(inputPath, officeContent);

            var start = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = "--headless --nologo --nolockcheck --norestore --convert-to pdf --outdir \"" + work + "\" \"" + inputPath + "\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            start.Environment["HOME"] = work;
            ApplySofficeLibraryPath(start);

            using var process = Process.Start(start);
            if (process == null)
                return null;

            if (!process.WaitForExit(120_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return null;
            }

            if (process.ExitCode != 0)
                return null;

            var pdf = Directory.GetFiles(work, "*.pdf").FirstOrDefault();
            if (pdf == null)
                return null;

            var bytes = File.ReadAllBytes(pdf);
            return bytes.Length > 0 ? bytes : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string? ResolveSofficePath()
    {
        var configured = Environment.GetEnvironmentVariable("VISA2026_SOFFICE");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        foreach (var candidate in new[]
        {
            "soffice",
            "libreoffice",
            "/usr/bin/soffice",
            "/usr/bin/libreoffice",
            "/usr/lib/libreoffice/program/soffice",
        })
        {
            if (candidate.Contains('/', StringComparison.Ordinal) || candidate.Contains('\\', StringComparison.Ordinal))
            {
                if (File.Exists(candidate))
                    return candidate;
                continue;
            }

            if (CanStart(candidate))
                return candidate;
        }

        return null;
    }

    private static void ApplySofficeLibraryPath(ProcessStartInfo start)
    {
        const string loProgram = "/usr/lib/libreoffice/program";
        var current = start.Environment.TryGetValue("LD_LIBRARY_PATH", out var existing)
            ? existing
            : Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        start.Environment["LD_LIBRARY_PATH"] = string.IsNullOrWhiteSpace(current)
            ? loProgram
            : loProgram + ":" + current;
    }

    private static bool CanStart(string fileName)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            ApplySofficeLibraryPath(start);
            using var process = Process.Start(start);
            if (process == null)
                return false;
            if (!process.WaitForExit(8_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
