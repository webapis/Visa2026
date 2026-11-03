using System;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Regression coverage for via-ministry list progress tail expansion
/// (awaiting CO vs number received pinned on the right).
/// </summary>
public sealed class ApplicationWorkspaceListProgressViaMinistryTailTests
{
    [Fact]
    public void ExpandViaMinistryTail_empty_or_null_is_empty()
    {
        Assert.Empty(ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(null!));
        Assert.Empty(ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(
            Array.Empty<ApplicationWorkspaceListProgressSteps.Step>()));
    }

    [Fact]
    public void ExpandViaMinistryTail_leaves_non_migration_steps_alone()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("leg-1", "Ministry", "current", "", "2 Aug", ""),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        Assert.Equal(2, expanded.Count);
        Assert.Equal("office", expanded[0].Key);
        Assert.Equal("leg-1", expanded[1].Key);
    }

    [Fact]
    public void ExpandViaMinistryTail_pending_migration_keeps_both_columns_pending()
    {
        var letter = Guid.NewGuid();
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "pending",
                "",
                "",
                "",
                letter,
                true,
                false),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        Assert.Equal(
            ["office", ApplicationWorkspaceListProgressSteps.MigrationAwaitingStepKey, ApplicationWorkspaceListProgressSteps.MigrationReceivedStepKey],
            expanded.Select(step => step.Key).ToArray());
        Assert.Equal("pending", expanded[1].State);
        Assert.Equal(string.Empty, expanded[1].OutcomeKind);
        Assert.Equal(string.Empty, expanded[1].Date);
        // Pending is not received yet, so a ministry letter stays on awaiting.
        Assert.True(expanded[1].HasDocuments);
        Assert.Equal(letter, expanded[1].DocumentProgressId);
        Assert.Equal("pending", expanded[2].State);
        Assert.Equal(string.Empty, expanded[2].OutcomeKind);
        Assert.False(expanded[2].HasDocuments);
        Assert.False(expanded[2].AsNumberReceived);
    }

    [Fact]
    public void ExpandViaMinistryTail_received_moves_letter_and_status_to_received_column()
    {
        var letter = Guid.NewGuid();
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "done",
                "issued",
                "21 Aug",
                "AS-4411",
                letter,
                true,
                true),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        Assert.Equal(ApplicationWorkspaceListProgressSteps.MigrationAwaitingStepKey, expanded[1].Key);
        Assert.Equal("done", expanded[1].State);
        Assert.Equal("ok", expanded[1].OutcomeKind);
        Assert.Equal("21 Aug", expanded[1].Date);
        Assert.False(expanded[1].HasDocuments);
        Assert.Equal(Guid.Empty, expanded[1].DocumentProgressId);
        Assert.False(expanded[1].AsNumberReceived);

        Assert.Equal(ApplicationWorkspaceListProgressSteps.MigrationReceivedStepKey, expanded[2].Key);
        Assert.Equal("done", expanded[2].State);
        Assert.Equal("issued", expanded[2].OutcomeKind);
        Assert.Equal("21 Aug", expanded[2].Date);
        Assert.Equal("AS-4411", expanded[2].StatusLabel);
        Assert.True(expanded[2].HasDocuments);
        Assert.Equal(letter, expanded[2].DocumentProgressId);
        Assert.True(expanded[2].AsNumberReceived);
    }

    [Fact]
    public void ExpandViaMinistryTail_strips_awaiting_status_label_from_received_column()
    {
        var awaiting = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.AwaitingAsCoNumber");
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "done",
                "",
                "4 Aug",
                awaiting,
                Guid.Empty,
                false,
                true),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        Assert.Equal(string.Empty, expanded[1].StatusLabel);
        Assert.True(expanded[1].AsNumberReceived);
    }

    [Fact]
    public void ExpandViaMinistryTail_rejected_without_number_keeps_letter_on_awaiting()
    {
        var letter = Guid.NewGuid();
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "current",
                "rejected",
                "9 Aug",
                "Rejected",
                letter,
                true,
                false),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        // Rejection without AsNumberReceived is not "received" — letter stays on awaiting.
        Assert.Equal("done", expanded[0].State);
        Assert.Equal("ok", expanded[0].OutcomeKind);
        Assert.True(expanded[0].HasDocuments);
        Assert.Equal(letter, expanded[0].DocumentProgressId);
        Assert.Equal("current", expanded[1].State);
        Assert.Equal("rejected", expanded[1].OutcomeKind);
        Assert.Equal("9 Aug", expanded[1].Date);
        // Status text only copies when AsNumberReceived/done — rejection alone leaves it blank.
        Assert.Equal(string.Empty, expanded[1].StatusLabel);
        Assert.False(expanded[1].HasDocuments);
        Assert.False(expanded[1].AsNumberReceived);
    }

    [Fact]
    public void ExpandViaMinistryTail_current_awaiting_holds_letter_until_number_arrives()
    {
        var letter = Guid.NewGuid();
        var awaiting = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.AwaitingAsCoNumber");
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "current",
                "",
                "4 Aug",
                awaiting,
                letter,
                true,
                false),
        };

        var expanded = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

        Assert.Equal("current", expanded[0].State);
        Assert.Equal("ok", expanded[0].OutcomeKind);
        Assert.True(expanded[0].HasDocuments);
        Assert.Equal(letter, expanded[0].DocumentProgressId);
        Assert.Equal("pending", expanded[1].State);
        Assert.Equal(string.Empty, expanded[1].StatusLabel);
        Assert.False(expanded[1].HasDocuments);
        Assert.False(expanded[1].AsNumberReceived);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("office", false)]
    [InlineData("migration", true)]
    [InlineData("migration-submitted", true)]
    [InlineData("migration-awaiting", true)]
    [InlineData("migration-received", true)]
    [InlineData("MIGRATION-AWAITING", true)]
    public void IsViaMinistryTailStep_recognizes_pinned_tail_keys(string? key, bool expected) =>
        Assert.Equal(expected, ApplicationWorkspaceListProgressSteps.IsViaMinistryTailStep(key));
}
