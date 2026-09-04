using System.Linq;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class OrganizationCatalogJsonSeedTests
{
    [Theory]
    [InlineData("company-profile.json", 2, "Demo Hyzmatlar")]
    [InlineData("authorized-signatory.json", 2, "Ali Demir")]
    [InlineData("authorized-representative.json", 2, "Orazowa")]
    public void Tenant_organization_json_includes_demo_row(string file, int expectedCount, string demoMarker)
    {
        var catalog = LookupCatalogResourceLoader.LoadCatalogFile(file);
        Assert.NotNull(catalog);
        Assert.Equal(expectedCount, catalog!.Rows.Count);
        Assert.Contains(catalog.Rows, row =>
            row.Values.Any(v => v.ValueKind == System.Text.Json.JsonValueKind.String
                && v.GetString()?.Contains(demoMarker) == true));
    }
}