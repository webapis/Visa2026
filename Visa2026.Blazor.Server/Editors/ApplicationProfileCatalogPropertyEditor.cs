#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationProfileCatalogEditorAliases.Catalog, false)]
public class ApplicationProfileCatalogPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationProfileCatalogQueryService? _queryService;
    private IApplicationProfileOverviewQueryService? _overviewQueryService;
    private IReadOnlyList<ApplicationProfileCatalogRow> _allRows = Array.Empty<ApplicationProfileCatalogRow>();

    public ApplicationProfileCatalogPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationProfileCatalogModel ComponentModel => (ApplicationProfileCatalogModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationProfileCatalogQueryService>();
        _overviewQueryService = application.ServiceProvider?.GetService<IApplicationProfileOverviewQueryService>();
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationProfileCatalogModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        NewProfileRequested = EventCallback.Factory.Create(this, NewProfileAsync),
        SelectProfileRequested = EventCallback.Factory.Create<Guid>(this, SelectProfileAsync),
        ConfigureRequested = EventCallback.Factory.Create(this, ConfigureSelectedAsync),
        SearchTextChanged = EventCallback.Factory.Create<string>(this, OnSearchTextChanged),
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
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Delay(16);

        try
        {
            if (_application == null)
            {
                model.StatusMessage = "Application host is not ready.";
                model.IsStatusError = true;
                return;
            }

            var queryService = _queryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfileCatalogQueryService>();
            if (queryService == null)
            {
                model.StatusMessage = "Application Profile catalog service is not registered.";
                model.IsStatusError = true;
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
            _allRows = queryService.GetProfiles(objectSpace);
            ApplyFilter(model);

            var keepSelection = model.SelectedProfileId != Guid.Empty
                && _allRows.Any(r => r.ProfileId == model.SelectedProfileId);
            var selectId = keepSelection
                ? model.SelectedProfileId
                : (_allRows.FirstOrDefault()?.ProfileId ?? Guid.Empty);

            if (selectId != Guid.Empty)
                await SelectProfileAsync(selectId);
            else
            {
                model.SelectedProfileId = Guid.Empty;
                model.OverviewSnapshot = null;
            }
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private void OnSearchTextChanged(string text)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SearchText = text ?? string.Empty;
        ApplyFilter(model);
    }

    private void ApplyFilter(ApplicationProfileCatalogModel model)
    {
        var q = (model.SearchText ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            model.Rows = _allRows;
            return;
        }

        model.Rows = _allRows
            .Where(r =>
                (r.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.SelectionCode?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.RailLabel?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    private async Task SelectProfileAsync(Guid profileId)
    {
        var model = ComponentModel;
        if (model == null || _application == null || profileId == Guid.Empty)
            return;

        model.SelectedProfileId = profileId;
        model.IsOverviewLoading = true;
        await Task.Delay(16);

        try
        {
            var overviewService = _overviewQueryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfileOverviewQueryService>()
                ?? new ApplicationProfileOverviewMockQueryService();

            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
            model.OverviewSnapshot = overviewService.Load(profileId, objectSpace);
        }
        finally
        {
            model.IsOverviewLoading = false;
        }
    }

    private Task NewProfileAsync()
    {
        if (_application == null)
            return Task.CompletedTask;

        var wizardView = ApplicationProfileCatalogCreateHelper.CreateNewProfileAndOpenWizard(_application);
        if (wizardView == null)
            return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }

    private Task ConfigureSelectedAsync()
    {
        var model = ComponentModel;
        if (_application == null || model == null || model.SelectedProfileId == Guid.Empty)
            return Task.CompletedTask;

        var wizardView = ApplicationProfileWizardOpenHelper.CreateWizardView(_application, model.SelectedProfileId);
        if (wizardView == null)
            return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }
}