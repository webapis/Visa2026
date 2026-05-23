using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

internal static class LookupCatalogResourceLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static LookupCatalogManifest LoadManifest()
    {
        var main = LoadManifestFromEmbedded("manifest.json")
            ?? throw new InvalidOperationException("Embedded LookupCatalogs/manifest.json not found.");

        var tenant = LoadManifestFromEmbedded("tenant/manifest.json")
            ?? TryLoadManifestFromDisk(
                Path.Combine(AppContext.BaseDirectory, "LookupCatalogs", "tenant", "manifest.json"));
        if (tenant?.Catalogs is { Count: > 0 })
            MergeManifests(main, tenant);

        return main;
    }

    public static LookupCatalogFile? LoadCatalogFile(string fileName)
    {
        var embedded = LoadCatalogFromEmbedded(fileName)
            ?? LoadCatalogFromEmbedded("tenant/" + fileName);
        if (embedded != null)
            return embedded;

        var diskPath = Path.Combine(AppContext.BaseDirectory, "LookupCatalogs", "tenant", fileName);
        if (!File.Exists(diskPath))
            return null;

        var json = File.ReadAllText(diskPath);
        return JsonSerializer.Deserialize<LookupCatalogFile>(json, JsonOptions);
    }

    private static void MergeManifests(LookupCatalogManifest main, LookupCatalogManifest tenant)
    {
        var byId = new Dictionary<string, LookupCatalogDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in main.Catalogs)
            byId[c.Id] = c;

        foreach (var c in tenant.Catalogs)
            byId[c.Id] = c;

        main.Catalogs = new List<LookupCatalogDefinition>(byId.Values);
    }

    private static LookupCatalogManifest? LoadManifestFromEmbedded(string fileName)
    {
        var json = ReadEmbeddedText(fileName);
        return json == null
            ? null
            : JsonSerializer.Deserialize<LookupCatalogManifest>(json, JsonOptions);
    }

    private static LookupCatalogManifest? TryLoadManifestFromDisk(string path) =>
        !File.Exists(path) ? null : JsonSerializer.Deserialize<LookupCatalogManifest>(File.ReadAllText(path), JsonOptions);

    private static LookupCatalogFile? LoadCatalogFromEmbedded(string fileName)
    {
        var json = ReadEmbeddedText(fileName);
        return json == null ? null : JsonSerializer.Deserialize<LookupCatalogFile>(json, JsonOptions);
    }

    private static string? ReadEmbeddedText(string fileName)
    {
        var assembly = typeof(LookupCatalogResourceLoader).Assembly;
        var suffix = fileName.Replace('\\', '/');
        string? resourceName = null;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith("LookupCatalogs." + fileName.Replace('/', '.'), StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }

        if (resourceName == null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
