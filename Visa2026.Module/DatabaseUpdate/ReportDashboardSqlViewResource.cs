using System;
using System.IO;
using System.Reflection;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Loads Report Dashboard SQL view scripts embedded under Visa2026.Module.SqlViews.*.
/// </summary>
internal static class ReportDashboardSqlViewResource
{
    public static string Load(string resourceLeaf)
    {
        if (string.IsNullOrWhiteSpace(resourceLeaf))
            throw new ArgumentException("Resource leaf is required.", nameof(resourceLeaf));

        var assembly = typeof(ReportDashboardSqlViewResource).Assembly;
        var resourceName = "Visa2026.Module.SqlViews." + resourceLeaf;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                "Missing embedded Report Dashboard SQL resource: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
