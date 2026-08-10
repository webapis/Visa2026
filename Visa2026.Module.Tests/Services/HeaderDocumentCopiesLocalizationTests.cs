using Visa2026.Module.Services.HeaderLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class HeaderDocumentCopiesLocalizationTests
{
    [Theory]
    [InlineData(HeaderDocumentCopiesFamily.WorkPermit, "WorkPermitDocumentCopies.Title")]
    [InlineData(HeaderDocumentCopiesFamily.Invitation, "InvitationDocumentCopies.Title")]
    [InlineData(HeaderDocumentCopiesFamily.Rejection, "RejectionDocumentCopies.Title")]
    [InlineData(HeaderDocumentCopiesFamily.BorderZone, "BorderZoneDocumentCopies.Title")]
    [InlineData((HeaderDocumentCopiesFamily)99, "HeaderDocumentCopies.Title")]
    public void TitleKey_MapsFamily(HeaderDocumentCopiesFamily family, string expectedKey)
    {
        Assert.Equal(expectedKey, HeaderDocumentCopiesLocalization.TitleKey(family));
    }

    [Theory]
    [InlineData(HeaderDocumentCopiesFamily.WorkPermit, "WorkPermitDocumentCopies.List.SelectOne")]
    [InlineData(HeaderDocumentCopiesFamily.Invitation, "InvitationDocumentCopies.List.SelectOne")]
    [InlineData(HeaderDocumentCopiesFamily.Rejection, "RejectionDocumentCopies.List.SelectOne")]
    [InlineData(HeaderDocumentCopiesFamily.BorderZone, "BorderZoneDocumentCopies.List.SelectOne")]
    [InlineData((HeaderDocumentCopiesFamily)99, "HeaderDocumentCopies.List.SelectOne")]
    public void ListSelectOneKey_MapsFamily(HeaderDocumentCopiesFamily family, string expectedKey)
    {
        Assert.Equal(expectedKey, HeaderDocumentCopiesLocalization.ListSelectOneKey(family));
    }

    [Theory]
    [InlineData(HeaderDocumentCopiesFamily.WorkPermit, "WorkPermitDocumentCopies.List.ColumnLink")]
    [InlineData(HeaderDocumentCopiesFamily.Invitation, "InvitationDocumentCopies.List.ColumnLink")]
    [InlineData(HeaderDocumentCopiesFamily.Rejection, "RejectionDocumentCopies.List.ColumnLink")]
    [InlineData(HeaderDocumentCopiesFamily.BorderZone, "BorderZoneDocumentCopies.List.ColumnLink")]
    [InlineData((HeaderDocumentCopiesFamily)99, "HeaderDocumentCopies.List.ColumnLink")]
    public void ListColumnLinkKey_MapsFamily(HeaderDocumentCopiesFamily family, string expectedKey)
    {
        Assert.Equal(expectedKey, HeaderDocumentCopiesLocalization.ListColumnLinkKey(family));
    }
}
