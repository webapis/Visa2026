using System;
using System.Collections.Generic;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Dashboard → ListView pending handoff for cancel/ext/transfer/state sibling filter services
/// (RegistrationStateFilter / VisaFilter covered separately).
/// </summary>
public sealed class VisaSiblingPendingFilterServiceTests
{
    [Fact]
    public void VisaCancelExtFilterService_SetPending_RaisesEventAndTakeClears()
    {
        var service = new VisaCancelExtFilterService();
        string eventCriteria = null;
        string eventCaption = null;
        service.CriteriaRequested += (c, cap) =>
        {
            eventCriteria = c;
            eventCaption = cap;
        };

        service.SetPending("StartsWith([Number], 'X')", "Caption");

        Assert.Equal("StartsWith([Number], 'X')", eventCriteria);
        Assert.Equal("Caption", eventCaption);

        var taken = service.TakeAndClear();
        Assert.Equal("StartsWith([Number], 'X')", taken.Criteria);
        Assert.Equal("Caption", taken.Caption);

        var empty = service.TakeAndClear();
        Assert.Null(empty.Criteria);
        Assert.Null(empty.Caption);
    }

    [Fact]
    public void VisaCancellationFilterService_SetPending_RaisesEventAndTakeClears()
    {
        var service = new VisaCancellationFilterService();
        string eventCriteria = null;
        service.CriteriaRequested += (c, _) => eventCriteria = c;

        service.SetPending("[IsCancelled] = True", "Cancelled");

        Assert.Equal("[IsCancelled] = True", eventCriteria);
        Assert.Equal("[IsCancelled] = True", service.TakeAndClear().Criteria);
        Assert.Null(service.TakeAndClear().Criteria);
    }

    [Fact]
    public void VisaExtFilterService_SetPending_RaisesEventAndTakeClears()
    {
        var service = new VisaExtFilterService();
        string eventCaption = null;
        service.CriteriaRequested += (_, cap) => eventCaption = cap;

        service.SetPending("[OnExtension] = True", "Extensions");

        Assert.Equal("Extensions", eventCaption);
        Assert.Equal("Extensions", service.TakeAndClear().Caption);
        Assert.Null(service.TakeAndClear().Caption);
    }

    [Fact]
    public void VisaTransferFilterService_SetPending_RaisesEventAndTakeClears()
    {
        var service = new VisaTransferFilterService();
        service.SetPending("[Transfer] = True", "Transfers");

        var taken = service.TakeAndClear();
        Assert.Equal("[Transfer] = True", taken.Criteria);
        Assert.Equal("Transfers", taken.Caption);
        Assert.Null(service.TakeAndClear().Criteria);
    }

    [Fact]
    public void VisaStateFilterService_NullPersonIds_BecomesEmptyAndTakeClears()
    {
        var service = new VisaStateFilterService();
        IReadOnlyList<Guid> eventIds = null;
        string eventCaption = null;
        service.CriteriaRequested += (ids, caption) =>
        {
            eventIds = ids;
            eventCaption = caption;
        };

        service.SetPending(null!, "Expired visas");

        Assert.NotNull(eventIds);
        Assert.Empty(eventIds);
        Assert.Equal("Expired visas", eventCaption);

        var taken = service.TakeAndClear();
        Assert.Empty(taken.PersonIds);
        Assert.Equal("Expired visas", taken.Caption);

        var cleared = service.TakeAndClear();
        Assert.Empty(cleared.PersonIds);
        Assert.Null(cleared.Caption);
    }

    [Fact]
    public void VisaStateFilterService_PreservesPersonIdsUntilTaken()
    {
        var service = new VisaStateFilterService();
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        service.SetPending(new[] { id }, "Active");

        var taken = service.TakeAndClear();
        Assert.Equal(new[] { id }, taken.PersonIds);
        Assert.Equal("Active", taken.Caption);
    }
}
