using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceCompletionPercentageTests
{
    private static ApplicationWorkspaceListProgressSteps.Step Step(string key, string state) =>
        new(key, key, state, string.Empty, string.Empty, string.Empty);

    private static ApplicationWorkspaceIssuedResultListCoverage.Chip Chip(
        string key,
        int coverage,
        int expected,
        bool optional = false) =>
        new(key, key, coverage, expected, optional);

    private static ApplicationWorkspaceListProgressSteps.Step[] AllDoneSteps() =>
    [
        Step("office", "done"),
        Step("leg-1", "done"),
        Step("migration-submitted", "done"),
        Step("migration", "done"),
    ];

    [Fact]
    public void Phase1_partial_steps_no_as_stays_within_40()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration-submitted", "current"),
            Step("migration", "pending"),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(steps, null, asGatePassed: false);

        Assert.Equal(24, percent); // round(3/5 * 40)
    }

    [Fact]
    public void Phase1_at_migration_awaiting_as_not_full_40()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration-submitted", "done"),
            Step("migration", "current"),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(steps, null, asGatePassed: false);

        Assert.Equal(32, percent); // round(4/5 * 40)
        Assert.True(percent < ApplicationProfileInstanceCompletionPercentage.Phase1Weight);
    }

    [Fact]
    public void Co_number_received_scores_above_awaiting_as()
    {
        var awaiting = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration-submitted", "done"),
            Step("migration", "current"),
        };
        var received = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration-submitted", "done"),
            new ApplicationWorkspaceListProgressSteps.Step(
                "migration",
                "migration",
                "current",
                "current",
                string.Empty,
                "CO0236909",
                default,
                false,
                true),
        };
        var chips = new[]
        {
            Chip("invitation", 0, 2),
            Chip("issuedVisa", 0, 2),
        };

        var waitingPercent = ApplicationProfileInstanceCompletionPercentage.Calculate(awaiting, chips, asGatePassed: false);
        var receivedPercent = ApplicationProfileInstanceCompletionPercentage.Calculate(received, chips, asGatePassed: true);

        Assert.Equal(32, waitingPercent);
        Assert.Equal(40, receivedPercent);
        Assert.True(receivedPercent > waitingPercent);
    }

    [Fact]
    public void Direct_hand_in_with_no_result_is_100_when_both_steps_are_done()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step("migration-submitted", "done"),
        };

        Assert.Equal(100, ApplicationProfileInstanceCompletionPercentage.Calculate(steps, null, asGatePassed: false));
        Assert.Equal(
            100,
            ApplicationProfileInstanceCompletionPercentage.Calculate(
                steps,
                new[] { Chip("issuedVisa", 0, 0) },
                asGatePassed: true));
    }

    [Fact]
    public void Direct_hand_in_only_office_done_is_half()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step("migration-submitted", "current"),
        };

        Assert.Equal(50, ApplicationProfileInstanceCompletionPercentage.Calculate(steps, null, asGatePassed: false));
    }

    [Fact]
    public void Direct_with_co_step_still_uses_process_then_result()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step("migration-submitted", "done"),
            new ApplicationWorkspaceListProgressSteps.Step(
                "migration",
                "migration",
                "done",
                "issued",
                string.Empty,
                "CO1",
                default,
                false,
                true),
        };
        var chips = new[]
        {
            Chip("invitation", 1, 1),
            Chip("issuedVisa", 0, 1),
        };

        Assert.Equal(70, ApplicationProfileInstanceCompletionPercentage.Calculate(steps, chips, asGatePassed: true));
    }

    [Fact]
    public void After_gate_all_steps_done_zero_docs_is_40()
    {
        var chips = new[]
        {
            Chip("invitation", 0, 3),
            Chip("issuedVisa", 0, 3),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(
            AllDoneSteps(),
            chips,
            asGatePassed: true);

        Assert.Equal(40, percent);
    }

    [Fact]
    public void After_gate_all_steps_done_half_coverage_is_70()
    {
        var chips = new[]
        {
            Chip("invitation", 3, 3),
            Chip("issuedVisa", 0, 3),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(AllDoneSteps(), chips, asGatePassed: true);

        Assert.Equal(70, percent); // 40 + round(3/6 * 60)
    }

    [Fact]
    public void After_gate_all_steps_done_full_coverage_is_100()
    {
        var chips = new[]
        {
            Chip("invitation", 3, 3),
            Chip("workPermit", 2, 2),
            Chip("issuedVisa", 3, 3),
        };

        Assert.Equal(100, ApplicationProfileInstanceCompletionPercentage.Calculate(AllDoneSteps(), chips, asGatePassed: true));
    }

    [Fact]
    public void After_gate_incomplete_timeline_with_full_visa_is_not_100()
    {
        // Like 5/-889: process number present, a current step remains, visa chips full.
        var steps = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration-submitted", "current"),
            Step("migration", "pending"),
        };
        var chips = new[]
        {
            Chip("rejection", 0, 0, optional: true),
            Chip("issuedVisa", 2, 2),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(steps, chips, asGatePassed: true);

        Assert.Equal(84, percent); // round(3/5 * 40) + 60
        Assert.True(percent < 100);
    }

    [Fact]
    public void After_gate_zero_expected_chips_adds_no_phase2()
    {
        var chips = new[]
        {
            Chip("invitation", 0, 0),
            Chip("issuedVisa", 0, 0),
            Chip("workPermit", 0, 0),
        };

        // Timeline incomplete + 0/0 chips (like 9/-1659) → phase1 only, no free 60.
        var steps = new[]
        {
            Step("office", "done"),
            Step("leg-1", "done"),
            Step("leg-2", "done"),
            Step("migration", "pending"),
        };

        var percent = ApplicationProfileInstanceCompletionPercentage.Calculate(steps, chips, asGatePassed: true);

        Assert.Equal(30, percent); // round(3/4 * 40) + 0
        Assert.Equal(40, ApplicationProfileInstanceCompletionPercentage.Calculate(AllDoneSteps(), chips, asGatePassed: true));
        Assert.Equal(40, ApplicationProfileInstanceCompletionPercentage.Calculate(AllDoneSteps(), null, asGatePassed: true));
    }

    [Fact]
    public void HasPassedAsGate_process_number_without_process_started_history()
    {
        var application = new ApplicationProfileInstance
        {
            ProcessNumber = "CO0239199",
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        Assert.True(ApplicationProfileInstanceCompletionPercentage.HasPassedAsGate(application));
        Assert.Equal(
            0,
            ApplicationProfileInstanceCompletionPercentage.Calculate(
                application.ListViewProgressSteps,
                application.ListViewResultCoverageChips,
                asGatePassed: true));
    }

    [Fact]
    public void ApplyTo_sets_cached_completion_percentage()
    {
        var application = new ApplicationProfileInstance { ProcessNumber = null };
        application.SetListViewProgressSteps(new[]
        {
            Step("office", "done"),
            Step("migration-submitted", "done"),
            Step("migration", "current"),
        });
        application.SetListViewResultCoverage(new[]
        {
            Chip("invitation", 1, 2),
        });

        ApplicationProfileInstanceCompletionPercentage.ApplyTo(application);

        Assert.Equal(27, application.CompletionPercentage); // round(2/3 * 40), gate closed
    }
}