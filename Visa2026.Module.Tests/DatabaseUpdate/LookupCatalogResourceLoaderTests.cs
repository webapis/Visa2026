using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class LookupCatalogResourceLoaderTests
{
    [Fact]
    public void LoadManifest_reads_embedded_catalogs()
    {
        var manifest = LookupCatalogResourceLoader.LoadManifest();

        Assert.True(manifest.Version >= 1);
        Assert.NotEmpty(manifest.Catalogs);
        Assert.Contains(manifest.Catalogs, c => c.Id == "country");
        Assert.Contains(manifest.Catalogs, c => c.Entity == "Gender");
    }

    [Fact]
    public void TryReadEmbeddedLookupCatalogText_returns_known_catalog_json()
    {
        var json = LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("country.json");

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("rows", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryReadEmbeddedLookupCatalogText_missing_file_returns_null()
    {
        Assert.Null(LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("definitely-missing-catalog.json"));
    }
}
