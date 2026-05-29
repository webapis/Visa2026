namespace Visa2026.Module.Localization;

/// <summary>Localized UI strings for the visa family members text property editor.</summary>
public static class VisaFamilyMembersTextLocalization
{
    public sealed class UiTexts
    {
        public string PopupTitle { get; init; } = string.Empty;
        public string PopupButtonTitle { get; init; } = string.Empty;
        public string OkText { get; init; } = string.Empty;
        public string CancelText { get; init; } = string.Empty;
        public string AddButtonText { get; init; } = string.Empty;
        public string EditButtonText { get; init; } = string.Empty;
        public string DeleteButtonText { get; init; } = string.Empty;
        public string EditPopupTitle { get; init; } = string.Empty;
        public string SaveEditText { get; init; } = string.Empty;
        public string DeleteConfirmTitle { get; init; } = string.Empty;
        public string DeleteConfirmFormat { get; init; } = string.Empty;
        public string MemberCountFormat { get; init; } = string.Empty;
        public string SummaryEmptyMessage { get; init; } = string.Empty;
        public string SummaryMemberCountFormat { get; init; } = string.Empty;
        public string EmptyListMessage { get; init; } = string.Empty;
        public string FullNameLabel { get; init; } = string.Empty;
        public string BirthDateLabel { get; init; } = string.Empty;
        public string RelationshipLabel { get; init; } = string.Empty;
        public string ValidationFailedMessage { get; init; } = string.Empty;
    }

    public static UiTexts Resolve() => new()
    {
        PopupTitle = VisaUiMessages.Get("VisaFamilyMembersText.PopupTitle"),
        PopupButtonTitle = VisaUiMessages.Get("VisaFamilyMembersText.PopupButtonTitle"),
        OkText = VisaUiMessages.Get("VisaFamilyMembersText.Ok"),
        CancelText = VisaUiMessages.Get("VisaFamilyMembersText.Cancel"),
        AddButtonText = VisaUiMessages.Get("VisaFamilyMembersText.AddMember"),
        EditButtonText = VisaUiMessages.Get("VisaFamilyMembersText.Edit"),
        DeleteButtonText = VisaUiMessages.Get("VisaFamilyMembersText.Delete"),
        EditPopupTitle = VisaUiMessages.Get("VisaFamilyMembersText.EditPopupTitle"),
        SaveEditText = VisaUiMessages.Get("VisaFamilyMembersText.Save"),
        DeleteConfirmTitle = VisaUiMessages.Get("VisaFamilyMembersText.DeleteConfirmTitle"),
        DeleteConfirmFormat = VisaUiMessages.Get("VisaFamilyMembersText.DeleteConfirm"),
        MemberCountFormat = VisaUiMessages.Get("VisaFamilyMembersText.MemberCount"),
        SummaryEmptyMessage = VisaUiMessages.Get("VisaFamilyMembersText.SummaryEmpty"),
        SummaryMemberCountFormat = VisaUiMessages.Get("VisaFamilyMembersText.SummaryCount"),
        EmptyListMessage = VisaUiMessages.Get("VisaFamilyMembersText.EmptyList"),
        FullNameLabel = VisaUiMessages.Get("VisaFamilyMembersText.FullName"),
        BirthDateLabel = VisaUiMessages.Get("VisaFamilyMembersText.BirthDate"),
        RelationshipLabel = VisaUiMessages.Get("VisaFamilyMembersText.Relationship"),
        ValidationFailedMessage = VisaUiMessages.Get("VisaFamilyMembersText.ValidationFailed"),
    };
}
