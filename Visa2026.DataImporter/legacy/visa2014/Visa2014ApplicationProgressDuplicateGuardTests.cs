using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ApplicationProgressDuplicateGuardTests
{
    [Fact]
    public void TryResolveFromPayload_finds_canonical_row_for_application_order()
    {
        var applicationId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        var guard = new Visa2014ApplicationProgressDuplicateGuard();
        guard.Register(applicationId, 3, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = applicationId },
            ["Order"] = 3,
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void Register_keeps_lowest_id_for_same_pair()
    {
        var applicationId = Guid.NewGuid();
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var guard = new Visa2014ApplicationProgressDuplicateGuard();
        guard.Register(applicationId, 1, higherId);
        guard.Register(applicationId, 1, lowerId);

        Assert.Equal(lowerId, guard.TryResolveFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = applicationId },
            ["Order"] = 1,
        }));
    }

    [Fact]
    public void TryResolveFromPayload_returns_null_when_order_missing()
    {
        var guard = new Visa2014ApplicationProgressDuplicateGuard();
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = Guid.NewGuid() },
        };
        Assert.Null(guard.TryResolveFromPayload(payload));
    }
}