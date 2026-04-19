using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Blazor.Server.Components;

namespace Visa2026.Blazor.Server.Editors
{
    public interface IModelStateDashboardViewItem : IModelViewItem { }

    [ViewItem(typeof(IModelStateDashboardViewItem))]
    public class StateDashboardViewItem : ViewItem
    {
        public StateDashboardViewItem(IModelStateDashboardViewItem model, Type objectType)
            : base(objectType, model?.Id) { }

        protected override object CreateControlCore() => new StateDashboardComponentAdapter();
    }

    public class StateDashboardComponentAdapter : IComponentAdapter
    {
        public IComponentModel ComponentModel => null;

        public RenderFragment ComponentContent => builder =>
        {
            builder.OpenComponent<StateDashboardComponent>(0);
            builder.CloseComponent();
        };

        public object GetValue() => null;
        public void SetValue(object value) { }
        public event EventHandler ValueChanged;
    }
}
