using System;
using System.Collections.Generic;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class NavigationPendingFilterServiceTests
{
    [Fact]
    public void RegistrationStateFilter_TakeAndClear_ReturnsNullWhenEmpty()
    {
        var service = new RegistrationStateFilterService();
        Assert.Null(service.TakeAndClear());
    }

    [Fact]
    public void RegistrationStateFilter_SetPending_RaisesAndTakeClears()
    {
        var service = new RegistrationStateFilterService();
        IReadOnlyList<Guid> seenIds = null;
        string seenCaption = null;
        string seenKey = null;
        service.CriteriaRequested += (ids, caption, key) =>
        {
            seenIds = ids;
            seenCaption = caption;
            seenKey = key;
        };

        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        service.SetPending(new[] { id }, "Pending visas", "expiring");

        Assert.Equal(new[] { id }, seenIds);
        Assert.Equal("Pending visas", seenCaption);
        Assert.Equal("expiring", seenKey);

        var taken = service.TakeAndClear();
        Assert.NotNull(taken);
        Assert.Equal(new[] { id }, taken.Value.VisaIds);
        Assert.Equal("Pending visas", taken.Value.Caption);
        Assert.Equal("expiring", taken.Value.StateKey);

        Assert.Null(service.TakeAndClear());
    }

    [Fact]
    public void RegistrationStateFilter_SetPending_NullIdsBecomesEmpty()
    {
        var service = new RegistrationStateFilterService();
        service.SetPending(null, "c", "k");
        var taken = service.TakeAndClear();
        Assert.NotNull(taken);
        Assert.Empty(taken.Value.VisaIds);
    }

    [Fact]
    public void VisaFilter_TakeAndClear_ReturnsPendingThenClears()
    {
        var service = new VisaFilterService();
        string seenCriteria = null;
        string seenCaption = null;
        service.CriteriaRequested += (criteria, caption) =>
        {
            seenCriteria = criteria;
            seenCaption = caption;
        };

        service.SetPending("[State] = 'Active'", "Active visas");
        Assert.Equal("[State] = 'Active'", seenCriteria);
        Assert.Equal("Active visas", seenCaption);

        var taken = service.TakeAndClear();
        Assert.Equal("[State] = 'Active'", taken.Criteria);
        Assert.Equal("Active visas", taken.Caption);

        var cleared = service.TakeAndClear();
        Assert.Null(cleared.Criteria);
        Assert.Null(cleared.Caption);
    }
}

public sealed class PdfPackagingNotesCultureResolverTests
{
    [Theory]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("en-US", "en-US")]
    [InlineData("tk-TM", "tk-TM")]
    [InlineData("tr", "tr-TR")]
    public void Resolve_RequestedCultureWinsWithoutObjectSpace(string requested, string expectedNormalized)
    {
        // requestedCulture short-circuits before any ObjectSpace query.
        var culture = PdfPackagingNotesCultureResolver.Resolve(objectSpace: null, requestedByUserName: "anyone", requestedCulture: requested);
        Assert.Equal(expectedNormalized, culture);
    }

    [Fact]
    public void Resolve_BlankUserFallsBackToDefaultCulture()
    {
        var culture = PdfPackagingNotesCultureResolver.Resolve(objectSpace: null, requestedByUserName: null, requestedCulture: null);
        Assert.Equal(VisaUiMessages.DefaultCultureName, culture);
    }

    [Fact]
    public void Resolve_WhitespaceRequestedCultureFallsThroughToDefaultWhenNoUser()
    {
        var culture = PdfPackagingNotesCultureResolver.Resolve(objectSpace: null, requestedByUserName: "  ", requestedCulture: "   ");
        Assert.Equal(VisaUiMessages.DefaultCultureName, culture);
    }
}
