using System;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class HeaderDocumentCopiesListLinkResolutionTests
{
    private static readonly Guid ParentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void TryResolve_NullOrUnknown_ReturnsFalse()
    {
        Assert.False(HeaderDocumentCopiesListLinkResolution.TryResolve(null, out _, out _, out _));
        Assert.False(HeaderDocumentCopiesListLinkResolution.TryResolve(new Person(), out _, out _, out _));
    }

    [Theory]
    [InlineData(typeof(WorkPermit), HeaderDocumentCopiesFamily.WorkPermit)]
    [InlineData(typeof(Invitation), HeaderDocumentCopiesFamily.Invitation)]
    [InlineData(typeof(Rejection), HeaderDocumentCopiesFamily.Rejection)]
    [InlineData(typeof(BorderZone), HeaderDocumentCopiesFamily.BorderZone)]
    public void TryResolve_ParentRow_SetsFamilyAndParentId(Type parentType, HeaderDocumentCopiesFamily family)
    {
        var parent = (BaseObject)Activator.CreateInstance(parentType)!;
        parent.ID = ParentId;

        Assert.True(HeaderDocumentCopiesListLinkResolution.TryResolve(
            parent, out var resolvedFamily, out var parentId, out var contextItemId));

        Assert.Equal(family, resolvedFamily);
        Assert.Equal(ParentId, parentId);
        Assert.Null(contextItemId);
    }

    [Fact]
    public void TryResolve_ParentWithEmptyId_ReturnsFalse()
    {
        var invitation = new Invitation { ID = Guid.Empty };

        Assert.False(HeaderDocumentCopiesListLinkResolution.TryResolve(
            invitation, out _, out _, out _));
    }

    [Fact]
    public void TryResolve_WorkPermitItem_UsesParentAndContextItem()
    {
        var parent = new WorkPermit { ID = ParentId };
        var item = new WorkPermitItem { ID = ItemId, WorkPermit = parent };

        Assert.True(HeaderDocumentCopiesListLinkResolution.TryResolve(
            item, out var family, out var parentId, out var contextItemId));

        Assert.Equal(HeaderDocumentCopiesFamily.WorkPermit, family);
        Assert.Equal(ParentId, parentId);
        Assert.Equal(ItemId, contextItemId);
    }

    [Fact]
    public void TryResolve_InvitationItem_WithoutParent_ReturnsFalse()
    {
        var item = new InvitationItem { ID = ItemId, Invitation = null };

        Assert.False(HeaderDocumentCopiesListLinkResolution.TryResolve(
            item, out _, out _, out _));
    }

    [Fact]
    public void TryResolve_InvitationItem_SetsContext()
    {
        var parent = new Invitation { ID = ParentId };
        var item = new InvitationItem { ID = ItemId, Invitation = parent };

        Assert.True(HeaderDocumentCopiesListLinkResolution.TryResolve(
            item, out var family, out var parentId, out var contextItemId));

        Assert.Equal(HeaderDocumentCopiesFamily.Invitation, family);
        Assert.Equal(ParentId, parentId);
        Assert.Equal(ItemId, contextItemId);
    }

    [Fact]
    public void TryResolve_RejectionItem_And_BorderZoneItem()
    {
        var rejection = new Rejection { ID = ParentId };
        var rejectionItem = new RejectionItem { ID = ItemId, Rejection = rejection };
        Assert.True(HeaderDocumentCopiesListLinkResolution.TryResolve(
            rejectionItem, out var rejectionFamily, out var rejectionParentId, out var rejectionContext));
        Assert.Equal(HeaderDocumentCopiesFamily.Rejection, rejectionFamily);
        Assert.Equal(ParentId, rejectionParentId);
        Assert.Equal(ItemId, rejectionContext);

        var borderZone = new BorderZone { ID = ParentId };
        var borderZoneItem = new BorderZoneItem { ID = ItemId, BorderZone = borderZone };
        Assert.True(HeaderDocumentCopiesListLinkResolution.TryResolve(
            borderZoneItem, out var bzFamily, out var bzParentId, out var bzContext));
        Assert.Equal(HeaderDocumentCopiesFamily.BorderZone, bzFamily);
        Assert.Equal(ParentId, bzParentId);
        Assert.Equal(ItemId, bzContext);
    }
}
