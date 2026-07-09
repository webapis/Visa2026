using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Suggests unused ministry <see cref="ApplicationType.SelectionCode"/> values when cloning variants.
/// </summary>
public static class ApplicationTypeSelectionCodeHelper
{
    public static string? SuggestNextSelectionCode(IObjectSpace objectSpace, string? sourceSelectionCode)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        if (string.IsNullOrWhiteSpace(sourceSelectionCode)
            || sourceSelectionCode.Length != 3
            || !int.TryParse(sourceSelectionCode, out var sourceCode))
        {
            return null;
        }

        var group = sourceCode / 100;
        if (group is < 1 or > 8)
            return null;

        var usedCodes = objectSpace.GetObjectsQuery<ApplicationType>()
            .Where(t => t.SelectionCode != null && t.SelectionCode != "")
            .Select(t => t.SelectionCode!)
            .AsEnumerable()
            .Where(code => code.Length == 3
                           && int.TryParse(code, out var parsed)
                           && parsed / 100 == group)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groupStart = group * 100;
        var groupEnd = groupStart + 99;
        for (var candidate = groupEnd; candidate >= groupStart; candidate--)
        {
            var code = candidate.ToString("D3");
            if (!usedCodes.Contains(code))
                return code;
        }

        return null;
    }
}