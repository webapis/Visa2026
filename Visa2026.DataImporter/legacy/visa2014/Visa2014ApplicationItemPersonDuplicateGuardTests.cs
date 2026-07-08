using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ApplicationItemPersonDuplicateGuardTests
{
    [Fact]
    public void TryResolveFromPayload_finds_canonical_item_for_application_person_pair()
    {
        var applicationId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        var guard = new Visa2014ApplicationItemPersonDuplicateGuard();
        guard.Register(applicationId, personId, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = applicationId },
            ["Person"] = new { ID = personId },
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void Register_keeps_lowest_item_id_for_same_pair()
    {
        var applicationId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var guard = new Visa2014ApplicationItemPersonDuplicateGuard();
        guard.Register(applicationId, personId, higherId);
        guard.Register(applicationId, personId, lowerId);

        Assert.True(guard.TryGetCanonical(applicationId, personId, out var itemId));
        Assert.Equal(lowerId, itemId);
    }

    [Fact]
    public void TryResolveFromPayload_returns_null_when_parents_missing()
    {
        var guard = new Visa2014ApplicationItemPersonDuplicateGuard();
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = Guid.NewGuid() },
        };

        Assert.Null(guard.TryResolveFromPayload(payload));
    }
}