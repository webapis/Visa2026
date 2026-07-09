using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

internal static class LookupCatalogEntitySync
{
    private static readonly HashSet<string> SkipPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ID", "Oid", "ObjectSpace", "GCRecord", "OptimisticLockField",
    };

    /// <summary>
    /// Top-level BO types in <see cref="Country"/>'s namespace (manifest <c>entity</c> names).
    /// Excludes nested compiler-generated types (<c>&lt;&gt;c</c>, display classes) that share
    /// <see cref="Type.Name"/> and would make <c>ToDictionary</c> throw.
    /// </summary>
    private static readonly Dictionary<string, Type> EntityTypes =
        typeof(Country).Assembly
            .GetTypes()
            .Where(t =>
                t.Namespace == typeof(Country).Namespace
                && t.IsClass
                && !t.IsAbstract
                && !t.IsNested)
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

    public static (int created, int updated, int skipped) Sync(
        IObjectSpace objectSpace,
        LookupCatalogDefinition definition,
        LookupCatalogFile file)
    {
        if (!EntityTypes.TryGetValue(definition.Entity, out var entityType))
            throw new InvalidOperationException($"Unknown lookup entity '{definition.Entity}'.");

        int created = 0, updated = 0, skipped = 0;
        foreach (var row in file.Rows)
        {
            if (row.Count == 0 || !RowHasKey(row, definition))
            {
                skipped++;
                continue;
            }

            var existing = FindExisting(objectSpace, entityType, definition, row);
            var isNew = existing == null;
            var target = isNew
                ? objectSpace.CreateObject(entityType)
                : existing!;

            if (isNew)
                created++;
            else
                updated++;

            if (definition.SyncMode == LookupCatalogSyncMode.InsertOnly && !isNew)
                continue;

            ApplyRow(objectSpace, target, row, definition);
        }

        return (created, updated, skipped);
    }

    private static bool RowHasKey(Dictionary<string, JsonElement> row, LookupCatalogDefinition definition) =>
        definition.MatchKey switch
        {
            LookupCatalogMatchKey.CodeOrName =>
                HasNonEmpty(row, "Code") || HasNonEmpty(row, "NameTm") || HasNonEmpty(row, "Name"),
            LookupCatalogMatchKey.FullName => HasNonEmpty(row, "FullName"),
            LookupCatalogMatchKey.FullAddress => HasNonEmpty(row, "FullAddress"),
            LookupCatalogMatchKey.BusinessObjectKey => HasNonEmpty(row, "BusinessObjectKey"),
            LookupCatalogMatchKey.NameAndRegion =>
                (HasNonEmpty(row, "NameTm") || HasNonEmpty(row, "Name"))
                && (HasNonEmpty(row, "Region") || HasNonEmpty(row, "RegionName")),
            LookupCatalogMatchKey.NameTm => HasNonEmpty(row, "NameTm"),
            LookupCatalogMatchKey.NameTmTitle => HasNonEmpty(row, "NameTm"),
            LookupCatalogMatchKey.ShortNameTm => HasNonEmpty(row, "ShortNameTm"),
            LookupCatalogMatchKey.CityAndFullAddress =>
                HasNonEmpty(row, "FullAddress")
                && (HasNonEmpty(row, "City") || HasNonEmpty(row, "NameTm"))
                && (HasNonEmpty(row, "Region") || HasNonEmpty(row, "RegionName")),
            LookupCatalogMatchKey.CityAndName =>
                HasNonEmpty(row, "Name")
                && (HasNonEmpty(row, "City") || HasNonEmpty(row, "NameTm"))
                && (HasNonEmpty(row, "Region") || HasNonEmpty(row, "RegionName")),
            _ => HasNonEmpty(row, "Name"),
        };

    private static object? FindExisting(
        IObjectSpace objectSpace,
        Type entityType,
        LookupCatalogDefinition definition,
        Dictionary<string, JsonElement> row)
    {
        if (entityType == typeof(CompanyProfile))
            return FindOrganizationSingleton(objectSpace, typeof(CompanyProfile), definition, row, "Name");
        if (entityType == typeof(ApplicationNumberingProfile))
            return FindOrganizationSingleton(objectSpace, typeof(ApplicationNumberingProfile), definition, row, "Name");
        if (entityType == typeof(AuthorizedSignatory))
            return FindOrganizationSingleton(objectSpace, typeof(AuthorizedSignatory), definition, row, "FullName");
        if (entityType == typeof(AuthorizedRepresentative))
            return FindOrganizationSingleton(objectSpace, typeof(AuthorizedRepresentative), definition, row, "FullName");

        return definition.MatchKey switch
        {
            LookupCatalogMatchKey.CodeOrName => FindByCodeOrName(objectSpace, entityType, row),
            LookupCatalogMatchKey.FullName => FindByProperty(objectSpace, entityType, "FullName", GetString(row, "FullName")),
            LookupCatalogMatchKey.FullAddress => FindByProperty(objectSpace, entityType, "FullAddress", GetString(row, "FullAddress")),
            LookupCatalogMatchKey.BusinessObjectKey =>
                FindByProperty(objectSpace, entityType, "BusinessObjectKey", GetString(row, "BusinessObjectKey")),
            LookupCatalogMatchKey.NameAndRegion => FindCity(objectSpace, row),
            LookupCatalogMatchKey.NameTm => FindByNameTm(objectSpace, entityType, row),
            LookupCatalogMatchKey.NameTmTitle => FindByNameTmTitle(objectSpace, entityType, row),
            LookupCatalogMatchKey.ShortNameTm =>
                FindByProperty(objectSpace, entityType, "ShortNameTm", GetString(row, "ShortNameTm")),
            LookupCatalogMatchKey.CityAndFullAddress => FindByCityAndProperty(objectSpace, entityType, row, "FullAddress"),
            LookupCatalogMatchKey.CityAndName => FindByCityAndProperty(objectSpace, entityType, row, "Name"),
            _ => FindByName(objectSpace, entityType, GetString(row, "Name")),
        };
    }

    /// <summary>
    /// Organization singleton: match by key, else reuse an empty placeholder row from
    /// <see cref="OrganizationSingletonSeedUpdater"/> if present.
    /// </summary>
    private static object? FindOrganizationSingleton(
        IObjectSpace objectSpace,
        Type entityType,
        LookupCatalogDefinition definition,
        Dictionary<string, JsonElement> row,
        string keyProperty)
    {
        object? byKey = definition.MatchKey switch
        {
            LookupCatalogMatchKey.CodeOrName when keyProperty == "Name" =>
                FindByCodeOrName(objectSpace, entityType, row),
            LookupCatalogMatchKey.FullName =>
                FindByProperty(objectSpace, entityType, "FullName", GetString(row, "FullName")),
            _ => FindByProperty(objectSpace, entityType, keyProperty, GetString(row, keyProperty)),
        };
        if (byKey != null)
            return byKey;

        // Singleton BO: JSON may change FullName/Name (match key). Reuse an existing populated row.
        var populated = objectSpace.GetObjects(entityType).Cast<object>()
            .Where(o => !string.IsNullOrWhiteSpace(GetPropertyString(o, keyProperty)))
            .OrderBy(o => GetPropertyString(o, keyProperty), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (populated.Count >= 1)
            return populated[0];

        return LookupCatalogQueryHelper.FirstOrDefault(objectSpace, entityType,
            o => string.IsNullOrWhiteSpace(GetPropertyString(o, keyProperty)));
    }

    /// <summary>
    /// When tenant JSON defines exactly one organization singleton identity, keep one row (matching JSON key
    /// when possible) and delete all other rows for that entity.
    /// </summary>
    public static int RemoveStaleOrganizationSingletonDuplicates(
        IObjectSpace objectSpace,
        IEnumerable<LookupCatalogDefinition> catalogs)
    {
        int removed = 0;
        foreach (var definition in catalogs)
        {
            if (!IsOrganizationSingletonEntity(definition.Entity))
                continue;
            if (!EntityTypes.TryGetValue(definition.Entity, out var entityType))
                continue;

            var file = LookupCatalogResourceLoader.LoadCatalogFile(definition.File);
            if (file?.Rows == null)
                continue;

            var keyProperty = OrganizationSingletonKeyProperty(definition.Entity);
            var allowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in file.Rows)
            {
                if (!RowHasKey(row, definition))
                    continue;
                var key = GetOrganizationSingletonKeyFromRow(row, definition, keyProperty);
                if (!string.IsNullOrWhiteSpace(key))
                    allowedKeys.Add(key);
            }

            if (allowedKeys.Count != 1)
                continue;

            var allowedKey = allowedKeys.First();
            var all = objectSpace.GetObjects(entityType).Cast<object>().ToList();
            if (all.Count <= 1)
                continue;

            object? keeper = all.FirstOrDefault(o =>
                    string.Equals(GetPropertyString(o, keyProperty), allowedKey, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(o => !string.IsNullOrWhiteSpace(GetPropertyString(o, keyProperty)))
                ?? all[0];

            foreach (var item in all)
            {
                if (ReferenceEquals(item, keeper))
                    continue;
                objectSpace.Delete(item);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// Removes duplicate lookup rows that share the same catalog identity (Code, LocalizationKey, or normalized NameTm).
    /// Repoints FK references before delete so re-running <c>--forceUpdate</c> cannot leave parallel copies.
    /// </summary>
    public static int RemoveDuplicateCatalogRows(
        IObjectSpace objectSpace,
        IEnumerable<LookupCatalogDefinition> catalogs)
    {
        int removed = 0;
        foreach (var definition in catalogs)
        {
            if (IsOrganizationSingletonEntity(definition.Entity))
                continue;

            if (!EntityTypes.TryGetValue(definition.Entity, out var entityType))
                continue;

            removed += RemoveDuplicateCatalogRows(objectSpace, entityType, definition);
        }

        return removed;
    }

    private static int RemoveDuplicateCatalogRows(
        IObjectSpace objectSpace,
        Type entityType,
        LookupCatalogDefinition definition)
    {
        var rows = objectSpace.GetObjects(entityType).Cast<object>().ToList();
        var duplicateGroups = rows
            .GroupBy(row => GetCatalogIdentityKey(row, definition), StringComparer.Ordinal)
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .ToList();

        if (duplicateGroups.Count == 0)
            return 0;

        int removed = 0;
        foreach (var group in duplicateGroups)
        {
            var keeper = SelectCatalogKeeper(group);
            foreach (var duplicate in group.Where(row => !ReferenceEquals(row, keeper)))
            {
                RepointLookupReferences(objectSpace, entityType, duplicate, keeper);
                objectSpace.Delete(duplicate);
                removed++;
            }
        }

        if (entityType == typeof(City) && definition.MatchKey == LookupCatalogMatchKey.NameAndRegion)
            removed += RemoveDuplicateCityNullRegionOrphans(objectSpace);

        return removed;
    }

    /// <summary>
    /// Merges orphan Cities (null <see cref="City.Region"/>) into the Region-linked row with the same <see cref="LookupBase.NameTm"/>.
    /// Identity keys differ (<c>T:name</c> vs <c>R:region|T:name</c>) so the standard dedupe pass does not group them.
    /// </summary>
    private static int RemoveDuplicateCityNullRegionOrphans(IObjectSpace objectSpace)
    {
        var cities = objectSpace.GetObjects(typeof(City)).Cast<City>().ToList();
        var duplicateNameGroups = cities
            .Where(c => !string.IsNullOrWhiteSpace(c.NameTm) || !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => LookupCatalogMatchHelper.NormalizeKey(c.NameTm ?? c.Name ?? string.Empty), StringComparer.Ordinal)
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .Where(g => g.Any(c => c.Region != null) && g.Any(c => c.Region == null))
            .ToList();

        int removed = 0;
        foreach (var group in duplicateNameGroups)
        {
            var keeper = group.Where(c => c.Region != null).OrderBy(c => c.ID).First();
            foreach (var duplicate in group.Where(c => c.ID != keeper.ID))
            {
                RepointLookupReferences(objectSpace, typeof(City), duplicate, keeper);
                objectSpace.Delete(duplicate);
                removed++;
            }
        }

        return removed;
    }

    private static object SelectCatalogKeeper(IEnumerable<object> group) =>
        group
            .OrderByDescending(HasPopulatedCatalogTitle)
            .ThenByDescending(row => row is City city && city.Region != null)
            .ThenBy(GetRowId)
            .First();

    private static bool HasPopulatedCatalogTitle(object row)
    {
        if (row is LookupBase lookup)
            return !string.IsNullOrWhiteSpace(lookup.NameTm)
                || !string.IsNullOrWhiteSpace(lookup.Name)
                || !string.IsNullOrWhiteSpace(lookup.Code);

        return !string.IsNullOrWhiteSpace(GetPropertyString(row, "NameTm"))
            || !string.IsNullOrWhiteSpace(GetPropertyString(row, "Name"))
            || !string.IsNullOrWhiteSpace(GetPropertyString(row, "Code"));
    }

    private static Guid GetRowId(object row)
    {
        var idProperty = row.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
        if (idProperty?.PropertyType == typeof(Guid) && idProperty.GetValue(row) is Guid id)
            return id;

        return Guid.Empty;
    }

    private static string GetCatalogIdentityKey(object row, LookupCatalogDefinition definition)
    {
        if (definition.MatchKey == LookupCatalogMatchKey.NameTmTitle)
        {
            if (row is ProjectContract contract && !string.IsNullOrWhiteSpace(contract.LocalizationKey))
                return "L:" + LookupCatalogMatchHelper.NormalizeKey(contract.LocalizationKey);

            var titleLocKey = GetPropertyString(row, "LocalizationKey");
            if (!string.IsNullOrWhiteSpace(titleLocKey))
                return "L:" + LookupCatalogMatchHelper.NormalizeKey(titleLocKey);

            var titleNameTm = row is LookupBase lookupTitle
                ? lookupTitle.NameTm
                : GetPropertyString(row, "NameTm");
            return string.IsNullOrWhiteSpace(titleNameTm)
                ? string.Empty
                : "T:" + LookupCatalogMatchHelper.NormalizeKey(titleNameTm);
        }

        if (definition.MatchKey == LookupCatalogMatchKey.ShortNameTm)
        {
            var shortNameTm = GetPropertyString(row, "ShortNameTm");
            return string.IsNullOrWhiteSpace(shortNameTm)
                ? string.Empty
                : "S:" + LookupCatalogMatchHelper.NormalizeKey(shortNameTm);
        }

        if (row is LookupBase lookup)
        {
            if (!string.IsNullOrWhiteSpace(lookup.LocalizationKey))
                return "L:" + LookupCatalogMatchHelper.NormalizeKey(lookup.LocalizationKey);

            if (!string.IsNullOrWhiteSpace(lookup.Code))
                return "C:" + LookupCatalogMatchHelper.NormalizeKey(lookup.Code);
        }

        var localizationKey = GetPropertyString(row, "LocalizationKey");
        if (!string.IsNullOrWhiteSpace(localizationKey))
            return "L:" + LookupCatalogMatchHelper.NormalizeKey(localizationKey);

        var code = GetPropertyString(row, "Code");
        if (!string.IsNullOrWhiteSpace(code))
            return "C:" + LookupCatalogMatchHelper.NormalizeKey(code);

        if (definition.MatchKey == LookupCatalogMatchKey.FullName)
        {
            var fullName = GetPropertyString(row, "FullName");
            return fullName == null ? string.Empty : "F:" + LookupCatalogMatchHelper.NormalizeKey(fullName);
        }

        if (definition.MatchKey == LookupCatalogMatchKey.FullAddress)
        {
            var fullAddress = GetPropertyString(row, "FullAddress");
            return fullAddress == null ? string.Empty : "A:" + LookupCatalogMatchHelper.NormalizeKey(fullAddress);
        }

        if (definition.MatchKey == LookupCatalogMatchKey.CityAndFullAddress)
            return BuildCityAndScalarSyncKey(row, "FullAddress");

        if (definition.MatchKey == LookupCatalogMatchKey.CityAndName)
            return BuildCityAndScalarSyncKey(row, "Name");

        if (definition.MatchKey == LookupCatalogMatchKey.NameAndRegion && row is City city)
        {
            var regionTitle = city.Region == null
                ? string.Empty
                : LookupCatalogMatchHelper.NormalizeKey(city.Region.NameTm ?? city.Region.Name);
            var cityTitle = LookupCatalogMatchHelper.NormalizeKey(city.NameTm ?? city.Name);
            if (cityTitle.Length == 0 || regionTitle.Length == 0)
                return string.Empty;

            return "R:" + regionTitle + "|T:" + cityTitle;
        }

        var nameTm = GetPropertyString(row, "NameTm");
        if (!string.IsNullOrWhiteSpace(nameTm))
            return "T:" + LookupCatalogMatchHelper.NormalizeKey(nameTm);

        var name = GetPropertyString(row, "Name");
        return name == null ? string.Empty : "N:" + LookupCatalogMatchHelper.NormalizeKey(name);
    }

    private static void RepointLookupReferences(
        IObjectSpace objectSpace,
        Type lookupType,
        object fromRow,
        object toRow)
    {
        var fromId = GetRowId(fromRow);
        if (fromId == Guid.Empty)
            return;

        foreach (var referrerType in GetReferrerTypes(lookupType))
        {
            foreach (var referrer in objectSpace.GetObjects(referrerType))
            {
                foreach (var property in GetLookupReferenceProperties(referrerType, lookupType))
                {
                    if (property.GetValue(referrer) is not { } current)
                        continue;

                    if (!ReferenceEquals(current, fromRow) && GetRowId(current) != fromId)
                        continue;

                    property.SetValue(referrer, toRow);
                }
            }
        }
    }

    private static IEnumerable<Type> GetReferrerTypes(Type lookupType)
    {
        foreach (var type in EntityTypes.Values)
        {
            if (type == lookupType)
                continue;

            if (GetLookupReferenceProperties(type, lookupType).Any())
                yield return type;
        }
    }

    private static IEnumerable<PropertyInfo> GetLookupReferenceProperties(Type referrerType, Type lookupType) =>
        referrerType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType == lookupType && p.GetIndexParameters().Length == 0);

    private static bool IsOrganizationSingletonEntity(string entity) =>
        entity.Equals(nameof(CompanyProfile), StringComparison.OrdinalIgnoreCase)
        || entity.Equals(nameof(ApplicationNumberingProfile), StringComparison.OrdinalIgnoreCase)
        || entity.Equals(nameof(AuthorizedSignatory), StringComparison.OrdinalIgnoreCase)
        || entity.Equals(nameof(AuthorizedRepresentative), StringComparison.OrdinalIgnoreCase);

    private static string OrganizationSingletonKeyProperty(string entity) =>
        entity.Equals(nameof(CompanyProfile), StringComparison.OrdinalIgnoreCase)
        || entity.Equals(nameof(ApplicationNumberingProfile), StringComparison.OrdinalIgnoreCase)
            ? "Name"
            : "FullName";

    private static string? GetOrganizationSingletonKeyFromRow(
        Dictionary<string, JsonElement> row,
        LookupCatalogDefinition definition,
        string keyProperty) =>
        definition.MatchKey switch
        {
            LookupCatalogMatchKey.FullName => GetString(row, "FullName"),
            LookupCatalogMatchKey.CodeOrName when keyProperty == "Name" =>
                GetString(row, "Code") ?? GetString(row, "NameTm") ?? GetString(row, "Name"),
            _ => GetString(row, keyProperty),
        };

    private static object? FindByName(IObjectSpace objectSpace, Type entityType, string? name) =>
        FindByProperty(objectSpace, entityType, "Name", name);

    /// <summary>
    /// Match catalog rows by <see cref="LookupBase.Code"/>, <see cref="LookupBase.LocalizationKey"/>, then
    /// normalized <see cref="LookupBase.NameTm"/> / <see cref="LookupBase.Name"/>.
    /// </summary>
    private static object? FindByNameTm(IObjectSpace objectSpace, Type entityType, Dictionary<string, JsonElement> row)
    {
        var code = GetString(row, "Code");
        if (!string.IsNullOrWhiteSpace(code))
        {
            var byCode = FindByProperty(objectSpace, entityType, "Code", code);
            if (byCode != null)
                return byCode;
        }

        var localizationKey = GetString(row, "LocalizationKey");
        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            var byLocalizationKey = FindByProperty(objectSpace, entityType, "LocalizationKey", localizationKey);
            if (byLocalizationKey != null)
                return byLocalizationKey;
        }

        var nameTm = GetString(row, "NameTm");
        if (string.IsNullOrWhiteSpace(nameTm))
            return null;

        return FindByTitle(objectSpace, entityType, nameTm);
    }

    /// <summary>
    /// Upsert identity for rows that share <see cref="LookupBase.Code"/> but differ by title (e.g. project contract leg variants).
    /// </summary>
    private static object? FindByNameTmTitle(IObjectSpace objectSpace, Type entityType, Dictionary<string, JsonElement> row)
    {
        if (entityType == typeof(ProjectContract))
        {
            var byProjectContract = FindProjectContractForCatalogRow(objectSpace, row);
            if (byProjectContract != null)
                return byProjectContract;
        }

        var nameTm = GetString(row, "NameTm");
        if (string.IsNullOrWhiteSpace(nameTm))
            return null;

        return FindByProperty(objectSpace, entityType, "NameTm", nameTm);
    }

    private static object? FindProjectContractForCatalogRow(
        IObjectSpace objectSpace,
        Dictionary<string, JsonElement> row)
    {
        var localizationKey = GetString(row, "LocalizationKey");
        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            var byKey = FindProjectContractsByProperty(objectSpace, nameof(LookupBase.LocalizationKey), localizationKey);
            if (byKey.Count > 0)
                return SelectProjectContractKeeper(objectSpace, byKey);
        }

        var nameTm = GetString(row, "NameTm");
        var code = GetString(row, "Code") ?? nameTm;
        if (!string.IsNullOrWhiteSpace(nameTm))
        {
            var byNameTm = FindByProperty(objectSpace, typeof(ProjectContract), "NameTm", nameTm);
            if (byNameTm != null)
                return byNameTm;
        }

        if (string.IsNullOrWhiteSpace(code))
            return null;

        var legacyMatches = objectSpace.GetObjects(typeof(ProjectContract))
            .Cast<ProjectContract>()
            .Where(c =>
                LegacyProjectContractTitleStartsWithCode(c.NameTm, code)
                || (!string.IsNullOrWhiteSpace(nameTm) && TitleMatches(c, nameTm)))
            .ToList();

        if (legacyMatches.Count == 0)
            return null;

        return SelectProjectContractKeeper(objectSpace, legacyMatches);
    }

    private static List<ProjectContract> FindProjectContractsByProperty(
        IObjectSpace objectSpace,
        string propertyName,
        string value) =>
        objectSpace.GetObjects(typeof(ProjectContract))
            .Cast<ProjectContract>()
            .Where(c => CatalogFieldEquals(c, propertyName, value))
            .ToList();

    private static ProjectContract SelectProjectContractKeeper(
        IObjectSpace objectSpace,
        IReadOnlyList<ProjectContract> matches) =>
        matches
            .OrderByDescending(c => CountProjectContractReferences(objectSpace, c))
            .ThenByDescending(c => c.Description?.Length ?? 0)
            .ThenBy(c => c.NameTm?.Length ?? int.MaxValue)
            .ThenBy(c => c.ID)
            .First();

    private static int CountProjectContractReferences(IObjectSpace objectSpace, ProjectContract contract)
    {
        var id = contract.ID;
        int count = objectSpace.GetObjects(typeof(Person)).Cast<Person>()
            .Count(p => p.ProjectContract?.ID == id);
        count += objectSpace.GetObjects(typeof(Application)).Cast<Application>()
            .Count(a => a.ProjectContract?.ID == id);
        count += objectSpace.GetObjects(typeof(UserReportTemplateProjectContract))
            .Cast<UserReportTemplateProjectContract>()
            .Count(l => l.ProjectContractId == id);
        return count;
    }

    private static bool LegacyProjectContractTitleStartsWithCode(string? nameTm, string? code)
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

    private static object? FindByProperty(IObjectSpace objectSpace, Type entityType, string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return LookupCatalogQueryHelper.FirstOrDefault(objectSpace, entityType,
            o => CatalogFieldEquals(o, propertyName, value));
    }

    private static bool CatalogFieldEquals(object instance, string propertyName, string? value)
    {
        var stored = GetPropertyString(instance, propertyName);
        if (string.IsNullOrWhiteSpace(stored))
            return false;

        return LookupCatalogMatchHelper.KeysEqual(stored, value)
            || string.Equals(stored, value, StringComparison.OrdinalIgnoreCase);
    }

    private static object? FindByCodeOrName(IObjectSpace objectSpace, Type entityType, Dictionary<string, JsonElement> row)
    {
        var code = GetString(row, "Code");
        if (!string.IsNullOrWhiteSpace(code))
        {
            var byCode = FindByProperty(objectSpace, entityType, "Code", code);
            if (byCode != null)
                return byCode;
        }

        var localizationKey = GetString(row, "LocalizationKey");
        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            var byLocalizationKey = FindByProperty(objectSpace, entityType, "LocalizationKey", localizationKey);
            if (byLocalizationKey != null)
                return byLocalizationKey;
        }

        return FindByTitle(objectSpace, entityType, GetRowTitle(row));
    }

    private static object? FindCity(IObjectSpace objectSpace, Dictionary<string, JsonElement> row)
    {
        var title = GetRowTitle(row);
        var regionName = GetString(row, "Region") ?? GetString(row, "RegionName");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(regionName))
            return null;

        return objectSpace.GetObjects(typeof(City))
            .Cast<City>()
            .FirstOrDefault(c =>
                TitleMatches(c, title)
                && c.Region != null
                && TitleMatches(c.Region, regionName));
    }

    private static string? GetRowTitle(Dictionary<string, JsonElement> row) =>
        GetString(row, "NameTm") ?? GetString(row, "Name");

    private static object? FindByTitle(IObjectSpace objectSpace, Type entityType, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return FindByProperty(objectSpace, entityType, "NameTm", title)
            ?? FindByProperty(objectSpace, entityType, "Name", title)
            ?? FindByProperty(objectSpace, entityType, "Code", title);
    }

    private static bool TitleMatches(LookupBase entity, string title) =>
        LookupCatalogMatchHelper.KeysEqual(entity.NameTm, title)
        || LookupCatalogMatchHelper.KeysEqual(entity.Name, title)
        || LookupCatalogMatchHelper.KeysEqual(entity.Code, title)
        || string.Equals(entity.NameTm, title, StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity.Name, title, StringComparison.OrdinalIgnoreCase)
        || string.Equals(entity.Code, title, StringComparison.OrdinalIgnoreCase);

    private static void ApplyRow(
        IObjectSpace objectSpace,
        object target,
        Dictionary<string, JsonElement> row,
        LookupCatalogDefinition definition)
    {
        foreach (var (key, value) in row)
        {
            if (key is "Region" or "RegionName" or "ApplicationTypeFilter")
            {
                ApplyNavigation(objectSpace, target, key, value);
                continue;
            }

            if (key == "City")
            {
                ApplyCityNavigation(objectSpace, target, row);
                continue;
            }

            if (SkipPropertyNames.Contains(key))
                continue;

            var prop = target.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || !prop.CanWrite || prop.GetIndexParameters().Length > 0)
                continue;

            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsEnum
                && prop.PropertyType != typeof(Guid) && !IsNullableValueType(prop.PropertyType))
                continue;

            try
            {
                var converted = ConvertJsonValue(value, prop.PropertyType);
                if (converted != null || IsNullable(prop.PropertyType))
                    prop.SetValue(target, converted);
            }
            catch
            {
                // Skip properties that do not convert cleanly.
            }
        }

        ApplyLocalizationKey(target, row);

        if (target is ProjectContract contract)
            ProjectContractApprovalLegProfileLinker.TryLinkContractFromCatalogRow(objectSpace, contract, row);
    }

    private static void ApplyLocalizationKey(object target, Dictionary<string, JsonElement> row)
    {
        if (target is not LookupBase lookup)
            return;

        var key = GetString(row, "LocalizationKey");
        if (string.IsNullOrWhiteSpace(key))
            key = GetString(row, "Code");

        // ProjectContract / MigrationService variants share Code but differ by NameTm — unique LocalizationKey per row.
        if ((target is ProjectContract or MigrationService)
            && string.IsNullOrWhiteSpace(GetString(row, "LocalizationKey")))
        {
            var title = GetString(row, "NameTm") ?? lookup.NameTm;
            if (!string.IsNullOrWhiteSpace(title))
            {
                lookup.LocalizationKey = LookupCatalogMatchHelper.ToLocalizationKey(title);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(key))
            return;

        lookup.LocalizationKey = LookupCatalogMatchHelper.ToLocalizationKey(key.Trim());
    }

    private static City? FindCityReference(IObjectSpace objectSpace, Dictionary<string, JsonElement> row)
    {
        var cityTitle = GetString(row, "City") ?? GetString(row, "NameTm");
        var regionName = GetString(row, "Region") ?? GetString(row, "RegionName");
        if (string.IsNullOrWhiteSpace(cityTitle) || string.IsNullOrWhiteSpace(regionName))
            return null;

        return objectSpace.GetObjects(typeof(City))
            .Cast<City>()
            .FirstOrDefault(c =>
                TitleMatches(c, cityTitle)
                && c.Region != null
                && TitleMatches(c.Region, regionName));
    }

    private static object? FindByCityAndProperty(
        IObjectSpace objectSpace,
        Type entityType,
        Dictionary<string, JsonElement> row,
        string scalarProperty)
    {
        var city = FindCityReference(objectSpace, row);
        var scalar = GetString(row, scalarProperty);
        if (city == null || string.IsNullOrWhiteSpace(scalar))
            return null;

        return objectSpace.GetObjects(entityType)
            .Cast<object>()
            .FirstOrDefault(item =>
            {
                var itemCity = item.GetType().GetProperty("City", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item) as City;
                if (itemCity == null || itemCity.ID != city.ID)
                    return false;

                var itemScalar = GetPropertyString(item, scalarProperty);
                return LookupCatalogMatchHelper.KeysEqual(itemScalar, scalar)
                    || string.Equals(itemScalar?.Trim(), scalar.Trim(), StringComparison.Ordinal);
            });
    }

    private static string BuildCityAndScalarSyncKey(Dictionary<string, JsonElement> row, string scalarProperty)
    {
        var regionName = GetString(row, "Region") ?? GetString(row, "RegionName");
        var cityName = GetString(row, "City") ?? GetString(row, "NameTm");
        var scalar = GetString(row, scalarProperty);
        return BuildCityAndScalarSyncKeyFromParts(regionName, cityName, scalar);
    }

    private static string BuildCityAndScalarSyncKey(object row, string scalarProperty) =>
        BuildCityAndScalarSyncKeyFromParts(
            GetPropertyString(row, "Region") ?? GetPropertyString(row, "RegionName"),
            GetPropertyString(row, "City") ?? GetPropertyString(row, "NameTm"),
            GetPropertyString(row, scalarProperty));

    private static string BuildCityAndScalarSyncKeyFromParts(string? regionName, string? cityName, string? scalar)
    {
        if (string.IsNullOrWhiteSpace(regionName) || string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(scalar))
            return string.Empty;

        return "R:" + LookupCatalogMatchHelper.NormalizeKey(regionName)
            + "|C:" + LookupCatalogMatchHelper.NormalizeKey(cityName)
            + "|S:" + LookupCatalogMatchHelper.NormalizeKey(scalar);
    }

    private static void ApplyCityNavigation(IObjectSpace objectSpace, object target, Dictionary<string, JsonElement> row)
    {
        var city = FindCityReference(objectSpace, row);
        if (city == null)
            return;

        var prop = target.GetType().GetProperty("City", BindingFlags.Instance | BindingFlags.Public);
        if (prop == null || !prop.CanWrite)
            return;

        prop.SetValue(target, city);
    }

    private static void ApplyNavigation(IObjectSpace objectSpace, object target, string key, JsonElement value)
    {
        var refName = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(refName))
            return;

        var propName = key switch
        {
            "RegionName" => "Region",
            "ApplicationTypeFilterNames" => "ApplicationTypeFilter",
            _ => key,
        };

        var prop = target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (prop == null || !prop.CanWrite)
            return;

        var refType = prop.PropertyType;
        var refEntity = typeof(LookupBase).IsAssignableFrom(refType)
            ? LookupCatalogQueryHelper.FirstOrDefault(objectSpace, refType,
                o => TitleMatches((LookupBase)o, refName))
            : LookupCatalogQueryHelper.FirstOrDefault(objectSpace, refType,
                o => string.Equals(GetPropertyString(o, "Name"), refName, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(GetPropertyString(o, "NameTm"), refName, StringComparison.OrdinalIgnoreCase));

        if (refEntity != null)
            prop.SetValue(target, refEntity);
    }

    private static object? ConvertJsonValue(JsonElement element, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (element.ValueKind == JsonValueKind.Null)
            return null;

        if (underlying == typeof(string))
            return element.GetString();

        if (underlying == typeof(bool))
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => DataImporterBoolParser.IsTrue(element.GetString()),
                JsonValueKind.Number => element.GetInt32() != 0,
                _ => false,
            };
        }

        if (underlying.IsEnum)
        {
            var s = element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.GetRawText();
            return Enum.Parse(underlying, s!, ignoreCase: true);
        }

        if (underlying == typeof(int))
            return element.ValueKind == JsonValueKind.String
                ? int.Parse(element.GetString()!, CultureInfo.InvariantCulture)
                : element.GetInt32();

        if (underlying == typeof(long))
            return element.GetInt64();

        if (underlying == typeof(decimal))
            return element.GetDecimal();

        if (underlying == typeof(double))
            return element.GetDouble();

        if (underlying == typeof(Guid))
            return Guid.Parse(element.GetString()!);

        if (underlying == typeof(DateTime))
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                return string.IsNullOrWhiteSpace(s)
                    ? null
                    : DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            return element.GetDateTime();
        }

        return element.GetString();
    }

    private static string? GetPropertyString(object o, string propertyName)
    {
        var prop = o.GetType().GetProperty(propertyName);
        return prop?.GetValue(o)?.ToString();
    }

    private static string? GetString(Dictionary<string, JsonElement> row, string key) =>
        row.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static bool HasNonEmpty(Dictionary<string, JsonElement> row, string key) =>
        row.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(el.GetString());

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static bool IsNullableValueType(Type type) =>
        Nullable.GetUnderlyingType(type) != null;
}

/// <summary>Shared bool parsing for JSON catalog rows (mirrors DataImporter).</summary>
internal static class DataImporterBoolParser
{
    public static bool IsTrue(string? raw) =>
        raw is "1" or "true" or "True" or "yes" or "Yes";
}
