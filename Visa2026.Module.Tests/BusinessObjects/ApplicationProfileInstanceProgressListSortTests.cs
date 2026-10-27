using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressListSortTests
{
    [Fact]
    public void Compose_GroupsIssuedBeforeInProcess_ByAppearancePriority()
    {
        var issued = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued);
        var started = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted);

        Assert.True(issued < started);
    }

    [Fact]
    public void Compose_OrdersWorkflowStagesWithinSameColorFamily()
    {
        var office = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared);
        var submitted = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration);
        var started = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted);

        Assert.True(office < submitted);
        Assert.True(submitted < started);
    }

    [Fact]
    public void Compose_SlaWarningSortsAfterHealthyInProcess()
    {
        var started = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted);
        var warning = ApplicationProfileInstanceProgressListSort.Compose(
            ApplicationProfileInstanceProgressSlaCodes.Warning,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted);

        Assert.True(started < warning);
        Assert.True(BoStateAppearanceColors.TryGet(ApplicationProfileInstanceProgressSlaCodes.Warning, out var appearance));
        Assert.Equal(320, appearance.DisplayPriority);
    }
}