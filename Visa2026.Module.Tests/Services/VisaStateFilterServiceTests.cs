using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaStateFilterServiceTests
{
    [Fact]
    public void SetPending_raises_event_and_TakeAndClear_returns_then_clears()
    {
        var service = new VisaStateFilterService();
        var ids = new[] { Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") };
        IReadOnlyList<Guid>? eventIds = null;
        string? eventCaption = null;
        service.CriteriaRequested += (personIds, caption) =>
        {
            eventIds = personIds;
            eventCaption = caption;
        };

        service.SetPending(ids, "Expiring soon");

        Assert.Same(ids, eventIds);
        Assert.Equal("Expiring soon", eventCaption);

        var pending = service.TakeAndClear();
        Assert.Equal(ids, pending.PersonIds);
        Assert.Equal("Expiring soon", pending.Caption);

        var cleared = service.TakeAndClear();
        Assert.Empty(cleared.PersonIds);
        Assert.Null(cleared.Caption);
    }

    [Fact]
    public void SetPending_null_personIds_becomes_empty_list()
    {
        var service = new VisaStateFilterService();

        service.SetPending(null!, "All");

        var pending = service.TakeAndClear();
        Assert.Empty(pending.PersonIds);
        Assert.Equal("All", pending.Caption);
    }
}
