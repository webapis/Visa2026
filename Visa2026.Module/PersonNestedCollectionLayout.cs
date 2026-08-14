namespace Visa2026.Module;

/// <summary>
/// Layout and nested ListView identifiers for <see cref="BusinessObjects.Person"/> detail collection tabs.
/// </summary>
public static class PersonNestedCollectionLayout
{
    public const string PersonCollectionSections = "PersonCollectionSections";
    public const string PersonRecordTabs = "PersonRecordTabs";
    public const string IssuedDocumentsTabs = "IssuedDocumentsTabs";
    public const string PersonNewRecordIssuedHint = "PersonNewRecordIssuedHint";
    public const string CvAndPersonalFilesTab = "Documents";
    public const string CvAndPersonalFilesTabCaptionKey = "Person.Tab.CvAndPersonalFiles";

    public const string ApplicationProfileInstancesListView = "Person_ApplicationProfileInstances_ListView";

    /// <summary>Legacy nested list id; replaced by <see cref="ApplicationProfileInstancesListView"/>.</summary>
    public const string ApplicationProfileInstancePeopleListView = ApplicationProfileInstancesListView;

    /// <summary>Legacy; replaced by <see cref="ApplicationProfileInstancePeopleListView"/> (slice 10 close-out).</summary>
    public const string ApplicationItemsListView = "Person_ApplicationItems_ListView";
    public const string WorkPermitItemsListView = "Person_WorkPermitItems_ListView";
    public const string InvitationItemsListView = "Person_InvitationItems_ListView";
    public const string RejectionItemsListView = "Person_RejectionItems_ListView";
    public const string FamilyMembersListView = "Person_FamilyMembers_ListView";

    public static readonly string[] ReadOnlyNestedListViewIds =
    [
        ApplicationProfileInstancesListView,
        WorkPermitItemsListView,
        InvitationItemsListView,
        RejectionItemsListView,
        FamilyMembersListView,
    ];

    public static readonly string TypedDetailViewIds =
        $"{PersonDetailViewIds.Employee};{PersonDetailViewIds.FamilyMember};{PersonDetailViewIds.TemporaryVisitor}";
}