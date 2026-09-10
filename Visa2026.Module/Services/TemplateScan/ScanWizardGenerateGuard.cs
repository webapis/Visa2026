#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Continue / Regenerate must not run while Remap unmarked (or Analyze) is busy,
/// and must not start from Upload / Done / Generating.
/// </summary>
public static class ScanWizardGenerateGuard
{
    public static bool AllowStart(bool busy, bool onFieldReview, bool onClarification, bool onPreview) =>
        !busy && (onFieldReview || onClarification || onPreview);
}