using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation preview banner: every person is a card; chips only when lives differ;
/// Choose narrows to one person when the open mark names them.
/// </summary>
public sealed class InvitationPreviewStateSelectionTests
{
    [Fact]
    public void UseChips_false_for_one_person_or_matching_lives()
    {
        var one = new[]
        {
            new InvitationPreviewStates.PersonState(Guid.NewGuid(), "Ali", InvitationLaterLife.InForce),
        };
        var same = new[]
        {
            new InvitationPreviewStates.PersonState(Guid.NewGuid(), "Ali", InvitationLaterLife.Used),
            new InvitationPreviewStates.PersonState(Guid.NewGuid(), "Berdi", InvitationLaterLife.Used),
        };

        Assert.False(InvitationPreviewStates.UseChips(one));
        Assert.False(InvitationPreviewStates.UseChips(same));
        Assert.False(InvitationPreviewStates.UseChips(Array.Empty<InvitationPreviewStates.PersonState>()));
    }

    [Fact]
    public void UseChips_true_when_people_have_different_later_lives()
    {
        var people = new[]
        {
            new InvitationPreviewStates.PersonState(Guid.NewGuid(), "Weijun Liu", InvitationLaterLife.Changed),
            new InvitationPreviewStates.PersonState(Guid.NewGuid(), "Zihie Liu", InvitationLaterLife.InForce),
        };

        Assert.True(InvitationPreviewStates.UseChips(people));
    }

    [Fact]
    public void Choose_returns_all_when_person_missing_or_empty()
    {
        var weijun = Guid.NewGuid();
        var zihie = Guid.NewGuid();
        var people = new[]
        {
            new InvitationPreviewStates.PersonState(weijun, "Weijun Liu", InvitationLaterLife.Changed),
            new InvitationPreviewStates.PersonState(zihie, "Zihie Liu", InvitationLaterLife.InForce),
        };

        Assert.Equal(people, InvitationPreviewStates.Choose(people, null));
        Assert.Equal(people, InvitationPreviewStates.Choose(people, Guid.Empty));
        Assert.Equal(people, InvitationPreviewStates.Choose(people, Guid.NewGuid()));
        Assert.Empty(InvitationPreviewStates.Choose(Array.Empty<InvitationPreviewStates.PersonState>(), weijun));
    }

    [Fact]
    public void Choose_narrows_to_the_named_person()
    {
        var weijun = Guid.NewGuid();
        var zihie = Guid.NewGuid();
        var people = new[]
        {
            new InvitationPreviewStates.PersonState(weijun, "Weijun Liu", InvitationLaterLife.Changed),
            new InvitationPreviewStates.PersonState(zihie, "Zihie Liu", InvitationLaterLife.InForce),
        };

        var chosen = InvitationPreviewStates.Choose(people, zihie);

        var only = Assert.Single(chosen);
        Assert.Equal(zihie, only.PersonId);
        Assert.Equal(InvitationLaterLife.InForce, only.Life);
    }
}
