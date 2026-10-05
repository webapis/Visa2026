using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationWorkspace;

public class ApplicationWorkspaceIssueOpenHelperGateTests
{
    [Fact]
    public void IssuedHeaderTryCreate_RejectsNullAppEmptyIdOrUnknownKey()
    {
        Assert.False(ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate(
            null!,
            sourceFrame: null,
            Guid.NewGuid(),
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation));

        Assert.False(ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate(
            null!,
            sourceFrame: null,
            Guid.Empty,
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation));

        Assert.False(ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate(
            null!,
            sourceFrame: null,
            Guid.NewGuid(),
            "not-a-catalog-key"));
    }

    [Fact]
    public void IssuedHeaderTryOpen_RejectsNullAppOrEmptyHeaderId()
    {
        Assert.False(ApplicationWorkspaceIssuedHeaderOpenHelper.TryOpen(
            null!,
            sourceFrame: null,
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
            Guid.NewGuid()));

        Assert.False(ApplicationWorkspaceIssuedHeaderOpenHelper.TryOpen(
            null!,
            sourceFrame: null,
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
            Guid.Empty));
    }

    [Fact]
    public void IssuedVisaTryOpenCompose_RejectsNullAppOrEmptyInstanceId()
    {
        Assert.False(ApplicationWorkspaceIssueIssuedVisaOpenHelper.TryOpenCompose(
            null!,
            Guid.NewGuid()));

        Assert.False(ApplicationWorkspaceIssueIssuedVisaOpenHelper.TryOpenCompose(
            null!,
            Guid.Empty));
    }

    [Fact]
    public void IssuedHeaderComposeHelper_RejectsNullAppEmptyIdOrBlankKey()
    {
        Assert.False(ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
            null!,
            Guid.NewGuid(),
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation));

        Assert.False(ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
            null!,
            Guid.Empty,
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit));

        Assert.False(ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
            null!,
            Guid.NewGuid(),
            "   "));

        Assert.False(ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
            null!,
            Guid.NewGuid(),
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa));
    }

    [Fact]
    public void DocumentCopiesTryOpen_RejectsNullApplication()
    {
        Assert.False(ApplicationWorkspaceDocumentCopiesOpenHelper.TryOpen(
            null!,
            Guid.NewGuid(),
            [Guid.NewGuid()]));
    }
}
