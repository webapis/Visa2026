using System.Text.Json;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Item-scoped Resminamalar batches persist selected ApplicationItem ids as JSON.
/// Wrong serialize/deserialize drops lines or re-includes empty Guids.
/// </summary>
public sealed class ApplicationWordReportPackageApplicationItemIdsHelperTests
{
    [Fact]
    public void Serialize_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(null));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(Array.Empty<Guid>()));
    }

    [Fact]
    public void Serialize_OnlyEmptyGuids_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(
            [Guid.Empty, Guid.Empty]));
    }

    [Fact]
    public void Serialize_DropsEmptyAndDedupes()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var json = ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(
            [a, Guid.Empty, a, b]);

        Assert.NotNull(json);
        var ids = JsonSerializer.Deserialize<List<Guid>>(json!);
        Assert.NotNull(ids);
        Assert.Equal(2, ids!.Count);
        Assert.Contains(a, ids);
        Assert.Contains(b, ids);
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("""["00000000-0000-0000-0000-000000000000"]""")]
    public void Deserialize_InvalidEmptyOrOnlyEmptyGuids_ReturnsNull(string json)
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(json));
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsDistinctIds()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var json = ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([a, b, a]);
        var restored = ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Count);
        Assert.Contains(a, restored);
        Assert.Contains(b, restored);
    }
}
