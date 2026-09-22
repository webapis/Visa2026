using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014SyncPayloadFkHelperTests
{
    private sealed class NestedId
    {
        public Guid ID { get; init; }
    }

    [Fact]
    public void TryGetPayloadFkId_DirectGuid_Succeeds()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var payload = new Dictionary<string, object?> { ["Person"] = id };

        Assert.True(Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void TryGetPayloadFkId_NestedIdProperty_Succeeds()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var payload = new Dictionary<string, object?>
        {
            ["Passport"] = new NestedId { ID = id },
        };

        Assert.True(Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Passport", out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void TryGetPayloadFkId_MissingNullOrWrongShape_Fails()
    {
        var payload = new Dictionary<string, object?>
        {
            ["Person"] = null,
            ["Other"] = "not-an-id",
        };

        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Missing", out _));
        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out _));
        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Other", out _));
    }

    [Fact]
    public void TryGetPayloadString_TrimsAndRejectsBlank()
    {
        var payload = new Dictionary<string, object?>
        {
            ["Code"] = "  ABC  ",
            ["Blank"] = "   ",
            ["Number"] = 12,
        };

        Assert.True(Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "Code", out var value));
        Assert.Equal("ABC", value);
        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "Blank", out _));
        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "Number", out _));
        Assert.False(Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "Missing", out _));
    }
}
