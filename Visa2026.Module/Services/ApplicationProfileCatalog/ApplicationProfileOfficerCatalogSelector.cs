using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

/// <summary>
/// Officer Templates catalog / profile picker: one row per template family for
/// Wave 0b import clones (same Code + SelectionCode, differ by DefaultProjectContract).
/// Type-only rows are all kept so an officer-created profile is not hidden behind a seed.
/// </summary>
public static class ApplicationProfileOfficerCatalogSelector
{
    public static IEnumerable<ApplicationProfile> SelectDistinctTemplates(IEnumerable<ApplicationProfile>? profiles)
    {
        if (profiles == null)
            return Array.Empty<ApplicationProfile>();

        return profiles
            .Where(p => p != null)
            .GroupBy(DedupeKey, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g =>
            {
                var items = g.ToList();
                var typeOnly = items.Where(p => !IsContractBound(p)).ToList();
                if (typeOnly.Count > 0)
                {
                    return typeOnly
                        .OrderByDescending(p => p.IsActive)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }

                return items
                    .OrderByDescending(p => p.IsActive)
                    .ThenBy(p => p.Name?.Length ?? int.MaxValue)
                    .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(1);
            });
    }

    public static bool IsContractBound(ApplicationProfile profile) =>
        profile.DefaultProjectContractId != null || profile.DefaultProjectContract != null;

    internal static string DedupeKey(ApplicationProfile profile)
    {
        var code = (profile.Code ?? string.Empty).Trim();
        var selection = (profile.SelectionCode ?? string.Empty).Trim();
        if (code.Length == 0 && selection.Length == 0)
            return "id:" + profile.ID.ToString("N");

        return code + "\u001f" + selection;
    }
}
