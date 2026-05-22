using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>Loads and extends lookup catalogs used by comma-separated multi-select editors.</summary>
public static class CommaSeparatedCatalogHelper
{
    public static IReadOnlyList<string> LoadCatalogNames(IObjectSpace objectSpace, Type catalogEntityType, string? noneValue)
    {
        if (objectSpace == null || catalogEntityType == null)
        {
            return Array.Empty<string>();
        }

        if (catalogEntityType == typeof(BorderZoneName))
        {
            return objectSpace.GetObjectsQuery<BorderZoneName>()
                .Select(z => z.NameTm)
                .ToList()
                .Where(n => IsValidCatalogName(n, noneValue))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            return objectSpace.GetObjectsQuery<WorkPermittedLocationName>()
                .Select(z => z.NameTm)
                .ToList()
                .Where(n => IsValidCatalogName(n, noneValue))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (!typeof(LookupBase).IsAssignableFrom(catalogEntityType))
        {
            return Array.Empty<string>();
        }

        var objects = objectSpace.GetObjects(catalogEntityType);
        return objects.Cast<LookupBase>()
            .Select(x => x.NameTm)
            .Where(n => IsValidCatalogName(n, noneValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryAddCatalogEntry(
        IObjectSpace objectSpace,
        Type catalogEntityType,
        string nameTm,
        bool commitChanges = true)
    {
        if (objectSpace == null || catalogEntityType == null || string.IsNullOrWhiteSpace(nameTm))
        {
            return false;
        }

        var trimmed = nameTm.Trim();

        if (catalogEntityType == typeof(BorderZoneName))
        {
            if (objectSpace.FirstOrDefault<BorderZoneName>(z => z.NameTm == trimmed) != null)
            {
                return true;
            }

            var zone = objectSpace.CreateObject<BorderZoneName>();
            zone.NameTm = trimmed;
            zone.Name = trimmed;
            if (commitChanges)
            {
                objectSpace.CommitChanges();
            }

            return true;
        }

        if (catalogEntityType == typeof(WorkPermittedLocationName))
        {
            if (objectSpace.FirstOrDefault<WorkPermittedLocationName>(z => z.NameTm == trimmed) != null)
            {
                return true;
            }

            var location = objectSpace.CreateObject<WorkPermittedLocationName>();
            location.NameTm = trimmed;
            location.Name = trimmed;
            if (commitChanges)
            {
                objectSpace.CommitChanges();
            }

            return true;
        }

        if (!typeof(LookupBase).IsAssignableFrom(catalogEntityType))
        {
            return false;
        }

        var existing = objectSpace.GetObjects(catalogEntityType)
            .Cast<LookupBase>()
            .FirstOrDefault(x => string.Equals(x.NameTm, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return true;
        }

        var entry = (LookupBase)objectSpace.CreateObject(catalogEntityType);
        entry.NameTm = trimmed;
        entry.Name = trimmed;
        if (commitChanges)
        {
            objectSpace.CommitChanges();
        }

        return true;
    }

    public static IReadOnlyList<string> MergeCatalogWithSelected(
        IReadOnlyList<string> catalog,
        IEnumerable<string> selected)
    {
        var merged = new HashSet<string>(catalog, StringComparer.OrdinalIgnoreCase);
        foreach (var item in selected)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                merged.Add(item.Trim());
            }
        }

        return merged.OrderBy(z => z, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsValidCatalogName(string? name, string? noneValue) =>
        !string.IsNullOrWhiteSpace(name)
        && !CommaSeparatedSelectionHelper.IsNoneValue(name, noneValue);
}
