using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileApprovalLegCreateRouteTests
{
    [Fact]
    public void ResolveEffectiveCreateRoute_PrefersCreationRoute()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            RequireTargetApplicationProfileInstance = true,
        };

        Assert.Equal(
            ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ApplicationProfileApprovalLegVersionHelper.ResolveEffectiveCreateRoute(
                profile,
                ApplicationProfileInstanceProgressRouteKind.ViaMinistries));
    }

    [Fact]
    public void TryResolveSharedProfileForCreate_SkipsLegsWhenCreationRouteIsDirect()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveSharedProfileForCreate(
            profile,
            null,
            objectSpace: null!,
            out var shared,
            out var error,
            ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService);

        Assert.True(ok);
        Assert.Null(shared);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolveSharedProfileForCreate_RequiresLegsWhenSeretmezlikCreatedVia()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            RequireTargetApplicationProfileInstance = true,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveSharedProfileForCreate(
            profile,
            null,
            objectSpace: null!,
            out _,
            out var error,
            ApplicationProfileInstanceProgressRouteKind.ViaMinistries);

        Assert.False(ok);
        Assert.Contains("approval-leg", error, StringComparison.OrdinalIgnoreCase);
    }
}