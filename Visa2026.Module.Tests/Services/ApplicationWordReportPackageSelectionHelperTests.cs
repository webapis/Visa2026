using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationWordReportPackageSelectionHelperTests
{
    [Fact]
    public void Serialize_NullOrEmptyOrWhitespaceOnly_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Serialize(null));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Serialize(Array.Empty<string>()));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Serialize(["", "  "]));
    }

    [Fact]
    public void Serialize_DistinctOrdinal_PreservesKeys()
    {
        var json = ApplicationWordReportPackageSelectionHelper.Serialize(
            ["Forma_16", "Forma_16", "Borcnama", "  "]);

        Assert.NotNull(json);
        var keys = JsonSerializer.Deserialize<List<string>>(json!);
        Assert.Equal(["Forma_16", "Borcnama"], keys);
    }

    [Fact]
    public void Deserialize_InvalidOrEmptyJson_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Deserialize(null));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Deserialize(" "));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Deserialize("not-json"));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Deserialize("[]"));
        Assert.Null(ApplicationWordReportPackageSelectionHelper.Deserialize("""[""]"""));
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsDistinctKeys()
    {
        var original = new[] { "A", "B", "A" };
        var json = ApplicationWordReportPackageSelectionHelper.Serialize(original);
        var restored = ApplicationWordReportPackageSelectionHelper.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Count);
        Assert.Contains("A", restored);
        Assert.Contains("B", restored);
    }

    [Fact]
    public void NormalizeSelection_NullRequested_ReturnsAllCatalogKeys()
    {
        var catalog = Catalog("one", "two");

        var selected = ApplicationWordReportPackageSelectionHelper.NormalizeSelection(catalog, null);

        Assert.Equal(2, selected.Count);
        Assert.Contains("one", selected);
        Assert.Contains("two", selected);
    }

    [Fact]
    public void NormalizeSelection_DropsUnknownAndWhitespace_Dedupes()
    {
        var catalog = Catalog("one", "two");

        var selected = ApplicationWordReportPackageSelectionHelper.NormalizeSelection(
            catalog,
            ["two", "two", "missing", "", "one"]);

        Assert.Equal(2, selected.Count);
        Assert.Contains("one", selected);
        Assert.Contains("two", selected);
        Assert.DoesNotContain("missing", selected);
    }

    [Fact]
    public void ApplicationItemIds_Serialize_FiltersEmptyAndDedupes()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(null));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([Guid.Empty]));

        var json = ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([a, Guid.Empty, a, b]);
        Assert.NotNull(json);

        var restored = ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(json);
        Assert.NotNull(restored);
        Assert.Equal([a, b], restored);
    }

    [Fact]
    public void ApplicationItemIds_Deserialize_InvalidJson_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize("not-json"));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize("[]"));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(
            """["00000000-0000-0000-0000-000000000000"]"""));
    }

    private static IReadOnlyList<ApplicationWordReportPackageCatalogEntry> Catalog(params string[] keys) =>
        keys.Select(key => new ApplicationWordReportPackageCatalogEntry
        {
            EntryKey = key,
            DisplayName = key
        }).ToList();
}
