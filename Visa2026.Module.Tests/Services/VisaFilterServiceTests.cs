using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaFilterServiceTests
{
    [Fact]
    public void SetPending_raises_event_and_TakeAndClear_returns_then_clears()
    {
        var service = new VisaFilterService();
        string? eventCriteria = null;
        string? eventCaption = null;
        service.CriteriaRequested += (criteria, caption) =>
        {
            eventCriteria = criteria;
            eventCaption = caption;
        };

        service.SetPending("[Person.ID] = ?", "Dashboard filter");

        Assert.Equal("[Person.ID] = ?", eventCriteria);
        Assert.Equal("Dashboard filter", eventCaption);

        var pending = service.TakeAndClear();
        Assert.Equal("[Person.ID] = ?", pending.Criteria);
        Assert.Equal("Dashboard filter", pending.Caption);

        var cleared = service.TakeAndClear();
        Assert.Null(cleared.Criteria);
        Assert.Null(cleared.Caption);
    }
}
