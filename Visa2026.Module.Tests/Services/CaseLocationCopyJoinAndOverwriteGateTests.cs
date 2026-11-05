using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Null/empty gates for Case→WorkPermit and Case→BorderZoneItem location copy
/// (AcceptOnJoin / TryOverwriteFromCase / TryApplyEditedLocations / ShowInLinkReview).
/// </summary>
public class CaseLocationCopyJoinAndOverwriteGateTests
{
    [Fact]
    public void WorkPermit_AcceptOnJoin_null_object_space_returns_false()
    {
        var application = WorkPermitEnabled("Mary");
        Assert.False(CaseWorkPermitLocationCopy.AcceptOnJoin(
            null!,
            application,
            new Person { ID = Guid.NewGuid() }));
        Assert.False(CaseWorkPermitLocationCopy.AcceptOnJoin(
            null!,
            application,
            new Person { ID = Guid.Empty }));
    }

    [Fact]
    public void WorkPermit_TryOverwriteFromCase_requires_ids_or_object_space()
    {
        Assert.False(CaseWorkPermitLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            out var error));
        Assert.Equal("Missing work permit link.", error);

        Assert.False(CaseWorkPermitLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            out error));
        Assert.Equal("Missing work permit link.", error);

        Assert.False(CaseWorkPermitLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            out error));
        Assert.Equal("Missing work permit link.", error);

        Assert.False(CaseWorkPermitLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            out error));
        Assert.Equal("Missing work permit link.", error);
    }

    [Fact]
    public void WorkPermit_TryApplyEditedLocations_null_object_space_returns_false()
    {
        var person = new Person { ID = Guid.NewGuid() };
        var application = WorkPermitEnabled("Mary");
        Assert.False(CaseWorkPermitLocationCopy.TryApplyEditedLocations(
            null!,
            application,
            person,
            Guid.NewGuid(),
            "Mary"));
    }

    [Fact]
    public void WorkPermit_ShowInLinkReview_requires_produce_wp_and_person_wp_item()
    {
        var application = WorkPermitEnabled("Mary");
        Assert.True(CaseWorkPermitLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile!.RequirePersonWorkPermitItem = false;
        Assert.False(CaseWorkPermitLocationCopy.ShowInLinkReview(application));

        // RequireWorkPermitLocationWhenProducingWorkPermit stays true while ProduceWorkPermit is on.
        application.ApplicationProfile.RequirePersonWorkPermitItem = true;
        application.ApplicationProfile.RequireWorkPermitLocation = false;
        Assert.True(CaseWorkPermitLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile.ProduceWorkPermit = false;
        Assert.False(CaseWorkPermitLocationCopy.ShowInLinkReview(application));
    }

    [Fact]
    public void BorderZone_AcceptOnJoin_null_object_space_returns_false()
    {
        var application = BorderZoneEnabled("Mary");
        Assert.False(CaseBorderZoneLocationCopy.AcceptOnJoin(
            null!,
            application,
            new Person { ID = Guid.NewGuid() }));
    }

    [Fact]
    public void BorderZone_TryOverwriteFromCase_requires_ids_or_object_space()
    {
        Assert.False(CaseBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            out var error));
        Assert.Equal("Missing border zone link.", error);

        Assert.False(CaseBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            out error));
        Assert.Equal("Missing border zone link.", error);

        Assert.False(CaseBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            out error));
        Assert.Equal("Missing border zone link.", error);
    }

    [Fact]
    public void BorderZone_ShowInLinkReview_requires_produce_border_zone()
    {
        var application = BorderZoneEnabled("Mary");
        Assert.True(CaseBorderZoneLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile!.ProduceBorderZone = false;
        Assert.False(CaseBorderZoneLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile.ProduceBorderZone = true;
        application.ApplicationProfile.RequirePersonBorderZoneItem = false;
        Assert.False(CaseBorderZoneLocationCopy.ShowInLinkReview(application));
    }

    [Fact]
    public void BorderZone_EnsureForPeopleWithEmptyLocations_null_object_space_does_not_throw()
    {
        CaseBorderZoneLocationCopy.EnsureForPeopleWithEmptyLocations(null!, BorderZoneEnabled("Mary"));
        CaseWorkPermitLocationCopy.EnsureForPeopleWithEmptyLocations(null!, WorkPermitEnabled("Mary"));
    }

    private static ApplicationProfileInstance WorkPermitEnabled(string location) =>
        new()
        {
            MovementPermitLocation = location,
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = true,
                RequirePersonWorkPermitItem = true,
                RequireWorkPermitLocation = true,
            },
        };

    private static ApplicationProfileInstance BorderZoneEnabled(string location) =>
        new()
        {
            BorderZoneLocation = location,
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = true,
            },
        };
}
