using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early gates for skip-nav DetachForDelete before a child BO delete (join FK would block EF).
/// </summary>
public class ApplicationProfileInstanceChildDetachForDeleteGateTests
{
    [Fact]
    public void DetachForDelete_NullObjectSpace_DoesNotThrow()
    {
        ApplicationProfileInstanceChildMembership.DetachForDelete(
            null!,
            ApplicationProfileInstancePersonLinkKind.Visa,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    [Fact]
    public void DetachForDelete_EmptyLinkedObjectId_DoesNotThrow()
    {
        ApplicationProfileInstanceChildMembership.DetachForDelete(
            null!,
            ApplicationProfileInstancePersonLinkKind.Passport,
            Guid.Empty);
    }

    [Fact]
    public void DetachForDelete_RejectionItem_HasNoSkipNavJoin_DoesNotQueryObjectSpace()
    {
        // RejectionItem is not in TryJoin; would throw if the gate fell through to GetObjectsQuery on null OS.
        ApplicationProfileInstanceChildMembership.DetachForDelete(
            null!,
            ApplicationProfileInstancePersonLinkKind.RejectionItem,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    }
}
