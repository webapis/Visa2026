using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// LinkableActiveCounts.Load early gates (including CancelVisas includeNotYetStartedVisas flag).
/// </summary>
public class ApplicationWorkspaceLinkableActiveCountsGateTests
{
    [Fact]
    public void Load_NullObjectSpaceOrPeople_ReturnsEmptyCounts()
    {
        var empty = ApplicationWorkspaceLinkableActiveCounts.Load(null, null, includeNotYetStartedVisas: true);
        Assert.Equal(0, empty.Get(Guid.NewGuid(), ApplicationProfileInstancePersonLinkKind.Visa));

        var noPeople = ApplicationWorkspaceLinkableActiveCounts.Load(
            null,
            Array.Empty<Person>(),
            includeNotYetStartedVisas: true);
        Assert.Equal(0, noPeople.Get(Guid.NewGuid(), ApplicationProfileInstancePersonLinkKind.Visa));
    }

    [Fact]
    public void Load_PeopleWithEmptyIds_ReturnsEmptyWithoutQuerying()
    {
        // Would throw if empty-id short-circuit were removed and GetObjectsQuery ran on null OS.
        var counts = ApplicationWorkspaceLinkableActiveCounts.Load(
            null,
            new List<Person> { new() { ID = Guid.Empty } },
            includeNotYetStartedVisas: true);

        Assert.Equal(0, counts.Get(Guid.Empty, ApplicationProfileInstancePersonLinkKind.Visa));
        Assert.Equal(0, counts.Get(Guid.NewGuid(), ApplicationProfileInstancePersonLinkKind.WorkPermitItem));
    }
}
