using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

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
                HasNonEmpty(row, "Code") || HasNonEmpty(row, "Name"),
            LookupCatalogMatchKey.FullName => HasNonEmpty(row, "FullName"),
            LookupCatalogMatchKey.NameAndRegion =>
                HasNonEmpty(row, "Name") && (HasNonEmpty(row, "Region") || HasNonEmpty(row, "RegionName")),
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
            LookupCatalogMatchKey.NameAndRegion => FindCity(objectSpace, row),
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

        return LookupCatalogQueryHelper.FirstOrDefault(objectSpace, entityType,
            o => string.IsNullOrWhiteSpace(GetPropertyString(o, keyProperty)));
    }

    private static object? FindByName(IObjectSpace objectSpace, Type entityType, string? name) =>
        FindByProperty(objectSpace, entityType, "Name", name);

    private static object? FindByProperty(IObjectSpace objectSpace, Type entityType, string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return LookupCatalogQueryHelper.FirstOrDefault(objectSpace, entityType,
            o => string.Equals(GetPropertyString(o, propertyName), value, StringComparison.OrdinalIgnoreCase));
    }

    private static object? FindByCodeOrName(IObjectSpace objectSpace, Type entityType, Dictionary<string, JsonElement> row)
    {
        var code = GetString(row, "Code");
        if (!string.IsNullOrWhiteSpace(code))
        {
            var byCode = LookupCatalogQueryHelper.FirstOrDefault(objectSpace, entityType,
                o => string.Equals(GetPropertyString(o, "Code"), code, StringComparison.OrdinalIgnoreCase));
            if (byCode != null)
                return byCode;
        }

        return FindByName(objectSpace, entityType, GetString(row, "Name"));
    }

    private static object? FindCity(IObjectSpace objectSpace, Dictionary<string, JsonElement> row)
    {
        var name = GetString(row, "Name");
        var regionName = GetString(row, "Region") ?? GetString(row, "RegionName");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(regionName))
            return null;

        return objectSpace.GetObjects(typeof(City))
            .Cast<City>()
            .FirstOrDefault(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                && c.Region != null
                && string.Equals(c.Region.Name, regionName, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyRow(
        IObjectSpace objectSpace,
        object target,
        Dictionary<string, JsonElement> row,
        LookupCatalogDefinition definition)
    {
        if (definition.SyncMode == LookupCatalogSyncMode.InsertOnly)
            return;

        foreach (var (key, value) in row)
        {
            if (key is "Region" or "RegionName" or "Ministry" or "ApplicationTypeFilter")
            {
                ApplyNavigation(objectSpace, target, key, value);
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
    }

    private static void ApplyLocalizationKey(object target, Dictionary<string, JsonElement> row)
    {
        if (target is not LookupBase lookup)
            return;

        var key = GetString(row, "LocalizationKey");
        if (string.IsNullOrWhiteSpace(key))
            key = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(key))
            return;

        lookup.LocalizationKey = key.Trim();
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
        var refEntity = LookupCatalogQueryHelper.FirstOrDefault(objectSpace, refType,
            o => string.Equals(GetPropertyString(o, "Name"), refName, StringComparison.OrdinalIgnoreCase));

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
