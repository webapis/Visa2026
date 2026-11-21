#nullable enable
using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

/// <summary>
/// DetailView editor for comma-joined border-zone / work-permit location strings.
/// Uses chip multi-select (<see cref="LocationMultiSelectField"/>); catalog popup retired.
/// </summary>
[PropertyEditor(typeof(string), CommaSeparatedMultiSelectEditorAliases.BorderZone, false)]
[PropertyEditor(typeof(string), CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation, false)]
public class CommaSeparatedMultiSelectPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private IObjectSpace? _objectSpace;
    private CommaSeparatedMultiSelectAttribute? _settings;

    public CommaSeparatedMultiSelectPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override LocationMultiSelectEditorModel ComponentModel =>
        (LocationMultiSelectEditorModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _objectSpace = objectSpace;

    protected override IComponentModel CreateComponentModel()
    {
        _settings = ResolveSettings();
        var model = new LocationMultiSelectEditorModel
        {
            NoneValue = _settings.NoneValue,
            Placeholder = "Search or type a new location",
            EmptyHint = IsWorkPermit(_settings)
                ? "Search or type a work permit location."
                : "Search or type a border zone location.",
            ValueChanged = EventCallback.Factory.Create<string?>(this, OnValueChanged),
        };
        return model;
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
            return;

        _settings ??= ResolveSettings();
        ComponentModel.NoneValue = _settings.NoneValue;
        ComponentModel.Value = PropertyValue as string;
        ComponentModel.ReadOnly = !AllowEdit;
        ComponentModel.Enabled = AllowEdit;
        ComponentModel.Suggestions = LoadSuggestions();
        ComponentModel.EmptyHint = IsWorkPermit(_settings)
            ? "Search or type a work permit location."
            : "Search or type a border zone location.";
    }

    protected override object GetControlValueCore() => ComponentModel?.Value ?? string.Empty;

    protected override void ApplyReadOnly()
    {
        base.ApplyReadOnly();
        if (ComponentModel == null)
            return;
        ComponentModel.ReadOnly = !AllowEdit;
        ComponentModel.Enabled = AllowEdit;
    }

    private void OnValueChanged(string? value)
    {
        if (ComponentModel == null)
            return;

        _settings ??= ResolveSettings();
        var none = _settings.NoneValue;
        var formatted = CaseLocationAutocomplete.FormatChips(
            CaseLocationAutocomplete.ParseChips(value, none),
            none);
        ComponentModel.Value = formatted;
        PropertyValue = formatted;
        TryRemember(formatted);
        OnControlValueChanged();
    }

    private void TryRemember(string? stored)
    {
        if (_objectSpace == null || _settings == null)
            return;

        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var labels = CaseLocationAutocomplete.ParseChips(stored, _settings.NoneValue);

        if (CurrentObject is ApplicationProfileInstance application)
            CaseSavedLocation.RememberAll(_objectSpace, application, field, labels);

    }

    private System.Collections.Generic.IReadOnlyList<CaseAddressAutocomplete.SavedEntry> LoadSuggestions()
    {
        if (_objectSpace == null || _settings == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var app = CurrentObject as ApplicationProfileInstance;
        try
        {
            return CaseSavedLocation.List(_objectSpace, field, app);
        }
        catch
        {
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();
        }
    }

    private CommaSeparatedMultiSelectAttribute ResolveSettings()
    {
        var attribute = MemberInfo?.FindAttribute<CommaSeparatedMultiSelectAttribute>(true);
        if (attribute != null)
            return attribute;

        var editorType = Model is IModelMemberViewItem memberModel ? memberModel.PropertyEditorType : null;
        return CommaSeparatedMultiSelectDefaults.ForEditorAlias(editorType?.Name);
    }

    private static bool IsWorkPermit(CommaSeparatedMultiSelectAttribute settings) =>
        settings.CatalogEntityType == typeof(WorkPermittedLocationName)
        || string.IsNullOrEmpty(settings.NoneValue);
}
