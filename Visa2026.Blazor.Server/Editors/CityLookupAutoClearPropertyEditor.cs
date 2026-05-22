using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(City), "CityLookupAutoClear", false)]
public class CityLookupAutoClearPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem {
    public CityLookupAutoClearPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }

    public override CityLookupAutoClearModel ComponentModel => (CityLookupAutoClearModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) {
        // currently not needed, but required by IComplexViewItem for future extensions
    }

    protected override IComponentModel CreateComponentModel() {
        var model = new CityLookupAutoClearModel();
        model.ValueExpression = () => model.Value;

        model.ValueChanged = EventCallback.Factory.Create<City>(this, value => {
            model.Value = value;
            OnControlValueChanged();
            WriteValue();
        });

        model.TextChanged = EventCallback.Factory.Create<string>(this, text => {
            model.Text = text;
            TryAutoClearMostlyUsedFilter(text);
        });

        return model;
    }

    protected override void ReadValueCore() {
        base.ReadValueCore();
        if (ComponentModel == null) {
            return;
        }
        ComponentModel.Value = (City)PropertyValue;
        ComponentModel.Data = GetAvailableCities();
        ComponentModel.ReadOnly = !AllowEdit;
    }

    protected override object GetControlValueCore() => ComponentModel.Value;

    protected override void ApplyReadOnly() {
        base.ApplyReadOnly();
        if (ComponentModel == null) {
            return;
        }
        ComponentModel.ReadOnly = !AllowEdit;
    }

    private void TryAutoClearMostlyUsedFilter(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return;
        }
    }

    private System.Collections.Generic.IEnumerable<City> GetAvailableCities() =>
        Array.Empty<City>();
}

