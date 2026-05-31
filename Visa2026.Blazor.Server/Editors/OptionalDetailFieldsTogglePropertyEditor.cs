using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Controllers;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(bool), OptionalDetailFieldsEditorAliases.Toggle, false)]
public sealed class OptionalDetailFieldsTogglePropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private IObjectSpace _objectSpace;

    public OptionalDetailFieldsTogglePropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override OptionalDetailFieldsToggleModel ComponentModel =>
        (OptionalDetailFieldsToggleModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _objectSpace = objectSpace;
    }

    protected override IComponentModel CreateComponentModel()
    {
        var model = new OptionalDetailFieldsToggleModel();
        model.OnToggleAsync = EventCallback.Factory.Create(this, OnToggleAsync);
        return model;
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
        {
            return;
        }

        bool value = CurrentObject is IOptionalDetailFields optional && optional.ShowOptionalFields;
        ComponentModel.Value = value;
        ComponentModel.ToolTip = GetToolTip(value);
    }

    protected override object GetControlValueCore() =>
        CurrentObject is IOptionalDetailFields optional && optional.ShowOptionalFields;

    private async Task OnToggleAsync()
    {
        bool newValue = CurrentObject is not IOptionalDetailFields optional || !optional.ShowOptionalFields;

        PropertyValue = newValue;
        WriteValue();
        OnControlValueChanged();
        _objectSpace?.SetModified(CurrentObject);

        if (ComponentModel != null)
        {
            ComponentModel.Value = newValue;
            ComponentModel.ToolTip = GetToolTip(newValue);
        }

        if (View is DetailView detailView)
        {
            OptionalDetailFieldsController.NotifyShowOptionalFieldsChanged(detailView);
        }

        await Task.CompletedTask;
    }

    private static string GetToolTip(bool showOptionalFields) =>
        VisaUiMessages.Get(showOptionalFields
            ? "Action.ToggleOptionalFields.Hide"
            : "Action.ToggleOptionalFields.Show");
}
