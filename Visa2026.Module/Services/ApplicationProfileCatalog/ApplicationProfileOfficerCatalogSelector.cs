using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

/// <summary>
/// Officer Templates catalog / profile picker: one row per template family.
/// Wave 0b import clones share <see cref="ApplicationProfile.Code"/> + SelectionCode and differ by
/// <see cref="ApplicationProfile.DefaultProjectContract"/>; the rail truncates that suffix so they look like duplicates.
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
            .Select(g => g
                .OrderByDescending(p => p.IsActive)
                .ThenBy(IsContractBound)
                .ThenBy(p => p.Name?.Length ?? int.MaxValue)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .First());
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
