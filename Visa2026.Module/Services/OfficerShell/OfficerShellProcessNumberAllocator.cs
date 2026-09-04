using System;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Allocates migration-service case numbers in <c>YYYY-NNNN</c> form (prototype parity).
/// </summary>
public static class OfficerShellProcessNumberAllocator
{
    public static string Allocate(IObjectSpace objectSpace, DateTime? asOf = null)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        var year = (asOf ?? DateTime.Today).Year;
        var prefix = $"{year}-";

        var maxSequence = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ProcessNumber != null && a.ProcessNumber.StartsWith(prefix))
            .AsEnumerable()
            .Select(a => TryParseSequence(a.ProcessNumber, prefix))
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxSequence + 1).ToString("D4", CultureInfo.InvariantCulture)}";
    }

    private static int TryParseSequence(string? processNumber, string prefix)
    {
        if (string.IsNullOrWhiteSpace(processNumber) || !processNumber.StartsWith(prefix, StringComparison.Ordinal))
            return 0;

        var suffix = processNumber[prefix.Length..].Trim();
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 0;
    }
}
