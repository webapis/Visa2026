using Visa2026.Module.Localization;

namespace Visa2026.Module;

/// <summary>
/// Localized captions for parent BO <c>Documents</c> collections (layout id <c>Documents</c> / <c>Documents_Group</c>).
/// Property names stay <c>Documents</c>; only officer-facing captions change.
/// </summary>
public static class DocumentCollectionTabCaptionHelper
{
    public const string DocumentsLayoutId = "Documents";
    public const string DocumentsGroupLayoutId = "Documents_Group";

    private static readonly Dictionary<string, string> CaptionKeysByDetailViewId =
        new(StringComparer.Ordinal)
        {
            [PersonDetailViewIds.Employee] = "Person.Tab.CvAndPersonalFiles",
            [PersonDetailViewIds.Default] = "Person.Tab.CvAndPersonalFiles",
            ["Passport_DetailView"] = "Passport.Tab.DocumentCopies",
            ["Education_DetailView"] = "Education.Tab.DocumentCopies",
            ["Visa_DetailView"] = "Visa.Tab.DocumentCopies",
            ["WorkPermit_DetailView"] = "WorkPermit.Tab.DocumentCopies",
            ["Invitation_DetailView"] = "Invitation.Tab.DocumentCopies",
            ["Rejection_DetailView"] = "Rejection.Tab.DocumentCopies",
            ["BorderZone_DetailView"] = "BorderZone.Tab.DocumentCopies",
            ["MedicalRecord_DetailView"] = "MedicalRecord.Tab.DocumentCopies",
            ["Lodging_DetailView"] = "Lodging.Tab.DocumentCopies",
            ["ProjectContract_DetailView"] = "ProjectContract.Tab.DocumentCopies",
            ["AddressOfResidence_DetailView"] = "AddressOfResidence.Tab.DocumentCopies",
        };

    public static bool IsDocumentsLayoutId(string layoutId) =>
        layoutId == DocumentsLayoutId || layoutId == DocumentsGroupLayoutId;

    public static string? TryGetCaptionKey(string detailViewId) =>
        CaptionKeysByDetailViewId.TryGetValue(detailViewId, out var key) ? key : null;

    public static string? TryGetBaseCaption(string detailViewId, string layoutId) =>
        IsDocumentsLayoutId(layoutId) && TryGetCaptionKey(detailViewId) is { } key
            ? VisaUiMessages.Get(key)
            : null;

    public static string? TryGetBaseCaptionForDetailView(string detailViewId) =>
        TryGetCaptionKey(detailViewId) is { } key ? VisaUiMessages.Get(key) : null;
}