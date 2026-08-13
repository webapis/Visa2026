using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Dry-run readiness hints must fail closed when ObjectSpace/application/template are missing
/// so Resminamalar catalog never throws on incomplete dialog state.
/// </summary>
public sealed class ApplicationWordReportPackageDryRunEvaluatorTests
{
    [Fact]
    public void CollectUserTemplateHints_NullArguments_ReturnsEmpty()
    {
        var application = new Application();
        var template = new UserReportTemplate();

        Assert.Empty(ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            objectSpace: null!,
            application,
            template));

        Assert.Empty(ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            objectSpace: null!,
            application: null!,
            template));

        Assert.Empty(ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            objectSpace: null!,
            application,
            template: null!));
    }

    [Fact]
    public void CollectUserTemplateHints_NullObjectSpace_IgnoresSelectedItems()
    {
        // Guard returns before selectedItems is used — callers must not rely on hints without OS.
        var items = new List<ApplicationItem> { new() };

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            objectSpace: null!,
            application: new Application(),
            template: new UserReportTemplate(),
            selectedItems: items);

        Assert.Empty(hints);
    }
}
