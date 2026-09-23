using System;
using System.Text;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Detects the DevExpress Office File API evaluation banner in a preview PDF.</summary>
internal static class OfficePreviewEvaluationStamp
{
    private static readonly string[] Markers =
    {
        "for evaluation purposes only",
        "evaluation warning",
        "please register an existing license",
        "devexpress product libraries",
    };

    internal static bool ContainsStamp(byte[]? pdf)
    {
        if (pdf == null || pdf.Length < 8)
            return false;

        var text = Encoding.Latin1.GetString(pdf);
        foreach (var marker in Markers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
