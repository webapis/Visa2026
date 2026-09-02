using System.Text.Json;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationWordReportPackageApplicationItemIdsHelperTests
{
    [Fact]
    public void Serialize_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(null));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize(Array.Empty<Guid>()));
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([Guid.Empty, Guid.Empty]));
    }

    [Fact]
    public void Serialize_DedupesAndDropsEmpty()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var json = ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([Guid.Empty, a, a, b]);

        Assert.NotNull(json);
        var ids = JsonSerializer.Deserialize<List<Guid>>(json!);
        Assert.Equal(new[] { a, b }, ids);
    }

    [Fact]
    public void Deserialize_RoundTripsSerializedIds()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var json = ApplicationWordReportPackageApplicationItemIdsHelper.Serialize([a])!;

        var ids = ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(json);

        Assert.NotNull(ids);
        Assert.Equal(new[] { a }, ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("[\"00000000-0000-0000-0000-000000000000\"]")]
    public void Deserialize_BlankInvalidOrEmpty_ReturnsNull(string? json)
    {
        Assert.Null(ApplicationWordReportPackageApplicationItemIdsHelper.Deserialize(json));
    }
}
