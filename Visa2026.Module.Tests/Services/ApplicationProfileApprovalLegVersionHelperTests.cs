using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileApprovalLegVersionHelperTests
{
    [Fact]
    public void TryResolveVersionForCreate_DirectMigration_SkipsVersion()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveVersionForCreate(
            profile, null, out var version, out var error);

        Assert.True(ok);
        Assert.Null(version);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolveVersionForCreate_ViaMinistry_RequiresNamedVersion()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveVersionForCreate(
            profile, null, out _, out var error);

        Assert.False(ok);
        Assert.Contains("no approval-leg versions", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolveVersionForCreate_ViaMinistry_AutoPicksSingleVersion()
    {
        var profile = ViaProfileWithVersions(1, out var only);

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveVersionForCreate(
            profile, null, out var version, out var error);

        Assert.True(ok);
        Assert.Same(only, version);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolveVersionForCreate_ViaMinistry_RequiresPickWhenMultiple()
    {
        var profile = ViaProfileWithVersions(2, out _);

        var ok = ApplicationProfileApprovalLegVersionHelper.TryResolveVersionForCreate(
            profile, null, out _, out var error);

        Assert.False(ok);
        Assert.Contains("Choose which approval-leg version", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveLegsForInstance_PrefersSnapshotOverLaterVersionEdit()
    {
        var profile = ViaProfileWithVersions(1, out var version);
        version.Legs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Live-A" },
        });

        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApprovalLegSnapshots =
            [
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 1, MinistryShortName = "Snap-A" },
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 2, MinistryShortName = "Snap-B" },
            ],
        };

        var legs = ApplicationProfileApprovalLegVersionHelper.ResolveLegsForInstance(application, profile);

        Assert.Equal(["Snap-A", "Snap-B"], legs.Select(l => l.Name).ToArray());
    }

    [Fact]
    public void EnsureSingleDefault_KeepsPreferred()
    {
        var profile = ViaProfileWithVersions(2, out var first);
        var second = profile.ApprovalLegVersions[1];
        first.IsDefault = true;
        second.IsDefault = true;

        ApplicationProfileApprovalLegVersionHelper.EnsureSingleDefault(profile, second);

        Assert.False(first.IsDefault);
        Assert.True(second.IsDefault);
    }

    private static ApplicationProfile ViaProfileWithVersions(
        int count,
        out ApplicationProfileApprovalLegVersion first)
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        first = null!;
        for (var i = 1; i <= count; i++)
        {
            var version = new ApplicationProfileApprovalLegVersion
            {
                ID = Guid.NewGuid(),
                Name = $"Version {i}",
                Sequence = i,
                IsDefault = i == 1,
                ApplicationProfile = profile,
            };
            profile.ApprovalLegVersions.Add(version);
            if (i == 1)
                first = version;
        }

        return profile;
    }
}