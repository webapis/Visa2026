using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Persist / list case Address 1–2 free-text suggestions (tenant-wide per field).
/// </summary>
/// <remarks>
/// Suggestions are reusable across all applications — not scoped by Application Profile,
/// ProjectContract, ApplicationType, or other case parameters. <see cref="ApplicationProfileId"/>
/// is retained only as a required FK on insert (last writer); List ignores it.
/// </remarks>
public static class CaseSavedAddress
{
    public const int Cap = 50;

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedAddressField field,
        string? text)
    {
        if (objectSpace == null || application == null)
            return false;

        var display = CaseAddressAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display))
            return false;

        var key = CaseAddressAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        var existing = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field && row.TextKey == key)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();

        if (existing != null)
        {
            existing.Text = display;
            existing.UseCount = Math.Max(1, existing.UseCount) + 1;
            existing.UsedAtUtc = DateTime.UtcNow;
            Prune(objectSpace, field);
            return true;
        }

        var profileId = application.ApplicationProfile?.ID ?? Guid.Empty;
        if (profileId == Guid.Empty)
            return false;

        var created = objectSpace.CreateObject<ApplicationProfileSavedAddress>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId)
            ?? application.ApplicationProfile!;
        created.ApplicationProfileId = profileId;
        // Not used for listing — kept Empty so rows are not project-keyed.
        created.ProjectContractId = Guid.Empty;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 1;
        created.UsedAtUtc = DateTime.UtcNow;
        Prune(objectSpace, field);
        return true;
    }

    public static IReadOnlyList<CaseAddressAutocomplete.SavedEntry> List(
        IObjectSpace objectSpace,
        ApplicationProfileSavedAddressField field)
    {
        if (objectSpace == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .ToList();

        var deduped = new List<CaseAddressAutocomplete.SavedEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var text = CaseAddressAutocomplete.NormalizeDisplay(row.Text);
            if (string.IsNullOrEmpty(text))
                continue;
            var fold = CaseAddressAutocomplete.FoldKey(text);
            if (!seen.Add(fold))
                continue;
            deduped.Add(new CaseAddressAutocomplete.SavedEntry(text, row.UseCount, row.UsedAtUtc));
            if (deduped.Count >= Cap)
                break;
        }

        // Dual-read legacy Address2 recents until fully migrated (also unscoped).
        if (field == ApplicationProfileSavedAddressField.Address2 && deduped.Count == 0)
        {
            var legacy = objectSpace.GetObjectsQuery<ApplicationProfileAddress2Recent>()
                .OrderByDescending(row => row.UsedAtUtc)
                .Take(Cap * 4)
                .Select(row => new { row.Address2Text, row.UsedAtUtc })
                .ToList();
            var legacySeen = new HashSet<string>(StringComparer.Ordinal);
            var legacyOut = new List<CaseAddressAutocomplete.SavedEntry>();
            foreach (var row in legacy)
            {
                var text = CaseAddressAutocomplete.NormalizeDisplay(row.Address2Text);
                if (string.IsNullOrEmpty(text))
                    continue;
                if (!legacySeen.Add(CaseAddressAutocomplete.FoldKey(text)))
                    continue;
                legacyOut.Add(new CaseAddressAutocomplete.SavedEntry(text, 1, row.UsedAtUtc));
                if (legacyOut.Count >= Cap)
                    break;
            }
            return legacyOut;
        }

        return deduped;
    }

    private static void Prune(IObjectSpace objectSpace, ApplicationProfileSavedAddressField field)
    {
        var extras = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .Skip(Cap)
            .ToList();
        foreach (var row in extras)
            objectSpace.Delete(row);
    }
}