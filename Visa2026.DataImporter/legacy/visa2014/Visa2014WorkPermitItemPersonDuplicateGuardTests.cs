using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014WorkPermitItemPersonDuplicateGuardTests
{
    [Fact]
    public void Register_KeepsSmallerItemIdForSamePair()
    {
        var guard = new Visa2014WorkPermitItemPersonDuplicateGuard();
        var workPermitId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var personId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var larger = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var smaller = Guid.Parse("11111111-1111-1111-1111-111111111111");

        guard.Register(workPermitId, personId, larger);
        guard.Register(workPermitId, personId, smaller);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["WorkPermit"] = workPermitId,
            ["Person"] = personId,
        };

        Assert.Equal(smaller, guard.TryResolveFromPayload(payload));
        Assert.Equal(1, guard.LoadedPairCount);
    }

    [Fact]
    public void TryResolveFromPayload_MissingParents_ReturnsNull()
    {
        var guard = new Visa2014WorkPermitItemPersonDuplicateGuard();
        guard.Register(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Null(guard.TryResolveFromPayload(new Dictionary<string, object?>
        {
            ["Person"] = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        }));
        Assert.Null(guard.TryResolveFromPayload(new Dictionary<string, object?>
        {
            ["WorkPermit"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        }));
    }

    [Fact]
    public void RegisterFromPayload_NestedParentIds_Resolves()
    {
        var guard = new Visa2014WorkPermitItemPersonDuplicateGuard();
        var workPermitId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var personId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        guard.RegisterFromPayload(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["WorkPermit"] = new { ID = workPermitId },
                ["Person"] = new { ID = personId },
            },
            itemId);

        Assert.Equal(
            itemId,
            guard.TryResolveFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["WorkPermit"] = workPermitId,
                ["Person"] = personId,
            }));
    }

    [Fact]
    public async Task LoadFromSqlAsync_BlankConnection_ReturnsEmptyGuard()
    {
        var guard = await Visa2014WorkPermitItemPersonDuplicateGuard
            .LoadFromSqlAsync("", verbose: false);

        Assert.Equal(0, guard.LoadedPairCount);
    }
}
