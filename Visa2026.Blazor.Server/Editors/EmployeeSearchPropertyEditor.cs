#nullable enable
using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.EmployeeSearch;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), EmployeeSearchEditorAliases.Search, false)]
public class EmployeeSearchPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private int _generation;

    public EmployeeSearchPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override EmployeeSearchModel ComponentModel => (EmployeeSearchModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new EmployeeSearchModel
    {
        IsLoading = true,
        Rows = Array.Empty<EmployeeSearchRow>(),
        Options = new EmployeeSearchOptions(),
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadOptionsAsync),
        SearchRequested = EventCallback.Factory.Create<EmployeeSearchCriteria>(this, SearchAsync),
        EditCriteriaRequested = EventCallback.Factory.Create(this, EditCriteria),
        OpenDossierRequested = EventCallback.Factory.Create<Guid>(this, OpenDossier),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        _ = LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var generation = ++_generation;
        var model = ComponentModel;
        if (model == null)
            return;

        model.IsLoading = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Yield();
        if (generation != _generation || ComponentModel == null || _application == null)
        {
            if (model.IsLoading && generation == _generation)
                model.IsLoading = false;
            return;
        }

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(Person));
            var options = EmployeeSearchService.LoadOptions(objectSpace);
            if (generation != _generation)
                return;

            model.Options = options;
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            if (generation == _generation)
                model.IsLoading = false;
        }
    }

    private async Task SearchAsync(EmployeeSearchCriteria criteria)
    {
        var generation = ++_generation;
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        model.IsBusy = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Yield();
        if (generation != _generation)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(Person));
            var result = EmployeeSearchService.Search(objectSpace, criteria);
            if (generation != _generation)
                return;

            model.Rows = result.Rows;
            model.Total = result.Total;
            model.Truncated = result.Truncated;
            model.ShowResults = true;
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
            model.ShowResults = false;
        }
        finally
        {
            if (generation == _generation)
                model.IsBusy = false;
        }
    }

    private Task EditCriteria()
    {
        if (ComponentModel != null)
            ComponentModel.ShowResults = false;
        return Task.CompletedTask;
    }

    private void OpenDossier(Guid personId)
    {
        if (_application == null || personId == Guid.Empty)
            return;

        var detailView = PersonDossierOpenHelper.CreateDossierView(_application, personId);
        if (detailView == null)
            return;

        var window = _application.MainWindow;
        if (window == null)
            return;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(window, null));
    }
}
