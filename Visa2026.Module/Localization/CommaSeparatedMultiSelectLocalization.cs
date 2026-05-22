using Visa2026.Module.Editors;

namespace Visa2026.Module.Localization;

/// <summary>Localized UI strings for the comma-separated multi-select property editor (Blazor popup).</summary>
public static class CommaSeparatedMultiSelectLocalization
{
    public sealed class UiTexts
    {
        public string PopupTitle { get; init; } = string.Empty;
        public string PopupButtonTitle { get; init; } = string.Empty;
        public string AddPlaceholder { get; init; } = string.Empty;
        public string AddButtonText { get; init; } = string.Empty;
        public string OkText { get; init; } = string.Empty;
        public string CancelText { get; init; } = string.Empty;
        public string SelectedCountFormat { get; init; } = string.Empty;
        public string EditButtonText { get; init; } = string.Empty;
        public string DeleteButtonText { get; init; } = string.Empty;
        public string EditPopupTitle { get; init; } = string.Empty;
        public string SaveEditText { get; init; } = string.Empty;
        public string RenameSuccessFormat { get; init; } = string.Empty;
        public string DeleteConfirmTitle { get; init; } = string.Empty;
        public string DeleteConfirmFormat { get; init; } = string.Empty;
        public string DeleteSuccessFormat { get; init; } = string.Empty;
        public string DeleteSuccessUnusedFormat { get; init; } = string.Empty;
        public string EmptyListMessage { get; init; } = string.Empty;
        public string ManageCatalogButtonText { get; init; } = string.Empty;
        public string DoneManageCatalogButtonText { get; init; } = string.Empty;
    }

    public static UiTexts Resolve(string? editorAlias) =>
        string.Equals(
            editorAlias,
            CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation,
            StringComparison.Ordinal)
            ? ResolveWorkPermittedLocation()
            : ResolveBorderZone();

    private static UiTexts ResolveBorderZone() => new()
    {
        PopupTitle = VisaUiMessages.Get("CommaMultiSelect.BorderZone.PopupTitle"),
        PopupButtonTitle = VisaUiMessages.Get("CommaMultiSelect.BorderZone.PopupButtonTitle"),
        AddPlaceholder = VisaUiMessages.Get("CommaMultiSelect.BorderZone.AddPlaceholder"),
        AddButtonText = VisaUiMessages.Get("CommaMultiSelect.Add"),
        OkText = VisaUiMessages.Get("CommaMultiSelect.Ok"),
        CancelText = VisaUiMessages.Get("CommaMultiSelect.Cancel"),
        SelectedCountFormat = VisaUiMessages.Get("CommaMultiSelect.SelectedCount"),
        EditButtonText = VisaUiMessages.Get("CommaMultiSelect.Edit"),
        DeleteButtonText = VisaUiMessages.Get("CommaMultiSelect.Delete"),
        EditPopupTitle = VisaUiMessages.Get("CommaMultiSelect.RenameTitle"),
        SaveEditText = VisaUiMessages.Get("CommaMultiSelect.Save"),
        RenameSuccessFormat = VisaUiMessages.Get("CommaMultiSelect.RenameSuccess"),
        DeleteConfirmTitle = VisaUiMessages.Get("CommaMultiSelect.DeleteConfirmTitle"),
        DeleteConfirmFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteConfirm"),
        DeleteSuccessFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteSuccess"),
        DeleteSuccessUnusedFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteSuccessUnused"),
        EmptyListMessage = VisaUiMessages.Get("CommaMultiSelect.EmptyList"),
        ManageCatalogButtonText = VisaUiMessages.Get("CommaMultiSelect.ManageCatalog"),
        DoneManageCatalogButtonText = VisaUiMessages.Get("CommaMultiSelect.DoneManageCatalog"),
    };

    private static UiTexts ResolveWorkPermittedLocation() => new()
    {
        PopupTitle = VisaUiMessages.Get("CommaMultiSelect.WorkPermit.PopupTitle"),
        PopupButtonTitle = VisaUiMessages.Get("CommaMultiSelect.WorkPermit.PopupButtonTitle"),
        AddPlaceholder = VisaUiMessages.Get("CommaMultiSelect.WorkPermit.AddPlaceholder"),
        AddButtonText = VisaUiMessages.Get("CommaMultiSelect.Add"),
        OkText = VisaUiMessages.Get("CommaMultiSelect.Ok"),
        CancelText = VisaUiMessages.Get("CommaMultiSelect.Cancel"),
        SelectedCountFormat = VisaUiMessages.Get("CommaMultiSelect.SelectedCount"),
        EditButtonText = VisaUiMessages.Get("CommaMultiSelect.Edit"),
        DeleteButtonText = VisaUiMessages.Get("CommaMultiSelect.Delete"),
        EditPopupTitle = VisaUiMessages.Get("CommaMultiSelect.RenameTitle"),
        SaveEditText = VisaUiMessages.Get("CommaMultiSelect.Save"),
        RenameSuccessFormat = VisaUiMessages.Get("CommaMultiSelect.RenameSuccess"),
        DeleteConfirmTitle = VisaUiMessages.Get("CommaMultiSelect.DeleteConfirmTitle"),
        DeleteConfirmFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteConfirm"),
        DeleteSuccessFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteSuccess"),
        DeleteSuccessUnusedFormat = VisaUiMessages.Get("CommaMultiSelect.DeleteSuccessUnused"),
        EmptyListMessage = VisaUiMessages.Get("CommaMultiSelect.EmptyList"),
        ManageCatalogButtonText = VisaUiMessages.Get("CommaMultiSelect.ManageCatalog"),
        DoneManageCatalogButtonText = VisaUiMessages.Get("CommaMultiSelect.DoneManageCatalog"),
    };

    public static string LocalizeCatalogMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return VisaUiMessages.Get("CommaMultiSelect.Error.OperationFailed");
        }

        if (message.StartsWith("CommaMultiSelect.", StringComparison.Ordinal))
        {
            return VisaUiMessages.Get(message);
        }

        return message;
    }
}
