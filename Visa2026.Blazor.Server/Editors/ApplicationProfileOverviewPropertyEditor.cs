#nullable enable
using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileOverview;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationProfileOverviewEditorAliases.Overview, false)]
public class ApplicationProfileOverviewPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationProfileOverviewQueryService? _queryService;

    public ApplicationProfileOverviewPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationProfileOverviewModel ComponentModel => (ApplicationProfileOverviewModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationProfileOverviewQueryService>();
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationProfileOverviewModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        ConfigureRequested = EventCallback.Factory.Create(this, ConfigureAsync),
        OpenInstanceRequested = EventCallback.Factory.Create<Guid>(this, OpenLinkedInstanceAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.IsLoading = true;
        await Task.Delay(16);

        try
        {
            var profileId = ResolveProfileId();
            var service = _queryService
                ?? _application?.ServiceProvider?.GetService<IApplicationProfileOverviewQueryService>()
                ?? new ApplicationProfileOverviewQueryService();

            using var objectSpace = _application?.CreateObjectSpace(typeof(ApplicationProfile));
            model.Snapshot = service.Load(profileId, objectSpace);
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private Guid ResolveProfileId()
    {
        if (CurrentObject is ApplicationProfileOverviewHost host && host.ApplicationProfileId != Guid.Empty)
            return host.ApplicationProfileId;

        var pending = _application?.ServiceProvider?.GetService<IApplicationProfileOverviewPendingOpen>();
        if (pending?.ApplicationProfileId is Guid id && id != Guid.Empty)
            return id;

        return ApplicationProfileOverviewPendingOpenGate.Get(_application!);
    }

    private Task ConfigureAsync()
    {
        if (_application == null)
            return Task.CompletedTask;

        var profileId = ResolveProfileId();
        if (profileId == Guid.Empty)
            return Task.CompletedTask;

        var wizardView = ApplicationProfileWizardOpenHelper.CreateWizardView(_application, profileId);
        if (wizardView == null)
            return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }

    private Task OpenLinkedInstanceAsync(Guid instanceId)
    {
        if (_application == null || instanceId == Guid.Empty)
            return Task.CompletedTask;

        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(_application, instanceId);
        if (workspaceView == null)
            return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(workspaceView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }
}
