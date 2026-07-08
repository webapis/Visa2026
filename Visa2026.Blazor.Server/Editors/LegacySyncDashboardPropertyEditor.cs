using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Editors;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), LegacySyncDashboardEditorAliases.Dashboard, false)]
public class LegacySyncDashboardPropertyEditor : BlazorPropertyEditorBase
{
    public LegacySyncDashboardPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override LegacySyncDashboardModel ComponentModel => (LegacySyncDashboardModel)base.ComponentModel;

    protected override IComponentModel CreateComponentModel()
    {
        var model = new LegacySyncDashboardModel();
        model.RefreshRequested = EventCallback.Factory.Create(this, () => Task.CompletedTask);
        return model;
    }
}