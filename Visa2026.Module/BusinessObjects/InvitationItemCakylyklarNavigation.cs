using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Top-level Invitations nav + Employee / Family member ListViews
/// (<see cref="docs/INVITATION_CAKYLYKLAR_LISTVIEW.md"/>).
/// </summary>
public static class InvitationItemCakylyklarNavigation
{
    public const string GroupNavId = "Invitations";
    public const string NavItemEmployee = "Employee";
    public const string NavItemFamilyMember = "FamilyMember";

    /// <summary>Legacy single nav under Application Profile Instances (hidden after split).</summary>
    public const string LegacyNavItemId = "InvitationItem_Cakylyklar";

    public const string ListViewEmployeesId = "InvitationItem_Cakylyklar_Employees_ListView";
    public const string ListViewFamilyMembersId = "InvitationItem_Cakylyklar_FamilyMembers_ListView";

    /// <summary>Legacy unsplit ListView id (kept for model diffs; no longer linked in nav).</summary>
    public const string ListViewId = "InvitationItem_Cakylyklar_ListView";

    public const string SourceListViewId = "InvitationItem_ListView";

    public const string CaptionGroup = "Invitations";
    public const string CaptionEmployee = "Invitations (Employee)";
    public const string CaptionFamilyMember = "Invitations (Family member)";

    public const string CaptionGroupTk = "Çakylyklar";
    public const string CaptionEmployeeTk = "Çakylyklar (Işgär)";
    public const string CaptionFamilyMemberTk = "Çakylyklar (Maşgala)";

    public const string CaptionGroupTr = "Davetiyeler";
    public const string CaptionEmployeeTr = "Davetiyeler (Çalışan)";
    public const string CaptionFamilyMemberTr = "Davetiyeler (Aile üyesi)";

    public const string CaptionGroupRu = "Приглашения";
    public const string CaptionEmployeeRu = "Приглашения (Сотрудник)";
    public const string CaptionFamilyMemberRu = "Приглашения (Член семьи)";

    /// <summary>Issued invitation lines only (parent invitation linked to an issuing case).</summary>
    public const string CriteriaIssued =
        "Invitation is not null And Invitation.ApplicationProfileInstance is not null";

    public const string CriteriaEmployees =
        CriteriaIssued
        + " And Person is not null And Person.PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,Employee#";

    public const string CriteriaFamilyMembers =
        CriteriaIssued
        + " And Person is not null And Person.PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,FamilyMember#";

    public const string ImageName = "BO_Contact";
    public const int GroupNavIndex = 2;

    public static readonly string[] AllListViewIds =
    [
        ListViewEmployeesId,
        ListViewFamilyMembersId,
    ];

    public static string TargetViewIds =>
        ListViewEmployeesId + ";" + ListViewFamilyMembersId;

    public static bool IsCakylyklarListView(string? viewId) =>
        string.Equals(viewId, ListViewEmployeesId, StringComparison.Ordinal)
        || string.Equals(viewId, ListViewFamilyMembersId, StringComparison.Ordinal)
        || string.Equals(viewId, ListViewId, StringComparison.Ordinal);
}