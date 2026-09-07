using System.Linq;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class OrganizationCatalogJsonSeedTests
{
    [Theory]
    [InlineData("company-profile.json", 1, "Çalyk Enerji", "Demo Hyzmatlar")]
    [InlineData("authorized-signatory.json", 1, "Mehmet Çırak", "Ali Demir")]
    [InlineData("authorized-representative.json", 1, "Nepesowa", "Orazowa")]
    public void Tenant_organization_json_has_production_row_only(
        string file,
        int expectedCount,
        string productionMarker,
        string demoMarker)
    {
        var catalog = LookupCatalogResourceLoader.LoadCatalogFile(file);
        Assert.NotNull(catalog);
        Assert.Equal(expectedCount, catalog!.Rows.Count);
        Assert.Contains(catalog.Rows, row =>
            row.Values.Any(v => v.ValueKind == System.Text.Json.JsonValueKind.String
                && v.GetString()?.Contains(productionMarker) == true));
        Assert.DoesNotContain(catalog.Rows, row =>
            row.Values.Any(v => v.ValueKind == System.Text.Json.JsonValueKind.String
                && v.GetString()?.Contains(demoMarker) == true));
    }
}
