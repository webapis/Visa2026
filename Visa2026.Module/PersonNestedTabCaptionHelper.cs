using Visa2026.Module.Localization;

namespace Visa2026.Module;

/// <summary>
/// Runtime base captions for Person typed detail nested collection tabs (before count suffix).
/// </summary>
public static class PersonNestedTabCaptionHelper
{
    public static string? TryGetBaseCaption(string layoutTabId) =>
        layoutTabId == PersonNestedCollectionLayout.CvAndPersonalFilesTab
            ? VisaUiMessages.Get(PersonNestedCollectionLayout.CvAndPersonalFilesTabCaptionKey)
            : null;
}