using System;

namespace Visa2026.Module.Editors;

/// <summary>Configures the Blazor visa family members text property editor.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VisaFamilyMembersTextEditorAttribute : Attribute
{
    public string PopupTitle { get; init; } = "Family members for visa (manual)";

    public string PopupButtonTitle { get; init; } = "Edit family members for visa";

    public string OkText { get; init; } = "OK";

    public string CancelText { get; init; } = "Cancel";

    public string AddButtonText { get; init; } = "Add member";

    public string EditButtonText { get; init; } = "Edit";

    public string DeleteButtonText { get; init; } = "Delete";

    public string EditPopupTitle { get; init; } = "Family member";

    public string SaveEditText { get; init; } = "Save";

    public string DeleteConfirmTitle { get; init; } = "Delete?";

    public string DeleteConfirmFormat { get; init; } = "Remove \"{0}\" from the list?";

    public string MemberCountFormat { get; init; } = "Members: {0}";

    public string SummaryEmptyMessage { get; init; } = "No family members declared";

    public string SummaryMemberCountFormat { get; init; } = "{0} family member(s)";

    public string EmptyListMessage { get; init; } = "No family members. Click Add member.";

    public string FullNameLabel { get; init; } = "Full name";

    public string BirthDateLabel { get; init; } = "Birth date";

    public string RelationshipLabel { get; init; } = "Relationship";
}

/// <summary>Registered <see cref="DevExpress.ExpressApp.Editors.EditorAliasAttribute"/> value.</summary>
public static class VisaFamilyMembersTextEditorAliases
{
    public const string Default = "VisaFamilyMembersTextEditor";
}
