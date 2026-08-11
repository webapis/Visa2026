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
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationWorkspaceEditorAliases.Workspace, false)]
public class ApplicationWorkspacePropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationWorkspaceQueryService? _queryService;
    private IApplicationWorkspacePersonUiActions? _personUiActions;

    public ApplicationWorkspacePropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationWorkspaceModel ComponentModel => (ApplicationWorkspaceModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationWorkspaceQueryService>();
        _personUiActions = application.ServiceProvider?.GetService<IApplicationWorkspacePersonUiActions>();
        if (_personUiActions != null)
            _personUiActions.WorkspaceChanged += OnWorkspaceChanged;
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationWorkspaceModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        LinkPersonRequested = EventCallback.Factory.Create(this, LinkPersonAsync),
        UnlinkPersonRequested = EventCallback.Factory.Create(this, UnlinkPersonAsync),
        OpenPersonDetailRequested = EventCallback.Factory.Create(this, OpenPersonDetailAsync),
        SelectPersonRowRequested = EventCallback.Factory.Create<int>(this, SelectPersonRow),
        OpenDocumentCopiesRequested = EventCallback.Factory.Create(this, OpenDocumentCopiesAsync),
        NewApplicationFromProfileRequested = EventCallback.Factory.Create<Guid>(this, NewApplicationFromProfileAsync),
        OpenProfileConfigRequested = EventCallback.Factory.Create<Guid>(this, OpenProfileConfigAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ApplyApplicationIdFromContext();

        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        var model = ComponentModel;
        if (model == null || model.IsLoading)
            return;

        if (model.Snapshot == null || model.Snapshot.ApplicationId != applicationId)
            _ = LoadAsync();
    }

    private void OnWorkspaceChanged()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = -1;
        UpdateActionState(model);
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
            var applicationId = ResolveApplicationId();
            if (_application == null || applicationId == Guid.Empty)
            {
                model.Snapshot = new ApplicationWorkspaceSnapshot { ApplicationId = applicationId };
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(Application));
            var service = _queryService
                ?? _application.ServiceProvider?.GetService<IApplicationWorkspaceQueryService>();

            model.Snapshot = service != null
                ? service.Load(objectSpace, applicationId)
                : new ApplicationWorkspaceMockQueryService().Load(objectSpace, applicationId);

            if (model.SelectedPersonRowIndex >= (model.Snapshot?.Tabs
                    .FirstOrDefault(t => t.Key == "person")?.RowPersonIds.Count ?? 0))
            {
                model.SelectedPersonRowIndex = -1;
            }
        }
        finally
        {
            model.IsLoading = false;
            UpdateActionState(model);
        }
    }

    private void UpdateActionState(ApplicationWorkspaceModel model)
    {
        var applicationId = ResolveApplicationId();
        var canLink = applicationId != Guid.Empty && _application?.MainWindow != null;
        model.CanLinkPerson = canLink;
        model.CanUnlinkPerson = canLink;

        var personTab = model.Snapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        model.CanOpenPersonDetail = personTab != null
            && model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowPersonIds.Count;
        model.CanOpenDocumentCopies = personTab != null
            && model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowApplicationPersonIds.Count;
    }

    private Task LinkPersonAsync()
    {
        if (_application?.MainWindow == null)
            return Task.CompletedTask;

        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspacePersonLinkHelper.ShowLinkPersonPicker(
            _application,
            _application.MainWindow,
            applicationId,
            OnWorkspaceChanged);

        return Task.CompletedTask;
    }

    private Task UnlinkPersonAsync()
    {
        if (_application?.MainWindow == null)
            return Task.CompletedTask;

        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspacePersonLinkHelper.ShowUnlinkPersonPicker(
            _application,
            _application.MainWindow,
            applicationId,
            OnWorkspaceChanged);

        return Task.CompletedTask;
    }

    private void SelectPersonRow(int rowIndex)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = model.SelectedPersonRowIndex == rowIndex ? -1 : rowIndex;
        UpdateActionState(model);
    }

    private async Task OpenPersonDetailAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        var personTab = model.Snapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        if (personTab == null
            || model.SelectedPersonRowIndex < 0
            || model.SelectedPersonRowIndex >= personTab.RowPersonIds.Count)
        {
            return;
        }

        var personId = personTab.RowPersonIds[model.SelectedPersonRowIndex];
        if (personId == Guid.Empty)
            return;

        PersonDetailOpenHelper.TryShowDetailView(
            _application,
            _application.MainWindow,
            personId);

        await Task.Delay(16);
    }

    private Task OpenDocumentCopiesAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return Task.CompletedTask;

        var personTab = model.Snapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        if (personTab == null
            || model.SelectedPersonRowIndex < 0
            || model.SelectedPersonRowIndex >= personTab.RowApplicationPersonIds.Count)
        {
            return Task.CompletedTask;
        }

        var applicationPersonId = personTab.RowApplicationPersonIds[model.SelectedPersonRowIndex];
        var applicationId = ResolveApplicationId();
        if (applicationPersonId == Guid.Empty || applicationId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceDocumentCopiesOpenHelper.TryOpen(
            _application,
            applicationId,
            [applicationPersonId],
            VisaPreviewSlotViewHelper.ResolveOwnerViewId(View));

        return Task.CompletedTask;
    }

    private Task NewApplicationFromProfileAsync(Guid profileId)
    {
        if (_application == null || profileId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceProfileRailHelper.TryCreateNewApplicationFromProfile(
            _application,
            profileId,
            ResolveApplicationId(),
            _application.MainWindow,
            out _);

        return Task.CompletedTask;
    }

    private Task OpenProfileConfigAsync(Guid profileId)
    {
        if (_application == null || profileId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceProfileRailHelper.TryOpenProfileConfiguration(
            _application,
            profileId,
            _application.MainWindow);

        return Task.CompletedTask;
    }

    private void ApplyApplicationIdFromContext()
    {
        if (CurrentObject is not ApplicationWorkspaceHost host || host.ApplicationId != Guid.Empty)
            return;

        var pending = _application != null
            ? ApplicationWorkspacePendingOpenGate.Get(_application)
            : Guid.Empty;
        if (pending != Guid.Empty)
            host.ApplicationId = pending;
    }

    private Guid ResolveApplicationId()
    {
        ApplyApplicationIdFromContext();
        if (CurrentObject is ApplicationWorkspaceHost host && host.ApplicationId != Guid.Empty)
            return host.ApplicationId;

        return _application != null
            ? ApplicationWorkspacePendingOpenGate.Get(_application)
            : Guid.Empty;
    }
}
