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
using Visa2026.Module.Services.ApplicationResultsDesk;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationResultsDeskEditorAliases.Desk, false)]
public class ApplicationResultsDeskPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private int _generation;
    private ApplicationResultsDeskRequest _request = new();

    public ApplicationResultsDeskPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override ApplicationResultsDeskModel ComponentModel => (ApplicationResultsDeskModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new ApplicationResultsDeskModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, () => LoadAsync()),
        LoadRequested = EventCallback.Factory.Create<ApplicationResultsDeskRequest>(this, request => LoadAsync(request)),
        OpenCaseRequested = EventCallback.Factory.Create<Guid>(this, OpenCase),
        OpenHeaderRequested = EventCallback.Factory.Create<ApplicationResultsDeskHeaderOpen>(this, OpenHeaderAsync),
        SaveLetterRequested = EventCallback.Factory.Create<ApplicationResultsDeskLetterSave>(this, SaveLetterAsync),
        OpenPersonRequested = EventCallback.Factory.Create<ApplicationResultsDeskPersonOpen>(this, OpenPerson),
        SaveDetailRequested = EventCallback.Factory.Create<ApplicationResultsDeskDetailSave>(this, SaveDetailAsync),
        SaveHeaderRequested = EventCallback.Factory.Create<ApplicationResultsDeskHeaderSave>(this, SaveHeaderAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        _ = LoadAsync();
    }

    private Task LoadAsync() => LoadAsync(_request);

    private async Task LoadAsync(ApplicationResultsDeskRequest request)
    {
        var generation = ++_generation;
        var model = ComponentModel;
        if (model == null)
            return;

        _request = request ?? new ApplicationResultsDeskRequest();
        model.IsLoading = model.Page == null;
        model.IsBusy = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Yield();
        if (generation != _generation || ComponentModel == null || _application == null)
        {
            if (generation == _generation)
                model.IsBusy = false;
            return;
        }

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var page = ApplicationResultsDeskService.Load(objectSpace, _request);
            if (generation != _generation)
                return;

            model.Page = page;
            if (!string.IsNullOrWhiteSpace(page.Notice))
            {
                model.StatusMessage = page.Notice;
                model.IsStatusError = true;
            }
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            if (generation == _generation)
            {
                model.IsLoading = false;
                model.IsBusy = false;
            }
        }
    }

    private async Task SaveLetterAsync(ApplicationResultsDeskLetterSave save)
    {
        if (_application == null || save == null)
            return;

        var model = ComponentModel;
        if (model != null)
            model.IsBusy = true;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(WorkPermit));
            var error = ApplicationResultsDeskService.SaveWorkPermitLetter(objectSpace, save);
            if (model != null)
            {
                model.StatusMessage = error ?? "Work permit letter saved.";
                model.IsStatusError = error != null;
            }
        }
        catch (Exception ex)
        {
            if (model != null)
            {
                model.StatusMessage = ex.Message;
                model.IsStatusError = true;
            }
        }

        _request.PeopleInstanceId = save.InstanceId;
        _request.PeopleKind = ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit;
        await LoadAsync(_request);
    }

    private Task OpenHeaderAsync(ApplicationResultsDeskHeaderOpen open)
    {
        if (open == null || open.InstanceId == Guid.Empty)
            return Task.CompletedTask;

        _request.HeaderKind = open.Kind ?? string.Empty;
        _request.PeopleInstanceId = open.InstanceId;
        _request.PeopleKind = string.Empty;
        _request.DetailOpen = false;
        _request.DetailPersonId = Guid.Empty;
        return LoadAsync(_request);
    }

    private void OpenCase(Guid instanceId)
    {
        if (_application == null || instanceId == Guid.Empty)
            return;

        var view = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(_application, instanceId);
        if (view == null || _application.MainWindow == null)
            return;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(view) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));
    }

    private Task OpenPerson(ApplicationResultsDeskPersonOpen open)
    {
        if (open == null || open.InstanceId == Guid.Empty)
            return Task.CompletedTask;

        _request.HeaderKind = string.Empty;
        _request.PeopleInstanceId = open.InstanceId;
        _request.PeopleKind = open.Kind ?? string.Empty;
        _request.DetailPersonId = open.PersonId;
        _request.DetailOpen = true;
        return LoadAsync(_request);
    }

    private async Task SaveDetailAsync(ApplicationResultsDeskDetailSave save)
    {
        if (_application == null || save == null)
            return;

        var model = ComponentModel;
        if (model != null)
            model.IsBusy = true;

        string error = null;
        try
        {
            var type = string.Equals(save.Kind, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase)
                ? typeof(Visa)
                : typeof(WorkPermitItem);
            using var objectSpace = _application.CreateObjectSpace(type);
            error = ApplicationResultsDeskService.SaveDetail(objectSpace, save);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        if (model != null)
        {
            model.IsBusy = false;
            if (error != null)
            {
                model.StatusMessage = error;
                model.IsStatusError = true;
                return;
            }

            model.StatusMessage = "Saved.";
            model.IsStatusError = false;
        }

        _request.DetailOpen = false;
        _request.DetailPersonId = Guid.Empty;
        await LoadAsync(_request);
    }

    private async Task SaveHeaderAsync(ApplicationResultsDeskHeaderSave save)
    {
        if (_application == null || save == null)
            return;

        var model = ComponentModel;
        if (model != null)
            model.IsBusy = true;

        string error = null;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            error = ApplicationResultsDeskService.SaveHeader(objectSpace, save);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        if (model != null)
        {
            model.IsBusy = false;
            if (error != null)
            {
                model.StatusMessage = error;
                model.IsStatusError = true;
                return;
            }

            model.StatusMessage = "Saved.";
            model.IsStatusError = false;
        }

        _request.HeaderKind = string.Empty;
        _request.PeopleInstanceId = Guid.Empty;
        await LoadAsync(_request);
    }
}
