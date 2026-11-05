using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseVisaBorderZoneLocationCopyJoinAndOverwriteGateTests
{
    [Fact]
    public void AcceptOnJoin_null_object_space_or_empty_person_returns_false()
    {
        var application = EnabledApplication("Mary");

        // Null OS short-circuits before GetObjectsQuery (same pattern as Detach gates).
        Assert.False(CaseVisaBorderZoneLocationCopy.AcceptOnJoin(
            null!,
            application,
            new Person { ID = Guid.NewGuid() }));
        Assert.False(CaseVisaBorderZoneLocationCopy.AcceptOnJoin(
            null!,
            application,
            new Person { ID = Guid.Empty }));
        Assert.False(CaseVisaBorderZoneLocationCopy.AcceptOnJoin(
            null!,
            application,
            null!));
    }

    [Fact]
    public void TryOverwriteFromCase_requires_ids_before_object_space_lookup()
    {
        Assert.False(CaseVisaBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            out var error));
        Assert.Equal("Missing visa link.", error);

        Assert.False(CaseVisaBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            out error));
        Assert.Equal("Missing visa link.", error);

        Assert.False(CaseVisaBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            out error));
        Assert.Equal("Missing visa link.", error);

        Assert.False(CaseVisaBorderZoneLocationCopy.TryOverwriteFromCase(
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            out error));
        Assert.Equal("Missing visa link.", error);
    }

    [Fact]
    public void TryApplyEditedLocations_null_object_space_returns_false()
    {
        var application = EnabledApplication("Mary");
        var person = new Person { ID = Guid.NewGuid() };

        Assert.False(CaseVisaBorderZoneLocationCopy.TryApplyEditedLocations(
            null!,
            application,
            person,
            Guid.NewGuid(),
            "Mary"));
        Assert.False(CaseVisaBorderZoneLocationCopy.TryApplyEditedLocations(
            null!,
            application,
            null!,
            Guid.NewGuid(),
            "Mary"));
    }

    [Fact]
    public void EnsureForPeopleWithEmptyLocations_null_object_space_does_not_throw()
    {
        CaseVisaBorderZoneLocationCopy.EnsureForPeopleWithEmptyLocations(null!, EnabledApplication("Mary"));
        CaseVisaBorderZoneLocationCopy.EnsureForPeopleWithEmptyLocations(null!, null!);
    }

    [Fact]
    public void OfferTick_on_application_respects_IsEnabled()
    {
        var visa = new Visa { BorderZoneLocation = "Ahal" };
        var application = EnabledApplication("Mary");
        Assert.True(CaseVisaBorderZoneLocationCopy.OfferTick(application, visa));

        // Producing a BorderZone permit disables visa-location defaulting.
        application.ApplicationProfile!.ProduceBorderZone = true;
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick(application, visa));

        // ProduceVisa=false disables even when RequireBorderZone is still true
        // (EffectiveRequireBorderZone stays on while ProduceVisa is true).
        application.ApplicationProfile.ProduceBorderZone = false;
        application.ApplicationProfile.ProduceVisa = false;
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick(application, visa));
    }

    [Fact]
    public void ShowInLinkReview_matches_IsEnabled()
    {
        var application = EnabledApplication("Mary");
        Assert.True(CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application));
        Assert.True(CaseVisaBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile!.ProduceVisa = false;
        Assert.False(CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application));
        Assert.False(CaseVisaBorderZoneLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void ResolveDisplaySummary_falls_back_to_em_dash()
    {
        Assert.Equal("—", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary(null, null));
        Assert.Equal("—", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary("  ", " "));
        Assert.Equal("Mary", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary(null, " Mary "));
        Assert.Equal("Ahal", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary(" Ahal ", "Mary"));
    }

    private static ApplicationProfileInstance EnabledApplication(string? caseLocation) =>
        new()
        {
            BorderZoneLocation = caseLocation,
            ApplicationProfile = new ApplicationProfile
            {
                ProduceVisa = true,
                ProduceBorderZone = false,
                RequireBorderZone = true,
            },
        };
}
