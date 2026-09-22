using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class RegistrationStateFilterServiceTests
{
    [Fact]
    public void TakeAndClear_when_empty_returns_null()
    {
        var service = new RegistrationStateFilterService();

        Assert.Null(service.TakeAndClear());
    }

    [Fact]
    public void SetPending_raises_event_and_TakeAndClear_returns_then_clears()
    {
        var service = new RegistrationStateFilterService();
        var ids = new[] { Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        IReadOnlyList<Guid>? eventIds = null;
        string? eventCaption = null;
        string? eventStateKey = null;
        service.CriteriaRequested += (visaIds, caption, stateKey) =>
        {
            eventIds = visaIds;
            eventCaption = caption;
            eventStateKey = stateKey;
        };

        service.SetPending(ids, "To check in", "check-in");

        Assert.Same(ids, eventIds);
        Assert.Equal("To check in", eventCaption);
        Assert.Equal("check-in", eventStateKey);

        var pending = service.TakeAndClear();
        Assert.NotNull(pending);
        Assert.Equal(ids, pending!.Value.VisaIds);
        Assert.Equal("To check in", pending.Value.Caption);
        Assert.Equal("check-in", pending.Value.StateKey);

        Assert.Null(service.TakeAndClear());
    }

    [Fact]
    public void SetPending_null_visaIds_becomes_empty_list()
    {
        var service = new RegistrationStateFilterService();

        service.SetPending(null!, "caption", "key");

        var pending = service.TakeAndClear();
        Assert.NotNull(pending);
        Assert.Empty(pending!.Value.VisaIds!);
        Assert.Equal("caption", pending.Value.Caption);
        Assert.Equal("key", pending.Value.StateKey);
    }
}
