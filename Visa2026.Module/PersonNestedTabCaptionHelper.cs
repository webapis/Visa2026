using Visa2026.Module.Localization;

namespace Visa2026.Module;

/// <summary>
/// Runtime base captions for Person typed detail nested collection tabs (before count suffix).
/// Delegates Documents captions to <see cref="DocumentCollectionTabCaptionHelper"/>.
/// </summary>
public static class PersonNestedTabCaptionHelper
{
    private static readonly Dictionary<string, string> TabKeysByLayoutId =
        new(StringComparer.Ordinal)
        {
            ["Educations"] = "Person.Tab.Educations",
            ["Passports"] = "Person.Tab.Passports",
            ["PositionHistory"] = "Person.Tab.PositionHistory",
            ["MedicalRecords"] = "Person.Tab.MedicalRecords",
            ["AddressesOfResidence"] = "Person.Tab.AddressesOfResidence",
            ["FamilyRelationDocuments"] = "Person.Tab.FamilyRelationDocuments",
            ["TravelHistories"] = "Person.Tab.TravelHistories",
            ["WorkDuties"] = "Person.Tab.WorkDuties",
            ["Salaries"] = "Person.Tab.Salaries",
            ["IncompleteData"] = "Person.Tab.IncompleteData",
            ["ApplicationProfileInstances"] = "Person.Tab.ApplicationsLinked",
            ["WorkPermitItems"] = "Person.Tab.WorkPermitsIssued",
            ["InvitationItems"] = "Person.Tab.InvitationsIssued",
            ["RejectionItems"] = "Person.Tab.RejectionsIssued",
            ["FamilyMembers"] = "Person.Tab.FamilyMembersLinked",
        };

    public static string? TryGetBaseCaption(string detailViewId, string layoutTabId)
    {
        if (IsPersonDetailView(detailViewId)
            && TabKeysByLayoutId.TryGetValue(layoutTabId, out var key))
        {
            return VisaUiMessages.Get(key);
        }

        return DocumentCollectionTabCaptionHelper.TryGetBaseCaption(detailViewId, layoutTabId);
    }

    private static bool IsPersonDetailView(string detailViewId) =>
        detailViewId == PersonDetailViewIds.Default
        || detailViewId == PersonDetailViewIds.Employee
        || detailViewId == PersonDetailViewIds.FamilyMember
        || detailViewId == PersonDetailViewIds.TemporaryVisitor;
}