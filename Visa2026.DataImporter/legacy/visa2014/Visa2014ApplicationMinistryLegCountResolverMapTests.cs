using System.Collections.ObjectModel;
using Bo = Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

/// <summary>
/// Pure mapping / resolve edges used when synthesizing ministry progress legs
/// from Visa2026 snapshots or approval profiles.
/// </summary>
public sealed class Visa2014ApplicationMinistryLegCountResolverMapTests
{
    [Fact]
    public void ResolveLegCount_PrefersNonEmptySnapshotsOverProfileMap()
    {
        var profileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var application = new Bo.Application
        {
            ApprovalLegSnapshots = new ObservableCollection<Bo.ApplicationApprovalLegSnapshot>
            {
                new() { MinistryShortName = "Energetika" },
                new() { MinistryShortName = "Gurlusyk" },
                new() { MinistryShortName = "   " },
            },
            ApprovalLegProfile = new Bo.ApprovalLegProfile { ID = profileId },
        };

        var fromMap = new Dictionary<Guid, int> { [profileId] = 5 };

        Assert.Equal(2, Visa2014ApplicationMinistryLegCountResolver.ResolveLegCount(application, fromMap));
    }

    [Fact]
    public void ResolveLegCount_UsesProfileMapWhenSnapshotsEmpty()
    {
        var profileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var application = new Bo.Application
        {
            ApprovalLegSnapshots = new ObservableCollection<Bo.ApplicationApprovalLegSnapshot>(),
            ApprovalLegProfile = new Bo.ApprovalLegProfile { ID = profileId },
        };

        var fromMap = new Dictionary<Guid, int> { [profileId] = 3 };

        Assert.Equal(3, Visa2014ApplicationMinistryLegCountResolver.ResolveLegCount(application, fromMap));
    }

    [Fact]
    public void ResolveLegCount_FallsBackToLiveProfileLegsWhenMapMissing()
    {
        var application = new Bo.Application
        {
            ApprovalLegSnapshots = new ObservableCollection<Bo.ApplicationApprovalLegSnapshot>(),
            ApprovalLegProfile = new Bo.ApprovalLegProfile
            {
                ID = Guid.NewGuid(),
                MinistryLegs = new ObservableCollection<Bo.ApprovalLegProfileMinistryLeg>
                {
                    new() { ApprovingMinistry = new Bo.ApprovingMinistry { ShortNameTm = "A" } },
                    new() { ApprovingMinistry = null },
                }
            },
        };

        Assert.Equal(1, Visa2014ApplicationMinistryLegCountResolver.ResolveLegCount(application));
    }

    [Fact]
    public void ResolveLegCount_ReturnsZeroWithoutProfileOrSnapshots()
    {
        var application = new Bo.Application
        {
            ApprovalLegSnapshots = new ObservableCollection<Bo.ApplicationApprovalLegSnapshot>(),
            ApprovalLegProfile = null,
        };

        Assert.Equal(0, Visa2014ApplicationMinistryLegCountResolver.ResolveLegCount(application));
    }

    [Fact]
    public void MapLegacyLegCounts_CopiesPositiveCountsOnly()
    {
        var legacyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var legacyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var targetA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var targetB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var legacyToTarget = new Dictionary<Guid, Guid>
        {
            [legacyA] = targetA,
            [legacyB] = targetB,
        };
        var targetCounts = new Dictionary<Guid, int>
        {
            [targetA] = 2,
            [targetB] = 0,
        };

        var mapped = Visa2014ApplicationMinistryLegCountResolver.MapLegacyLegCounts(
            legacyToTarget,
            targetCounts);

        Assert.Single(mapped);
        Assert.Equal(2, mapped[legacyA]);
        Assert.False(mapped.ContainsKey(legacyB));
    }

    [Fact]
    public void ResolveTargetApplicationIdsInScope_ReturnsTargetsWithPositiveLegs()
    {
        var legacyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var legacyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var targetA = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var targetB = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1");

        var legacyToTarget = new Dictionary<Guid, Guid>
        {
            [legacyA] = targetA,
            [legacyB] = targetB,
        };
        var targetCounts = new Dictionary<Guid, int>
        {
            [targetA] = 1,
            [targetB] = 0,
        };

        var inScope = Visa2014ApplicationMinistryLegCountResolver.ResolveTargetApplicationIdsInScope(
            legacyToTarget,
            targetCounts);

        Assert.Equal(new HashSet<Guid> { targetA }, inScope);
    }
}
