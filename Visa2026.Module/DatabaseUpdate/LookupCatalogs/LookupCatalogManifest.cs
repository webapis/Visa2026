using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>Deserialized <c>manifest.json</c> driving <see cref="LookupCatalogSyncUpdater"/>.</summary>
internal sealed class LookupCatalogManifest
{
    public int Version { get; set; } = 1;

    public List<LookupCatalogDefinition> Catalogs { get; set; } = new();
}

internal sealed class LookupCatalogDefinition
{
    public required string Id { get; set; }

    /// <summary>OData / EF type name, e.g. <c>Country</c>, <c>CheckPoint</c>.</summary>
    public required string Entity { get; set; }

    /// <summary>JSON file name under <c>LookupCatalogs/</c> (embedded resource).</summary>
    public required string File { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LookupCatalogMatchKey MatchKey { get; set; } = LookupCatalogMatchKey.Name;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LookupCatalogSyncMode SyncMode { get; set; } = LookupCatalogSyncMode.OverwriteScalars;

    /// <summary>When true, missing file is ignored (tenant overlay).</summary>
    public bool Optional { get; set; }
}

internal enum LookupCatalogMatchKey
{
    Name,
    NameTm,
    FullName,
    FullAddress,
    CodeOrName,
    NameAndRegion,
    NameAndCompany,
    BusinessObjectKey,
}

internal enum LookupCatalogSyncMode
{
  OverwriteScalars,
  InsertOnly,
}

internal sealed class LookupCatalogFile
{
    public List<Dictionary<string, JsonElement>> Rows { get; set; } = new();
}
