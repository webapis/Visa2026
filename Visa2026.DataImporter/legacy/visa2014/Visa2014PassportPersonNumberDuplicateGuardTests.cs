using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014PassportPersonNumberDuplicateGuardTests
{
    [Fact]
    public void Register_KeepsSmallerPassportIdForSamePair()
    {
        var guard = new Visa2014PassportPersonNumberDuplicateGuard();
        var personId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var larger = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var smaller = Guid.Parse("11111111-1111-1111-1111-111111111111");

        guard.Register(personId, " P-100 ", larger);
        guard.Register(personId, "P-100", smaller);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = personId,
            ["PassportNumber"] = "P-100",
        };

        Assert.Equal(smaller, guard.TryResolveFromPayload(payload));
        Assert.Equal(1, guard.LoadedPairCount);
    }

    [Fact]
    public void TryResolveFromPayload_MissingPersonOrNumber_ReturnsNull()
    {
        var guard = new Visa2014PassportPersonNumberDuplicateGuard();
        guard.Register(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "P-1",
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Null(guard.TryResolveFromPayload(new Dictionary<string, object?> { ["PassportNumber"] = "P-1" }));
        Assert.Null(guard.TryResolveFromPayload(new Dictionary<string, object?>
        {
            ["Person"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ["PassportNumber"] = "   ",
        }));
    }

    [Fact]
    public void RegisterFromPayload_NestedPersonId_Resolves()
    {
        var guard = new Visa2014PassportPersonNumberDuplicateGuard();
        var personId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var passportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nested = new { ID = personId };

        guard.RegisterFromPayload(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Person"] = nested,
                ["PassportNumber"] = "AA123",
            },
            passportId);

        Assert.Equal(
            passportId,
            guard.TryResolveFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Person"] = personId,
                ["PassportNumber"] = "AA123",
            }));
    }

    [Fact]
    public async Task LoadFromSqlAsync_BlankConnection_ReturnsEmptyGuard()
    {
        var guard = await Visa2014PassportPersonNumberDuplicateGuard
            .LoadFromSqlAsync("", verbose: false);

        Assert.Equal(0, guard.LoadedPairCount);
    }
}
