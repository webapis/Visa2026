using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Merges duplicate <see cref="ProjectContract"/> rows (legacy long <see cref="LookupBase.NameTm"/> vs new short title)
/// and applies <see cref="ProjectContract.Description"/> from the tenant catalog JSON.
/// </summary>
public sealed class ProjectContractTitleDescriptionMergeUpdater : ModuleUpdater
{
    public ProjectContractTitleDescriptionMergeUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var catalog = LookupCatalogResourceLoader.LoadCatalogFile("project-contract.json");
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            return;

        int merged = 0;
        int updated = 0;

        foreach (var row in catalog.Rows)
        {
            var nameTm = GetString(row, "NameTm");
            var description = GetString(row, "Description");
            var localizationKey = GetString(row, "LocalizationKey");
            var code = GetString(row, "Code") ?? nameTm;

            if (string.IsNullOrWhiteSpace(nameTm))
                continue;

            var matches = FindMatchingContracts(localizationKey, code, nameTm);
            if (matches.Count == 0)
                continue;

            var keeper = SelectKeeper(matches);
            if (!string.IsNullOrWhiteSpace(description))
                keeper.Description = description;
            if (!string.Equals(keeper.NameTm, nameTm, StringComparison.Ordinal))
            {
                keeper.NameTm = nameTm;
                updated++;
            }

            foreach (var duplicate in matches.Where(c => c.ID != keeper.ID))
            {
                RepointReferences(duplicate, keeper);
                ObjectSpace.Delete(duplicate);
                merged++;
            }
        }

        merged += CleanupLocalizationKeyDuplicates();

        if (merged > 0 || updated > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"ProjectContractTitleDescriptionMergeUpdater: merged {merged} duplicate row(s), refreshed {updated} contract title/description row(s).");
        }
    }

    private int CleanupLocalizationKeyDuplicates()
    {
        int removed = 0;
        var groups = ObjectSpace.GetObjectsQuery<ProjectContract>()
            .Where(c => c.LocalizationKey != null && c.LocalizationKey != "")
            .AsEnumerable()
            .GroupBy(c => c.LocalizationKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var matches = group.ToList();
            var keeper = SelectKeeper(matches);
            foreach (var duplicate in matches.Where(c => c.ID != keeper.ID))
            {
                RepointReferences(duplicate, keeper);
                ObjectSpace.Delete(duplicate);
                removed++;
            }
        }

        return removed;
    }

    private List<ProjectContract> FindMatchingContracts(string? localizationKey, string? code, string nameTm)
    {
        var all = ObjectSpace.GetObjectsQuery<ProjectContract>().ToList();
        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            var byKey = all
                .Where(c => LookupCatalogMatchHelper.KeysEqual(c.LocalizationKey, localizationKey))
                .ToList();
            if (byKey.Count > 0)
                return byKey;
        }

        return all
            .Where(c =>
                TitleMatches(c.NameTm, nameTm)
                || TitleMatches(c.NameTm, code)
                || LegacyTitleStartsWithCode(c.NameTm, code))
            .ToList();
    }

    private static bool LegacyTitleStartsWithCode(string? nameTm, string? code)
    {
        if (string.IsNullOrWhiteSpace(nameTm) || string.IsNullOrWhiteSpace(code))
            return false;

        var title = nameTm.Trim();
        var shortCode = code.Trim();
        if (!title.StartsWith(shortCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (title.Length == shortCode.Length)
            return true;

        var separator = title[shortCode.Length];
        return separator is ' ' or '-' or '—';
    }

    private ProjectContract SelectKeeper(IReadOnlyList<ProjectContract> matches)
    {
        return matches
            .OrderByDescending(CountReferences)
            .ThenByDescending(c => c.Description?.Length ?? 0)
            .ThenBy(c => c.NameTm?.Length ?? int.MaxValue)
            .ThenBy(c => c.ID)
            .First();
    }

    private int CountReferences(ProjectContract contract)
    {
        var id = contract.ID;
        int count = ObjectSpace.GetObjectsQuery<Person>()
            .Count(p => p.ProjectContract != null && p.ProjectContract.ID == id);
        count += ObjectSpace.GetObjectsQuery<Application>()
            .Count(a => a.ProjectContract != null && a.ProjectContract.ID == id);
        count += ObjectSpace.GetObjectsQuery<UserReportTemplateProjectContract>()
            .Count(l => l.ProjectContractId == id);
        return count;
    }

    private void RepointReferences(ProjectContract from, ProjectContract to)
    {
        foreach (var person in ObjectSpace.GetObjectsQuery<Person>()
                     .Where(p => p.ProjectContract != null && p.ProjectContract.ID == from.ID)
                     .ToList())
            person.ProjectContract = to;

        foreach (var application in ObjectSpace.GetObjectsQuery<Application>()
                     .Where(a => a.ProjectContract != null && a.ProjectContract.ID == from.ID)
                     .ToList())
            application.ProjectContract = to;

        foreach (var link in ObjectSpace.GetObjectsQuery<UserReportTemplateProjectContract>()
                     .Where(l => l.ProjectContractId == from.ID)
                     .ToList())
        {
            var duplicate = ObjectSpace.GetObjectsQuery<UserReportTemplateProjectContract>()
                .Any(l => l.UserReportTemplateId == link.UserReportTemplateId && l.ProjectContractId == to.ID);
            if (duplicate)
                ObjectSpace.Delete(link);
            else
                link.ProjectContract = to;
        }
    }

    private static bool TitleMatches(string? stored, string? expected) =>
        !string.IsNullOrWhiteSpace(stored)
        && !string.IsNullOrWhiteSpace(expected)
        && (LookupCatalogMatchHelper.KeysEqual(stored, expected)
            || string.Equals(stored.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? GetString(Dictionary<string, JsonElement> row, string key) =>
        row.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
