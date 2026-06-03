using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
        return DeserializeCatalogFile(json);
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
        return json == null ? null : DeserializeCatalogFile(json);
    }

    private static LookupCatalogFile? DeserializeCatalogFile(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LookupCatalogFile>(json, JsonOptions);
        }
        catch (JsonException) when (TryCoerceSingleRowCatalog(json, out var coerced))
        {
            return JsonSerializer.Deserialize<LookupCatalogFile>(coerced, JsonOptions);
        }
    }

    /// <summary>PowerShell ConvertTo-Json can emit a single row as an object instead of a one-element array.</summary>
    private static bool TryCoerceSingleRowCatalog(string json, out string coerced)
    {
        coerced = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Object)
                return false;

            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("rows");
                writer.WriteStartArray();
                rows.WriteTo(writer);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            coerced = Encoding.UTF8.GetString(buffer.ToArray());
            return true;
        }
        catch
        {
            return false;
        }
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
