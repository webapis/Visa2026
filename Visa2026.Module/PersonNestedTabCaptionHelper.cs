using Visa2026.Module.Localization;

namespace Visa2026.Module;

/// <summary>
/// Runtime base captions for Person typed detail nested collection tabs (before count suffix).
/// Delegates Documents captions to <see cref="DocumentCollectionTabCaptionHelper"/>.
/// </summary>
public static class PersonNestedTabCaptionHelper
{
    public static string? TryGetBaseCaption(string detailViewId, string layoutTabId) =>
        DocumentCollectionTabCaptionHelper.TryGetBaseCaption(detailViewId, layoutTabId);
}