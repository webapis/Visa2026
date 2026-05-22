using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Editors;
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
            CollapsedVisibleCount = CommaSeparatedSelectionHelper.CollapsedVisibleCount,
            ObjectSpace = _objectSpace,
            AllowAddNew = _settings.AllowAddNew,
            PopupTitle = _settings.PopupTitle,
            PopupButtonTitle = _settings.PopupButtonTitle,
            SearchPlaceholder = _settings.SearchPlaceholder,
            AddPlaceholder = _settings.AddPlaceholder,
            AddButtonText = _settings.AddButtonText,
            ShowMoreText = _settings.ShowMoreText,
            ShowLessText = _settings.ShowLessText,
            OkText = _settings.OkText,
            CancelText = _settings.CancelText,
            SelectedCountFormat = _settings.SelectedCountFormat,
        };

        model.SelectedItemsChanged = EventCallback.Factory.Create<HashSet<string>>(this, OnSelectedItemsChanged);
        model.DraftSelectedItemsChanged = EventCallback.Factory.Create<HashSet<string>>(this, draft =>
        {
            model.DraftSelectedItems = CloneSet(draft);
        });
        model.PopupVisibleChanged = EventCallback.Factory.Create<bool>(this, visible =>
        {
            model.PopupVisible = visible;
        });
        model.ExpandedChanged = EventCallback.Factory.Create<bool>(this, expanded =>
        {
            model.Expanded = expanded;
        });
        model.FilterTextChanged = EventCallback.Factory.Create<string>(this, text =>
        {
            model.FilterText = text;
        });
        model.NewItemNameChanged = EventCallback.Factory.Create<string>(this, text =>
        {
            model.NewItemName = text;
        });
        model.AddItemRequested = EventCallback.Factory.Create<string>(this, AddNewCatalogItemAsync);

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
            ComponentModel.FilterText = string.Empty;
            ComponentModel.PopupVisible = false;
            ComponentModel.Expanded = false;
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
        ComponentModel.FilterText = string.Empty;
        ComponentModel.Expanded = true;
        ComponentModel.DraftSelectedItems = draft;
        ComponentModel.CatalogItems = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
            LoadCatalogNames(),
            draft);
        ComponentModel.PopupVisible = true;

        return Task.CompletedTask;
    }

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
        model.PopupTitle = _settings.PopupTitle;
        model.PopupButtonTitle = _settings.PopupButtonTitle;
        model.SearchPlaceholder = _settings.SearchPlaceholder;
        model.AddPlaceholder = _settings.AddPlaceholder;
        model.AddButtonText = _settings.AddButtonText;
        model.ShowMoreText = _settings.ShowMoreText;
        model.ShowLessText = _settings.ShowLessText;
        model.OkText = _settings.OkText;
        model.CancelText = _settings.CancelText;
        model.SelectedCountFormat = _settings.SelectedCountFormat;
    }

    private string FormatDisplayText(string? stored) =>
        CommaSeparatedSelectionHelper.FormatSelected(
            CommaSeparatedSelectionHelper.ParseSelected(stored, _settings?.NoneValue),
            _settings?.NoneValue);

    private static HashSet<string> CloneSet(HashSet<string>? source) =>
        source == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(source, StringComparer.OrdinalIgnoreCase);
}
