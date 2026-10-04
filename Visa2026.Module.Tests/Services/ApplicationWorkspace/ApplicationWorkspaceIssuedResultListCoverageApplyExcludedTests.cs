using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationWorkspace;

/// <summary>
/// ApplyTo must subtract Seretmezlik-excluded people from ListView expected counts
/// (same as Result-tab Netije syny). Regression: FK rewrite to ExclusionPerson left
/// expected roster inflated when exclusions were ignored.
/// </summary>
public sealed class ApplicationWorkspaceIssuedResultListCoverageApplyExcludedTests
{
    [Fact]
    public void ApplyTo_null_or_empty_applications_is_noop()
    {
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(null, null!);
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(
            MultiTypeSeedObjectSpaceStub.Create(),
            Array.Empty<ApplicationProfileInstance>());
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(
            MultiTypeSeedObjectSpaceStub.Create(),
            null!);
    }

    [Fact]
    public void ApplyTo_subtracts_excluded_people_from_expected_when_roster_query_empty()
    {
        var instanceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var exclusionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var excludedPersonId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var application = new ApplicationProfileInstance
        {
            ID = instanceId,
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
                ProduceWorkPermit = false,
                ProduceRejection = false,
                ProduceBorderZone = false,
            },
        };
        application.SetListViewTotalPersonCount(4);

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(new ApplicationProfileInstanceExclusion
            {
                ID = exclusionId,
                ApplicationProfileInstanceId = instanceId,
            });
            stub.Seed(new ApplicationProfileInstanceExclusionPerson
            {
                ExclusionId = exclusionId,
                PersonId = excludedPersonId,
            });
            // Empty Guid PersonId must not inflate excluded count.
            stub.Seed(new ApplicationProfileInstanceExclusionPerson
            {
                ExclusionId = exclusionId,
                PersonId = Guid.Empty,
            });
        });

        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(space, [application]);

        var chips = application.ListViewResultCoverageChips;
        Assert.Equal(2, chips.Count);
        Assert.All(chips, c => Assert.Equal(3, c.ExpectedCount));
        Assert.Contains(chips, c => c.Key == ApplicationWorkspaceIssuedRecordsCatalog.Invitation);
        Assert.Contains(chips, c => c.Key == ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa);
    }

    [Fact]
    public void ApplyTo_without_exclusions_keeps_total_person_count_as_expected()
    {
        var instanceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var application = new ApplicationProfileInstance
        {
            ID = instanceId,
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };
        application.SetListViewTotalPersonCount(2);

        var space = MultiTypeSeedObjectSpaceStub.Create();
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(space, [application]);

        var chip = Assert.Single(application.ListViewResultCoverageChips);
        Assert.Equal(2, chip.ExpectedCount);
        Assert.Equal(0, chip.CoverageCount);
        Assert.Equal("miss", ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
    }

    [Fact]
    public void ApplyTo_skips_instances_with_empty_id()
    {
        var application = new ApplicationProfileInstance
        {
            ID = Guid.Empty,
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };
        application.SetListViewTotalPersonCount(5);

        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(
            MultiTypeSeedObjectSpaceStub.Create(),
            [application]);

        // ids filter drops empty Guid → roster/excluded empty → expected = TotalPersonCount - 0
        var chip = Assert.Single(application.ListViewResultCoverageChips);
        Assert.Equal(5, chip.ExpectedCount);
    }
}
