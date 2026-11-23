#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
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
            FieldName = IsWorkPermit(_settings) ? "work permit location" : "border zone location",
            Placeholder = "Start typing a location",
            EmptyHint = "Search saved locations. Use the + button to add a new one.",
            ValueChanged = EventCallback.Factory.Create<string?>(this, OnValueChanged),
            AddSaved = EventCallback.Factory.Create<string>(this, OnAddSavedAsync),
            RenameSaved = EventCallback.Factory.Create<(string Previous, string Next)>(this, OnRenameSavedAsync),
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
        ComponentModel.FieldName = IsWorkPermit(_settings) ? "work permit location" : "border zone location";
        ComponentModel.EmptyHint = "Search saved locations. Use the + button to add a new one.";
        EnsureHydrated(ComponentModel.Value);
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
        var previous = ComponentModel.Value;
        ComponentModel.Value = formatted;
        PropertyValue = formatted;
        TryRemember(previous, formatted);
        ComponentModel.Suggestions = LoadSuggestions();
        OnControlValueChanged();
    }

    private void TryRemember(string? previous, string? stored)
    {
        if (_objectSpace == null || _settings == null)
            return;
        if (string.Equals(previous ?? string.Empty, stored ?? string.Empty, StringComparison.Ordinal))
            return;

        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var application = ResolveApplication(CurrentObject);
        if (application?.ApplicationProfile == null)
            return;

        CaseSavedLocation.ReplaceEdited(
            _objectSpace,
            application.ApplicationProfile,
            field,
            previous,
            stored,
            _settings.NoneValue);
    }

    private void EnsureHydrated(string? stored)
    {
        if (_objectSpace == null || _settings == null)
            return;
        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var application = ResolveApplication(CurrentObject);
        if (application?.ApplicationProfile == null)
            return;
        var changed = false;
        foreach (var label in CaseLocationAutocomplete.ParseChips(stored, _settings.NoneValue))
        {
            if (CaseSavedLocation.EnsureExists(_objectSpace, application.ApplicationProfile, field, label))
                changed = true;
        }
        if (changed)
            ComponentModel.Suggestions = LoadSuggestions();
    }

    private Task OnAddSavedAsync(string text)
    {
        if (_objectSpace == null || _settings == null)
            return Task.CompletedTask;
        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var application = ResolveApplication(CurrentObject);
        if (application?.ApplicationProfile == null)
            return Task.CompletedTask;
        if (!CaseSavedLocation.TryAdd(_objectSpace, application.ApplicationProfile, field, text, out var error))
            throw new InvalidOperationException(error ?? CaseSavedLocation.DuplicateError);
        ComponentModel.Suggestions = LoadSuggestions();
        return Task.CompletedTask;
    }

    private Task OnRenameSavedAsync((string Previous, string Next) pair)
    {
        if (_objectSpace == null || _settings == null)
            return Task.CompletedTask;
        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        var application = ResolveApplication(CurrentObject);
        if (application?.ApplicationProfile == null)
            return Task.CompletedTask;
        if (!CaseSavedLocation.TryRename(
                _objectSpace, application.ApplicationProfile, field, pair.Previous, pair.Next, out var error))
            throw new InvalidOperationException(error ?? CaseSavedLocation.DuplicateError);
        ComponentModel.Suggestions = LoadSuggestions();
        return Task.CompletedTask;
    }

    private System.Collections.Generic.IReadOnlyList<CaseAddressAutocomplete.SavedEntry> LoadSuggestions()
    {
        if (_objectSpace == null || _settings == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        var field = IsWorkPermit(_settings)
            ? ApplicationProfileSavedLocationField.WorkPermitLocation
            : ApplicationProfileSavedLocationField.BorderZone;
        try
        {
            return CaseSavedLocation.List(_objectSpace, field);
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

    private static ApplicationProfileInstance? ResolveApplication(object? current) => current switch
    {
        ApplicationProfileInstance application => application,
        Visa visa => visa.IssuingApplicationProfileInstance
            ?? visa.ApplicationProfileInstances?.FirstOrDefault(),
        Invitation invitation => invitation.ApplicationProfileInstance,
        WorkPermitItem item => item.ApplicationProfileInstance
            ?? item.ApplicationProfileInstances?.FirstOrDefault(),
        BorderZoneItem item => item.BorderZone?.ApplicationProfileInstance,
        WorkPermitLocationGrantItem item => item.Grant?.ApplicationProfileInstance,
        ApplicationRosterMergeLine line => line.ApplicationProfileInstance,
        _ => null,
    };
}
