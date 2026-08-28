using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class BorderZoneSelectionHelperTests
{
    [Fact]
    public void ResolveForIssuedVisa_PrefersInvitationThenInstanceThenYok()
    {
        var invitation = new Invitation { BorderZoneLocation = "Ahal, Mary" };
        var instance = new ApplicationProfileInstance { BorderZoneLocation = "Balkan" };

        Assert.Equal("Ahal, Mary", BorderZoneSelectionHelper.ResolveForIssuedVisa(invitation, instance));
        Assert.Equal("Balkan", BorderZoneSelectionHelper.ResolveForIssuedVisa(new Invitation(), instance));
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, BorderZoneSelectionHelper.ResolveForIssuedVisa(null, null));
    }

    [Fact]
    public void ToggleLabel_AddsAndRemovesZones()
    {
        var stored = CommaSeparatedSelectionHelper.ToggleLabel(null, "Ahal", true);
        Assert.Equal("Ahal", stored);
        stored = CommaSeparatedSelectionHelper.ToggleLabel(stored, "Mary", true);
        Assert.Contains("Mary", stored);
        stored = CommaSeparatedSelectionHelper.ToggleLabel(stored, "Ahal", false);
        Assert.Equal("Mary", stored);
        stored = CommaSeparatedSelectionHelper.ToggleLabel(stored, "Mary", false);
        Assert.Equal(CommaSeparatedSelectionHelper.NoneValue, stored);
    }

    [Fact]
    public void ApplyDefaultIfEmpty_InstanceSetsYok()
    {
        var instance = new ApplicationProfileInstance { BorderZoneLocation = null };
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(instance);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, instance.BorderZoneLocation);

        instance.BorderZoneLocation = "Ahal";
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(instance);
        Assert.Equal("Ahal", instance.BorderZoneLocation);
    }

    [Fact]
    public void CoalesceToNone_EmptyIsYok()
    {
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, BorderZoneSelectionHelper.CoalesceToNone(null));
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, BorderZoneSelectionHelper.CoalesceToNone("  "));
        Assert.Equal("Ahal", BorderZoneSelectionHelper.CoalesceToNone("Ahal"));
    }
}