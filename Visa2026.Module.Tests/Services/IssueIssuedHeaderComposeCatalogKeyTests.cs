using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssueIssuedHeaderComposeCatalogKeyTests
{
    [Theory]
    [InlineData(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, IssueIssuedHeaderKind.Invitation)]
    [InlineData(ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, IssueIssuedHeaderKind.WorkPermit)]
    [InlineData(ApplicationWorkspaceIssuedRecordsCatalog.Rejection, IssueIssuedHeaderKind.Rejection)]
    [InlineData(ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, IssueIssuedHeaderKind.BorderZone)]
    [InlineData("INVITATION", IssueIssuedHeaderKind.Invitation)]
    [InlineData("workpermit", IssueIssuedHeaderKind.WorkPermit)]
    public void TryResolveKind_AcceptsIssuedHeaderCatalogKeys(string key, IssueIssuedHeaderKind expected)
    {
        Assert.True(IssueIssuedHeaderComposeService.TryResolveKind(key, out var kind));
        Assert.Equal(expected, kind);
        Assert.NotNull(ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(
            IssueIssuedHeaderComposeService.CatalogKeyFor(kind)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("issuedVisa")]
    [InlineData("inv")]
    [InlineData("passport")]
    public void TryResolveKind_RejectsNonComposeCatalogKeys(string? key)
    {
        Assert.False(IssueIssuedHeaderComposeService.TryResolveKind(key!, out var kind));
        Assert.Equal(default, kind);
    }

    [Fact]
    public void CatalogKeyFor_RoundTripsKnownKinds_AndEmptyForUnknown()
    {
        Assert.Equal(
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
            IssueIssuedHeaderComposeService.CatalogKeyFor(IssueIssuedHeaderKind.Invitation));
        Assert.Equal(
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            IssueIssuedHeaderComposeService.CatalogKeyFor(IssueIssuedHeaderKind.WorkPermit));
        Assert.Equal(
            ApplicationWorkspaceIssuedRecordsCatalog.Rejection,
            IssueIssuedHeaderComposeService.CatalogKeyFor(IssueIssuedHeaderKind.Rejection));
        Assert.Equal(
            ApplicationWorkspaceIssuedRecordsCatalog.BorderZone,
            IssueIssuedHeaderComposeService.CatalogKeyFor(IssueIssuedHeaderKind.BorderZone));
        Assert.Equal(string.Empty, IssueIssuedHeaderComposeService.CatalogKeyFor((IssueIssuedHeaderKind)99));
    }

    [Fact]
    public void Delete_RejectsMissingObjectSpaceOrIds()
    {
        var missingSpace = IssueIssuedHeaderComposeService.Delete(
            null!,
            Guid.NewGuid(),
            IssueIssuedHeaderKind.Invitation,
            Guid.NewGuid());
        Assert.False(missingSpace.Succeeded);
        Assert.Equal("Delete is not available.", missingSpace.ErrorMessage);

        var missingIds = IssueIssuedHeaderComposeService.Delete(
            null!,
            Guid.Empty,
            IssueIssuedHeaderKind.WorkPermit,
            Guid.Empty);
        Assert.False(missingIds.Succeeded);
        Assert.Equal("Delete is not available.", missingIds.ErrorMessage);
    }

    [Fact]
    public void AddDocument_RejectsMissingObjectSpaceOrIds()
    {
        var result = IssueIssuedHeaderComposeService.AddDocument(
            null!,
            Guid.Empty,
            IssueIssuedHeaderKind.Invitation,
            Guid.Empty,
            "scan.pdf",
            [1, 2, 3]);

        Assert.False(result.Succeeded);
        Assert.Equal("Upload is not available.", result.ErrorMessage);
    }
}
