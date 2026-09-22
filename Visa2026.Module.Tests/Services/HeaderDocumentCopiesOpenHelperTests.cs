using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class HeaderDocumentCopiesOpenHelperTests
{
    [Theory]
    [InlineData(typeof(WorkPermit), HeaderDocumentCopiesFamily.WorkPermit)]
    [InlineData(typeof(WorkPermitItem), HeaderDocumentCopiesFamily.WorkPermit)]
    [InlineData(typeof(Invitation), HeaderDocumentCopiesFamily.Invitation)]
    [InlineData(typeof(InvitationItem), HeaderDocumentCopiesFamily.Invitation)]
    [InlineData(typeof(Rejection), HeaderDocumentCopiesFamily.Rejection)]
    [InlineData(typeof(RejectionItem), HeaderDocumentCopiesFamily.Rejection)]
    [InlineData(typeof(BorderZone), HeaderDocumentCopiesFamily.BorderZone)]
    [InlineData(typeof(BorderZoneItem), HeaderDocumentCopiesFamily.BorderZone)]
    public void TryGetFamilyForType_maps_parent_and_item_types(Type objectType, HeaderDocumentCopiesFamily expected)
    {
        var ok = HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(objectType, out var family);

        Assert.True(ok);
        Assert.Equal(expected, family);
    }

    [Fact]
    public void TryGetFamilyForType_null_returns_false()
    {
        var ok = HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(null, out var family);

        Assert.False(ok);
        Assert.Equal(default, family);
    }

    [Fact]
    public void TryGetFamilyForType_unrelated_type_returns_false()
    {
        var ok = HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(typeof(Person), out var family);

        Assert.False(ok);
        Assert.Equal(default, family);
    }
}
