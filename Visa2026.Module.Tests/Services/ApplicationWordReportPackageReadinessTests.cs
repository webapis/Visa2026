using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWordReportPackageReadinessTests
{
    [Fact]
    public void Compute_Empty_ReturnsZeros()
    {
        var summary = ApplicationWordReportPackageReadinessSummary.Compute([]);
        Assert.Equal(0, summary.ReadyCount);
        Assert.Equal(0, summary.WarningCount);
        Assert.False(summary.HasWarnings);
    }

    [Fact]
    public void Compute_CountsReadyAndWarning()
    {
        var entries = new[]
        {
            Entry("a", ApplicationWordReportPackageReadinessLevel.Ready),
            Entry("b", ApplicationWordReportPackageReadinessLevel.Warning),
            Entry("c", ApplicationWordReportPackageReadinessLevel.Ready)
        };

        var summary = ApplicationWordReportPackageReadinessSummary.Compute(entries);

        Assert.Equal(2, summary.ReadyCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.True(summary.HasWarnings);
    }

    [Fact]
    public void Compute_SelectedKeys_FiltersEntries()
    {
        var entries = new[]
        {
            Entry("a", ApplicationWordReportPackageReadinessLevel.Ready),
            Entry("b", ApplicationWordReportPackageReadinessLevel.Warning),
            Entry("c", ApplicationWordReportPackageReadinessLevel.Warning)
        };

        var summary = ApplicationWordReportPackageReadinessSummary.Compute(
            entries,
            selectedEntryKeys: new HashSet<string>(StringComparer.Ordinal) { "a", "c" });

        Assert.Equal(1, summary.ReadyCount);
        Assert.Equal(1, summary.WarningCount);
    }

    [Fact]
    public void ApplyDryRunHints_AlreadyWarning_KeepsMessage()
    {
        var hints = new[]
        {
            new ApplicationWordReportPackageReadinessHint { MessageKey = "gap" }
        };

        var (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.ApplyDryRunHints(
            ApplicationWordReportPackageReadinessLevel.Warning,
            "existing",
            hints);

        Assert.Equal(ApplicationWordReportPackageReadinessLevel.Warning, level);
        Assert.Equal("existing", messageKey);
    }

    [Fact]
    public void ApplyDryRunHints_ReadyWithGaps_PromotesToWarning()
    {
        var hints = new[]
        {
            new ApplicationWordReportPackageReadinessHint { MessageKey = "gap" }
        };

        var (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.ApplyDryRunHints(
            ApplicationWordReportPackageReadinessLevel.Ready,
            null,
            hints);

        Assert.Equal(ApplicationWordReportPackageReadinessLevel.Warning, level);
        Assert.Equal("ApplicationReportPackage.Readiness.DataGaps", messageKey);
    }

    [Fact]
    public void ApplyDryRunHints_ReadyWithoutGaps_StaysReady()
    {
        var (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.ApplyDryRunHints(
            ApplicationWordReportPackageReadinessLevel.Ready,
            null,
            Array.Empty<ApplicationWordReportPackageReadinessHint>());

        Assert.Equal(ApplicationWordReportPackageReadinessLevel.Ready, level);
        Assert.Null(messageKey);
    }

    private static ApplicationWordReportPackageCatalogEntry Entry(
        string key,
        ApplicationWordReportPackageReadinessLevel readiness) =>
        new()
        {
            EntryKey = key,
            DisplayName = key,
            Readiness = readiness
        };
}
