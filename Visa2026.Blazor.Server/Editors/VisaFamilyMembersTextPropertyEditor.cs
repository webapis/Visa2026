using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), VisaFamilyMembersTextEditorAliases.Default, false)]
public class VisaFamilyMembersTextPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private IObjectSpace _objectSpace;
    private VisaFamilyMembersTextEditorAttribute _settings;
    private VisaFamilyMembersTextLocalization.UiTexts _ui;

    public VisaFamilyMembersTextPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override VisaFamilyMembersTextModel ComponentModel => (VisaFamilyMembersTextModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _objectSpace = objectSpace;

    protected override IComponentModel CreateComponentModel()
    {
        _settings = ResolveSettings();
        _ui = VisaFamilyMembersTextLocalization.Resolve();
        var model = new VisaFamilyMembersTextModel();
        ApplyLocalizedUiTexts(model);

        model.LinesCommitted = EventCallback.Factory.Create<IReadOnlyList<VisaFamilyMemberLineDto>>(this, OnLinesCommittedAsync);
        model.DraftLinesChanged = EventCallback.Factory.Create<IReadOnlyList<VisaFamilyMemberLineDto>>(this, draft =>
        {
            model.DraftLines = CloneLines(draft);
        });
        model.PopupVisibleChanged = EventCallback.Factory.Create<bool>(this, visible =>
        {
            model.PopupVisible = visible;
            if (visible)
            {
                SetStatus(string.Empty, isError: false);
                model.DraftLines = CloneLines(model.Lines);
                model.RelationshipOptions = VisaFamilyMemberLinesHelper.LoadRelationshipOptions(_objectSpace);
                model.CountryOptions = VisaFamilyMemberLinesHelper.LoadCountryOptions(_objectSpace);
            }
        });

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
        _ui ??= VisaFamilyMembersTextLocalization.Resolve();
        ApplyLocalizedUiTexts(ComponentModel);

        ComponentModel.ObjectSpace = _objectSpace;
        ComponentModel.ReadOnly = !AllowEdit;
        ComponentModel.RelationshipOptions = VisaFamilyMemberLinesHelper.LoadRelationshipOptions(_objectSpace);
        ComponentModel.CountryOptions = VisaFamilyMemberLinesHelper.LoadCountryOptions(_objectSpace);

        var lines = EnrichLines(VisaFamilyMemberLinesHelper.Parse(PropertyValue as string));
        ComponentModel.Lines = lines;
        if (!ComponentModel.PopupVisible)
        {
            ComponentModel.DraftLines = CloneLines(lines);
        }

        ComponentModel.DisplayText = VisaFamilyMemberLinesHelper.FormatDisplaySummary(
            PropertyValue as string,
            _ui.SummaryEmptyMessage,
            _ui.SummaryMemberCountFormat);

        // Tab/layout refresh must not reopen the picker (DxPopup can spuriously bind Visible on first show).
        if (ComponentModel.PopupVisible && !ComponentModel.StatusIsError)
        {
            ComponentModel.PopupVisible = false;
        }
    }

    protected override object GetControlValueCore()
    {
        var formatted = VisaFamilyMemberLinesHelper.Format(ComponentModel?.Lines);
        if (!string.IsNullOrWhiteSpace(formatted))
        {
            return formatted;
        }

        return PropertyValue as string ?? string.Empty;
    }

    protected override void ApplyReadOnly()
    {
        base.ApplyReadOnly();
        if (ComponentModel != null)
        {
            ComponentModel.ReadOnly = !AllowEdit;
        }
    }

    private Task OnLinesCommittedAsync(IReadOnlyList<VisaFamilyMemberLineDto> draft)
    {
        if (ComponentModel == null)
        {
            return Task.CompletedTask;
        }

        var lines = CloneLines(draft);
        if (!VisaFamilyMemberLinesHelper.TryValidate(lines, out var error))
        {
            SetStatus(
                string.IsNullOrWhiteSpace(error) ? _ui.ValidationFailedMessage : error,
                isError: true);
            ComponentModel.PopupVisible = true;
            return Task.CompletedTask;
        }

        foreach (var row in lines)
        {
            var relationship = VisaFamilyMemberLinesHelper.ResolveRelationship(
                _objectSpace,
                row.RelationshipOid,
                row.RelationshipNameTm);
            VisaFamilyMemberLinesHelper.ApplyRelationshipSelection(row, relationship);

            var country = VisaFamilyMemberLinesHelper.ResolveCountry(
                _objectSpace,
                row.CountryOid,
                row.CountryCode);
            VisaFamilyMemberLinesHelper.ApplyCountrySelection(row, country);
        }

        ComponentModel.Lines = lines;
        ComponentModel.DraftLines = CloneLines(lines);
        var formatted = VisaFamilyMemberLinesHelper.Format(lines);
        PropertyValue = formatted;
        ComponentModel.DisplayText = VisaFamilyMemberLinesHelper.FormatDisplaySummary(
            formatted,
            _ui.SummaryEmptyMessage,
            _ui.SummaryMemberCountFormat);
        ComponentModel.PopupVisible = false;
        SetStatus(string.Empty, isError: false);
        OnControlValueChanged();
        return Task.CompletedTask;
    }

    private IReadOnlyList<VisaFamilyMemberLineDto> EnrichLines(IReadOnlyList<VisaFamilyMemberLineDto> lines)
    {
        if (lines.Count == 0)
        {
            return lines;
        }

        var enriched = CloneLines(lines);
        foreach (var row in enriched)
        {
            var relationship = VisaFamilyMemberLinesHelper.ResolveRelationship(
                _objectSpace,
                row.RelationshipOid,
                row.RelationshipNameTm);
            if (relationship != null)
            {
                VisaFamilyMemberLinesHelper.ApplyRelationshipSelection(row, relationship);
            }

            var country = VisaFamilyMemberLinesHelper.ResolveCountry(
                _objectSpace,
                row.CountryOid,
                row.CountryCode);
            if (country != null)
            {
                VisaFamilyMemberLinesHelper.ApplyCountrySelection(row, country);
            }
        }

        return enriched;
    }

    private void SetStatus(string message, bool isError)
    {
        if (ComponentModel == null)
        {
            return;
        }

        ComponentModel.StatusMessage = message;
        ComponentModel.StatusIsError = isError;
    }

    private VisaFamilyMembersTextEditorAttribute ResolveSettings() =>
        MemberInfo?.FindAttribute<VisaFamilyMembersTextEditorAttribute>(true)
        ?? new VisaFamilyMembersTextEditorAttribute();

    private void ApplyLocalizedUiTexts(VisaFamilyMembersTextModel model)
    {
        _ui ??= VisaFamilyMembersTextLocalization.Resolve();
        model.PopupTitle = _ui.PopupTitle;
        model.PopupButtonTitle = _ui.PopupButtonTitle;
        model.OkText = _ui.OkText;
        model.CancelText = _ui.CancelText;
        model.AddButtonText = _ui.AddButtonText;
        model.EditButtonText = _ui.EditButtonText;
        model.DeleteButtonText = _ui.DeleteButtonText;
        model.EditPopupTitle = _ui.EditPopupTitle;
        model.SaveEditText = _ui.SaveEditText;
        model.DeleteConfirmTitle = _ui.DeleteConfirmTitle;
        model.DeleteConfirmFormat = _ui.DeleteConfirmFormat;
        model.MemberCountFormat = _ui.MemberCountFormat;
        model.EmptyListMessage = _ui.EmptyListMessage;
        model.FullNameLabel = _ui.FullNameLabel;
        model.BirthDateLabel = _ui.BirthDateLabel;
        model.RelationshipLabel = _ui.RelationshipLabel;
        model.CountryLabel = _ui.CountryLabel;
        model.EditFormIncompleteHint = _ui.EditFormIncompleteHint;
    }

    private static List<VisaFamilyMemberLineDto> CloneLines(IEnumerable<VisaFamilyMemberLineDto>? source) =>
        source == null
            ? new List<VisaFamilyMemberLineDto>()
            : source.Select(line => new VisaFamilyMemberLineDto
            {
                RowId = line.RowId,
                FullName = line.FullName,
                BirthDate = line.BirthDate,
                RelationshipNameTm = line.RelationshipNameTm,
                RelationshipOid = line.RelationshipOid,
                CountryCode = line.CountryCode,
                CountryOid = line.CountryOid,
                IsLegacyIncomplete = line.IsLegacyIncomplete,
            }).ToList();
}
