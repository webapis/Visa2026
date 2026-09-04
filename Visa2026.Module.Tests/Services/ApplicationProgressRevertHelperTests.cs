using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProgressRevertHelperTests
{
    [Fact]
    public void RowsToDelete_EmptyHistory_IsEmpty()
    {
        Assert.Empty(ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(null, null));
        Assert.Empty(ApplicationProfileInstanceProgressRevertHelper.RowsToDelete([], ApplicationWorkspaceProgressTimeline.OfficeKey));
    }

    [Fact]
    public void RowsToDelete_LastStep_RemovesOnlyLatest()
    {
        var history = ThreeStepHistory();
        var deleted = ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(history, stepKey: null);

        Assert.Single(deleted);
        Assert.Equal(ApplicationProfileInstanceProgressStateCodes.ProcessStarted, deleted[0].State.Code);
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void RowsToDelete_Office_RemovesAll()
    {
        var history = ThreeStepHistory();
        var deleted = ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(
            history,
            ApplicationWorkspaceProgressTimeline.OfficeKey);

        Assert.Equal(3, deleted.Count);
    }

    [Fact]
    public void RowsToDelete_FirstLeg_KeepsThatSlotAndRemovesLater()
    {
        var history = ThreeStepHistory();
        var deleted = ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(history, "leg-1");

        Assert.Equal(2, deleted.Count);
        Assert.DoesNotContain(deleted, row => row.State.Code == ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1));
        Assert.Contains(deleted, row => row.State.Code == ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2));
        Assert.Contains(deleted, row => row.State.Code == ApplicationProfileInstanceProgressStateCodes.ProcessStarted);
    }

    [Fact]
    public void SlotKeyFor_MapsMinistryAndMigration()
    {
        Assert.Equal("leg-2", ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(Row(2, ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2))));
        Assert.Equal(
            ApplicationWorkspaceProgressTimeline.MigrationKey,
            ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(Row(3, ApplicationProfileInstanceProgressStateCodes.ProcessIssued)));
        Assert.Equal(
            ApplicationWorkspaceProgressTimeline.OfficeKey,
            ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(Row(1, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared)));
    }

    private static List<ApplicationProfileInstanceProgress> ThreeStepHistory() =>
        [
            Row(1, ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1)),
            Row(2, ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2)),
            Row(3, ApplicationProfileInstanceProgressStateCodes.ProcessStarted),
        ];

    private static ApplicationProfileInstanceProgress Row(int order, string stateCode) =>
        new()
        {
            ID = Guid.NewGuid(),
            Order = order,
            Date = DateTime.Today,
            State = new ApplicationState { Code = stateCode },
        };
}