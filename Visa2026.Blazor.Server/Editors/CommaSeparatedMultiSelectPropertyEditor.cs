using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), CommaSeparatedMultiSelectEditorAliases.BorderZone, false)]
[PropertyEditor(typeof(string), CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation, false)]
public class CommaSeparatedMultiSelectPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private IObjectSpace _objectSpace;
    private CommaSeparatedMultiSelectAttribute _settings;

    public CommaSeparatedMultiSelectPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override CommaSeparatedMultiSelectModel ComponentModel => (CommaSeparatedMultiSelectModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _objectSpace = objectSpace;
    }

    protected override IComponentModel CreateComponentModel()
    {
        _settings = ResolveSettings();
        var model = new CommaSeparatedMultiSelectModel
        {
            ObjectSpace = _objectSpace,
            AllowAddNew = _settings.AllowAddNew,
            AllowManageCatalog = _settings.AllowAddNew && _settings.AllowManageCatalog,
        };
        ApplyLocalizedUiTexts(model);

        model.SelectedItemsChanged = EventCallback.Factory.Create<HashSet<string>>(this, OnSelectedItemsChanged);
        model.DraftSelectedItemsChanged = EventCallback.Factory.Create<HashSet<string>>(this, draft =>
        {
            model.DraftSelectedItems = CloneSet(draft);
        });
        model.PopupVisibleChanged = EventCallback.Factory.Create<bool>(this, visible =>
        {
            model.PopupVisible = visible;
            if (visible)
            {
                SetCatalogStatus(string.Empty, isError: false);
            }
        });
        model.NewItemNameChanged = EventCallback.Factory.Create<string>(this, text =>
        {
            model.NewItemName = text;
        });
        model.AddItemRequested = EventCallback.Factory.Create<string>(this, AddNewCatalogItemAsync);
        model.RenameCatalogRequested = EventCallback.Factory.Create<CatalogRenameRequest>(this, RenameCatalogItemAsync);
        model.DeleteCatalogRequested = EventCallback.Factory.Create<string>(this, DeleteCatalogItemAsync);

        return model;
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
        {
            return;
        }

        _settings ??= ResolveSettings();
        ApplySettingsToComponentModel(ComponentModel);

        var popupWasOpen = ComponentModel.PopupVisible;

        ComponentModel.ObjectSpace = _objectSpace;
        var selected = CommaSeparatedSelectionHelper.ParseSelected(PropertyValue as string, _settings.NoneValue);
        ComponentModel.SelectedItems = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        ComponentModel.DisplayText = FormatDisplayText(PropertyValue as string);
        ComponentModel.ReadOnly = !AllowEdit;

        if (popupWasOpen)
        {
            var draft = ComponentModel.DraftSelectedItems ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ComponentModel.CatalogItems = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
                LoadCatalogNames(),
                draft);
            ComponentModel.PopupVisible = true;
        }
        else
        {
            ComponentModel.CatalogItems = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
                LoadCatalogNames(),
                selected);
            ComponentModel.DraftSelectedItems = CloneSet(ComponentModel.SelectedItems);
            ComponentModel.NewItemName = string.Empty;
            ComponentModel.PopupVisible = false;
        }
    }

    protected override object GetControlValueCore() =>
        CommaSeparatedSelectionHelper.FormatSelected(ComponentModel?.SelectedItems, _settings?.NoneValue);

    protected override void ApplyReadOnly()
    {
        base.ApplyReadOnly();
        if (ComponentModel != null)
        {
            ComponentModel.ReadOnly = !AllowEdit;
        }
    }

    private void OnSelectedItemsChanged(HashSet<string> selected)
    {
        if (ComponentModel == null)
        {
            return;
        }

        _settings ??= ResolveSettings();
        ComponentModel.SelectedItems = selected ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ComponentModel.DraftSelectedItems = CloneSet(ComponentModel.SelectedItems);
        ComponentModel.DisplayText = CommaSeparatedSelectionHelper.FormatSelected(
            ComponentModel.SelectedItems,
            _settings.NoneValue);
        ComponentModel.PopupVisible = false;
        CommitPendingCatalogEntries();
        OnControlValueChanged();
        WriteValue();
    }

    private Task AddNewCatalogItemAsync(string? nameFromUi)
    {
        if (ComponentModel == null || _objectSpace == null || !AllowEdit || _settings == null || !_settings.AllowAddNew)
        {
            return Task.CompletedTask;
        }

        var name = (nameFromUi ?? ComponentModel.NewItemName)?.Trim();
        if (string.IsNullOrWhiteSpace(name)
            || CommaSeparatedSelectionHelper.IsNoneValue(name, _settings.NoneValue))
        {
            return Task.CompletedTask;
        }

        CommaSeparatedCatalogHelper.TryAddCatalogEntry(
            _objectSpace,
            _settings.CatalogEntityType,
            name,
            commitChanges: false);

        var draft = CloneSet(ComponentModel.DraftSelectedItems);
        draft.Add(name);

        ComponentModel.NewItemName = string.Empty;
        ComponentModel.DraftSelectedItems = draft;
        ComponentModel.CatalogItems = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
            LoadCatalogNames(),
            draft);
        ComponentModel.PopupVisible = true;

        return Task.CompletedTask;
    }

    private Task RenameCatalogItemAsync(CatalogRenameRequest request)
    {
        if (ComponentModel == null || _objectSpace == null || !AllowEdit || _settings == null
            || !_settings.AllowManageCatalog)
        {
            return Task.CompletedTask;
        }

        var result = CommaSeparatedCatalogHelper.TryRenameCatalogEntry(
            _objectSpace,
            _settings.CatalogEntityType,
            request.OldName,
            request.NewName,
            _settings.NoneValue,
            commitChanges: false);

        if (!result.Success)
        {
            SetCatalogStatus(FormatCatalogError(result), isError: true);
            return Task.CompletedTask;
        }

        var newName = request.NewName.Trim();
        var draft = CloneSet(ComponentModel.DraftSelectedItems);
        if (draft.Remove(request.OldName))
        {
            draft.Add(newName);
        }

        var selected = CloneSet(ComponentModel.SelectedItems);
        if (selected.Remove(request.OldName))
        {
            selected.Add(newName);
        }

        ComponentModel.DraftSelectedItems = draft;
        ComponentModel.SelectedItems = selected;
        ComponentModel.DisplayText = CommaSeparatedSelectionHelper.FormatSelected(
            ComponentModel.SelectedItems,
            _settings.NoneValue);

        var stored = PropertyValue as string;
        if (CommaSeparatedSelectionHelper.ContainsLabel(stored, request.OldName, _settings.NoneValue))
        {
            PropertyValue = CommaSeparatedSelectionHelper.ReplaceLabel(
                stored,
                request.OldName,
                newName,
                _settings.NoneValue);
            OnControlValueChanged();
        }

        RefreshCatalogInPopup(
            draft,
            successMessage: CommaSeparatedMultiSelectLocalization.Resolve(GetEditorAlias()).RenameSuccessFormat);
        return Task.CompletedTask;
    }

    private Task DeleteCatalogItemAsync(string name)
    {
        if (ComponentModel == null || _objectSpace == null || !AllowEdit || _settings == null
            || !_settings.AllowManageCatalog)
        {
            return Task.CompletedTask;
        }

        var usageContext = BuildUsageContext();
        var result = CommaSeparatedCatalogHelper.TryDeleteCatalogEntry(
            _objectSpace,
            _settings.CatalogEntityType,
            name,
            _settings.NoneValue,
            commitChanges: false,
            usageContext: usageContext);

        if (!result.Success)
        {
            SetCatalogStatus(FormatCatalogError(result), isError: true);
            return Task.CompletedTask;
        }

        var draft = CloneSet(ComponentModel.DraftSelectedItems);
        draft.Remove(name);
        var selected = CloneSet(ComponentModel.SelectedItems);
        selected.Remove(name);

        ComponentModel.DraftSelectedItems = draft;
        ComponentModel.SelectedItems = selected;
        var formatted = CommaSeparatedSelectionHelper.FormatSelected(draft, _settings.NoneValue);
        ComponentModel.DisplayText = formatted;
        PropertyValue = formatted;
        OnControlValueChanged();

        var ui = CommaSeparatedMultiSelectLocalization.Resolve(GetEditorAlias());
        var successMessage = result.UsageCount > 0
            ? string.Format(ui.DeleteSuccessFormat, result.UsageCount)
            : ui.DeleteSuccessUnusedFormat;
        RefreshCatalogInPopup(draft, successMessage: successMessage, excludeCatalogName: name.Trim());
        return Task.CompletedTask;
    }

    private CatalogUsageContext? BuildUsageContext()
    {
        if (ComponentModel == null || _settings == null)
        {
            return null;
        }

        var editingObject = CurrentObject as BaseObject;
        return new CatalogUsageContext
        {
            EditingObjectId = editingObject?.ID,
            EditingEffectiveStored = CommaSeparatedSelectionHelper.FormatSelected(
                ComponentModel.DraftSelectedItems,
                _settings.NoneValue),
        };
    }

    private void RefreshCatalogInPopup(
        HashSet<string> draft,
        string? successMessage,
        string? excludeCatalogName = null)
    {
        var catalog = LoadCatalogNames();
        if (!string.IsNullOrWhiteSpace(excludeCatalogName))
        {
            catalog = catalog
                .Where(n => !string.Equals(n, excludeCatalogName.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ComponentModel.CatalogItems = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(catalog, draft)
            .ToList();
        ComponentModel.PopupVisible = true;
        SetCatalogStatus(successMessage ?? string.Empty, isError: false);
    }

    private void SetCatalogStatus(string message, bool isError)
    {
        if (ComponentModel == null)
        {
            return;
        }

        ComponentModel.StatusMessage = message;
        ComponentModel.StatusIsError = isError;
    }

    private static string FormatCatalogError(CatalogOperationResult result) =>
        CommaSeparatedMultiSelectLocalization.LocalizeCatalogMessage(result.Message);

    private void CommitPendingCatalogEntries()
    {
        if (_objectSpace == null || !_objectSpace.IsModified)
        {
            return;
        }

        _objectSpace.CommitChanges();
    }

    private IReadOnlyList<string> LoadCatalogNames() =>
        CommaSeparatedCatalogHelper.LoadCatalogNames(
            _objectSpace,
            _settings?.CatalogEntityType,
            _settings?.NoneValue);

    private CommaSeparatedMultiSelectAttribute ResolveSettings()
    {
        var attribute = MemberInfo?.FindAttribute<CommaSeparatedMultiSelectAttribute>(true);
        if (attribute != null)
        {
            return attribute;
        }

        var editorType = Model is IModelMemberViewItem memberModel ? memberModel.PropertyEditorType : null;
        var alias = editorType?.Name;
        return CommaSeparatedMultiSelectDefaults.ForEditorAlias(alias);
    }

    private void ApplySettingsToComponentModel(CommaSeparatedMultiSelectModel model)
    {
        if (_settings == null)
        {
            return;
        }

        model.AllowAddNew = _settings.AllowAddNew;
        model.AllowManageCatalog = _settings.AllowAddNew && _settings.AllowManageCatalog && AllowEdit;
        ApplyLocalizedUiTexts(model);
    }

    private void ApplyLocalizedUiTexts(CommaSeparatedMultiSelectModel model)
    {
        var ui = CommaSeparatedMultiSelectLocalization.Resolve(GetEditorAlias());
        model.PopupTitle = ui.PopupTitle;
        model.PopupButtonTitle = ui.PopupButtonTitle;
        model.AddPlaceholder = ui.AddPlaceholder;
        model.AddButtonText = ui.AddButtonText;
        model.OkText = ui.OkText;
        model.CancelText = ui.CancelText;
        model.SelectedCountFormat = ui.SelectedCountFormat;
        model.EditButtonText = ui.EditButtonText;
        model.DeleteButtonText = ui.DeleteButtonText;
        model.EditPopupTitle = ui.EditPopupTitle;
        model.SaveEditText = ui.SaveEditText;
        model.DeleteConfirmTitle = ui.DeleteConfirmTitle;
        model.DeleteConfirmFormat = ui.DeleteConfirmFormat;
        model.EmptyListMessage = ui.EmptyListMessage;
        model.ManageCatalogButtonText = ui.ManageCatalogButtonText;
        model.DoneManageCatalogButtonText = ui.DoneManageCatalogButtonText;
    }

    private string? GetEditorAlias() =>
        _settings?.CatalogEntityType == typeof(WorkPermittedLocationName)
            ? CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation
            : CommaSeparatedMultiSelectEditorAliases.BorderZone;

    private string FormatDisplayText(string? stored) =>
        CommaSeparatedSelectionHelper.FormatSelected(
            CommaSeparatedSelectionHelper.ParseSelected(stored, _settings?.NoneValue),
            _settings?.NoneValue);

    private static HashSet<string> CloneSet(HashSet<string>? source) =>
        source == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(source, StringComparer.OrdinalIgnoreCase);
}
