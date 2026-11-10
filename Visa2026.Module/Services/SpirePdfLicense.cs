using System;
using System.IO;

namespace Visa2026.Module.Services;

/// <summary>
/// Applies a Spire.PDF license before any Spire save (XFA fill, etc.).
/// Document-copies scan preview/pages are built with PdfSharpCore and do not need this.
/// </summary>
public static class SpirePdfLicense
{
    private static bool applied;

    /// <summary>
    /// Reads <c>SPIRE_LICENSEKEY</c> / <c>SPIRE_PDF_LICENSEKEY</c> or
    /// <c>SPIRE_LICENSE_FILE</c> / <c>SPIRE_PDF_LICENSE_FILE</c> once per process.
    /// </summary>
    public static void ApplyFromEnvironment()
    {
        if (applied)
            return;
        applied = true;

        try
        {
            var file = Environment.GetEnvironmentVariable("SPIRE_LICENSE_FILE")
                ?? Environment.GetEnvironmentVariable("SPIRE_PDF_LICENSE_FILE");
            if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
            {
                Spire.Pdf.License.LicenseProvider.SetLicenseFileName(file);
                return;
            }

            var key = Environment.GetEnvironmentVariable("SPIRE_LICENSEKEY")
                ?? Environment.GetEnvironmentVariable("SPIRE_PDF_LICENSEKEY");
            if (!string.IsNullOrWhiteSpace(key))
                Spire.Pdf.License.LicenseProvider.SetLicenseKey(key.Trim());
        }
        catch
        {
            // Missing or invalid license must not block startup; unlicensed Spire still runs.
        }
    }
}