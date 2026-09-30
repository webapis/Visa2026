using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early validation for persisting the officer-chosen shared approval-leg default
/// (empty ids / missing profile / direct migration / inactive chain).
/// </summary>
public class ApplicationProfileApprovalLegTrySetTemplateDefaultGateTests
{
    [Fact]
    public void TrySetTemplateDefault_rejects_empty_ids()
    {
        var space = QueryableObjectSpaceStub.Create();

        Assert.False(ApplicationProfileApprovalLegVersionHelper.TrySetTemplateDefault(
            space, Guid.Empty, Guid.NewGuid(), out var error));
        Assert.Contains("Could not set", error, StringComparison.OrdinalIgnoreCase);

        Assert.False(ApplicationProfileApprovalLegVersionHelper.TrySetTemplateDefault(
            space, Guid.NewGuid(), Guid.Empty, out error));
        Assert.Contains("Could not set", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrySetTemplateDefault_rejects_missing_profile()
    {
        var space = QueryableObjectSpaceStub.Create();

        Assert.False(ApplicationProfileApprovalLegVersionHelper.TrySetTemplateDefault(
            space, Guid.NewGuid(), Guid.NewGuid(), out var error));
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolveSharedProfileForCreate_direct_migration_skips_catalog()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveSharedProfileForCreate(
            profile,
            requestedApprovalLegProfileId: null,
            QueryableObjectSpaceStub.Create(),
            out var shared,
            out var error);

        Assert.True(ok);
        Assert.Null(shared);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolveSharedProfileForCreate_rejects_unknown_requested_chain()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveSharedProfileForCreate(
            profile,
            Guid.NewGuid(),
            QueryableObjectSpaceStub.Create(),
            out var shared,
            out var error);

        Assert.False(ok);
        Assert.Null(shared);
        Assert.Contains("valid approval-leg", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadSharedProfileWithLegs_returns_null_for_empty_id()
    {
        Assert.Null(ApplicationProfileApprovalLegVersionHelper.LoadSharedProfileWithLegs(
            QueryableObjectSpaceStub.Create(), Guid.Empty));
    }
}
