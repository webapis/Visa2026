using System;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using AppBO = Visa2026.Module.BusinessObjects.Application;

namespace Visa2026.Blazor.Server.Editors;

/// <summary>
/// Quick code field with an inline picker (…) that opens a link-style code table popup.
/// </summary>
[PropertyEditor(typeof(string), ApplicationTypeQuickCodeEditorAliases.QuickCode, false)]
public class ApplicationTypeQuickCodePropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private const string LogPrefix = "[AppTypeQuickCodeEditor]";
    private IObjectSpace _objectSpace;

    public ApplicationTypeQuickCodePropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override ApplicationTypeQuickCodeModel ComponentModel => (ApplicationTypeQuickCodeModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _objectSpace = objectSpace;

    protected override IComponentModel CreateComponentModel()
    {
        var model = new ApplicationTypeQuickCodeModel();
        model.ValueChanged = EventCallback.Factory.Create<string>(this, OnTextChangedAsync);
        model.PopupVisibleChanged = EventCallback.Factory.Create<bool>(this, OnPopupVisibleChanged);
        ApplyLocalizedUiTexts(model);
        return model;
    }

    private static void ApplyLocalizedUiTexts(ApplicationTypeQuickCodeModel model)
    {
        model.PickerButtonTitle = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerAction");
        model.PopupTitle = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerCaption");
        model.EmptyListMessage = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerEmpty");
        model.ColCode = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerColCode");
        model.ColType = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerColType");
        model.ColGroup = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerColGroup");
        model.CloseButtonText = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerClose");
        model.ApplyingMessage = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerApplying");
        model.ColStatus = VisaUiMessages.Get("ApplicationTypeQuickCode.PickerColStatus");
        model.StatusReadyTitle = VisaUiMessages.Get("ApplicationTypeQuickCode.StatusReady");
        model.StatusPendingTitle = VisaUiMessages.Get("ApplicationTypeQuickCode.StatusPending");
        model.StatusNotReadyTitle = VisaUiMessages.Get("ApplicationTypeQuickCode.StatusNotReady");
        model.ReadinessLegend = VisaUiMessages.Get("ApplicationTypeQuickCode.ReadinessLegend");
        model.ReadinessBlockedMessage = VisaUiMessages.Get("ApplicationTypeQuickCode.ReadinessBlockedPicker");
    }

    private void OnPopupVisibleChanged(bool visible)
    {
        if (ComponentModel == null)
            return;

        ComponentModel.PopupVisible = visible;
        if (visible)
            ReloadPickerRows();
    }

    private void ReloadPickerRows()
    {
        if (ComponentModel == null || _objectSpace == null)
            return;

        ComponentModel.ObjectSpace = _objectSpace;
        ComponentModel.PickerRows = ApplicationTypeCodePickerHelper.LoadRows(_objectSpace).ToList();
        Log($"Picker opened, rows={ComponentModel.PickerRows.Count}");
    }

    private async Task OnTextChangedAsync(string value)
    {
        var text = value ?? string.Empty;
        ComponentModel.Value = text;
        Log($"TextChanged len={text.Length} text='{text}'");

        var app = GetApplication();
        if (app != null)
        {
            var hadResolvedType = app.ApplicationType != null;
            if (!string.Equals(app.ApplicationTypeQuickCode, text, StringComparison.Ordinal))
                app.ApplicationTypeQuickCode = text;
            else if (hadResolvedType && text.Length < 3)
                app.ApplicationTypeQuickCodeChanged?.Invoke(text);
            else if (text.Length == 3 && text.All(char.IsDigit))
                app.ApplicationTypeQuickCodeChanged?.Invoke(text);
        }

        PropertyValue = text;
        WriteValue();
        OnControlValueChanged();
        await Task.CompletedTask;
    }

    private AppBO? GetApplication() => CurrentObject as AppBO;

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
            return;

        var popupWasOpen = ComponentModel.PopupVisible;
        ComponentModel.ObjectSpace = _objectSpace;
        ComponentModel.Value = Convert.ToString(PropertyValue) ?? string.Empty;
        ComponentModel.ReadOnly = !AllowEdit;
        ApplyLocalizedUiTexts(ComponentModel);

        if (popupWasOpen)
            ReloadPickerRows();
    }

    protected override object GetControlValueCore() => ComponentModel?.Value ?? string.Empty;

    protected override void ApplyReadOnly()
    {
        base.ApplyReadOnly();
        if (ComponentModel != null)
            ComponentModel.ReadOnly = !AllowEdit;
    }

    private static void Log(string message)
    {
        var line = $"{LogPrefix} {message}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
    }
}
