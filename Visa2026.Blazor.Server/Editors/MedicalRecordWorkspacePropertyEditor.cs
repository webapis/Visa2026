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
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.MedicalRecordWorkspace;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.MedicalRecordWorkspace;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), MedicalRecordWorkspaceEditorAliases.Workspace, false)]
public class MedicalRecordWorkspacePropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private int _viewGeneration;

    public MedicalRecordWorkspacePropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override MedicalRecordWorkspaceModel ComponentModel => (MedicalRecordWorkspaceModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new MedicalRecordWorkspaceModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadListAsync),
        SearchRequested = EventCallback.Factory.Create<string>(this, SearchAsync),
        OpenRequested = EventCallback.Factory.Create<Guid>(this, OpenAsync),
        NewRequested = EventCallback.Factory.Create(this, NewAsync),
        PersonSearchRequested = EventCallback.Factory.Create<string>(this, SearchPeopleAsync),
        SaveRequested = EventCallback.Factory.Create<MedicalRecordSaveRequest>(this, SaveAsync),
        PreviewRequested = EventCallback.Factory.Create<MedicalRecordPreviewRequest>(this, PreviewAsync),
        CancelRequested = EventCallback.Factory.Create(this, LoadListAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        _ = LoadListAsync();
    }

    private Task SearchAsync(string search) => LoadListAsync(search);

    private async Task LoadListAsync() => await LoadListAsync(null);

    private async Task LoadListAsync(string? search)
    {
        var generation = ++_viewGeneration;
        var model = ComponentModel;
        if (model == null)
            return;

        model.IsLoading = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Delay(16);
        if (generation != _viewGeneration || ComponentModel == null)
            return;

        try
        {
            if (_application == null)
            {
                model.StatusMessage = "Medical records are not ready.";
                model.IsStatusError = true;
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(MedicalRecord));
            var list = MedicalRecordWorkspaceService.List(objectSpace, search ?? string.Empty);
            if (generation != _viewGeneration)
                return;

            model.Rows = list.Rows;
            model.ListTruncated = list.Truncated;
            model.ValidityOptions = MedicalRecordWorkspaceService.ListValidity(objectSpace);
            model.PersonHits = Array.Empty<MedicalRecordPersonHit>();
            model.Mode = MedicalRecordWorkspaceMode.List;
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            if (generation == _viewGeneration)
                model.IsLoading = false;
        }
    }

    private Task NewAsync()
    {
        _viewGeneration++;
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.Mode = MedicalRecordWorkspaceMode.Detail;
        model.Detail = new MedicalRecordDetailDto { IssueDate = DateTime.Today };
        model.DetailRevision++;
        model.PersonHits = Array.Empty<MedicalRecordPersonHit>();
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.IsLoading = false;
        if (model.ValidityOptions.Count == 0 && _application != null)
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ValidityDuration));
            model.ValidityOptions = MedicalRecordWorkspaceService.ListValidity(objectSpace);
        }
        return Task.CompletedTask;
    }

    private Task OpenAsync(Guid id) => LoadDetailAsync(id, null);

    private async Task LoadDetailAsync(Guid id, string? statusMessage)
    {
        var generation = ++_viewGeneration;
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        model.IsBusy = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(MedicalRecord));
            var detail = MedicalRecordWorkspaceService.Load(objectSpace, id);
            if (generation != _viewGeneration)
                return;

            if (detail == null)
            {
                model.StatusMessage = "The medical record could not be found.";
                model.IsStatusError = true;
                model.Mode = MedicalRecordWorkspaceMode.List;
                return;
            }

            if (model.ValidityOptions.Count == 0)
                model.ValidityOptions = MedicalRecordWorkspaceService.ListValidity(objectSpace);

            model.Detail = detail;
            model.DetailRevision++;
            model.PersonHits = Array.Empty<MedicalRecordPersonHit>();
            model.Mode = MedicalRecordWorkspaceMode.Detail;
            model.StatusMessage = statusMessage;
            model.IsStatusError = false;
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            if (generation == _viewGeneration)
                model.IsBusy = false;
        }
    }

    private async Task SearchPeopleAsync(string search)
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        model.IsBusy = true;
        model.PersonHits = Array.Empty<MedicalRecordPersonHit>();
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(Person));
            model.PersonHits = MedicalRecordWorkspaceService.SearchPeople(objectSpace, search);
            if (model.PersonHits.Count == 0)
            {
                model.StatusMessage = "No person matches that search.";
                model.IsStatusError = true;
            }
            else
            {
                model.StatusMessage = null;
                model.IsStatusError = false;
            }
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            model.IsBusy = false;
        }
    }

    private async Task SaveAsync(MedicalRecordSaveRequest request)
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        model.IsBusy = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(MedicalRecord));
            if (!MedicalRecordWorkspaceService.TrySave(objectSpace, request, out var id, out var error))
            {
                model.StatusMessage = error;
                model.IsStatusError = true;
                return;
            }

            if (request.CloseAfterSave)
            {
                await LoadListAsync();
                if (ComponentModel != null && string.IsNullOrWhiteSpace(ComponentModel.StatusMessage))
                {
                    ComponentModel.StatusMessage = "Saved.";
                    ComponentModel.IsStatusError = false;
                }
                return;
            }

            await LoadDetailAsync(id, "Saved.");
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            if (ComponentModel != null)
                ComponentModel.IsBusy = false;
        }
    }

    private async Task PreviewAsync(MedicalRecordPreviewRequest request)
    {
        if (request == null || _application == null)
            return;

        var slot = _application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slot == null)
            return;

        var ownerViewId = MedicalRecordWorkspaceViewIds.DetailView;
        if (request.InlineContent is { Length: > 0 })
        {
            await slot.OpenPersonDocumentCopiesAsync(new PersonDocumentCopiesSlotRequest
            {
                PersonIds = request.PersonId == Guid.Empty ? Array.Empty<Guid>() : new[] { request.PersonId },
                OpenPreviewOnly = true,
                AllowEditFiles = false,
                FocusDisplayName = request.DisplayName,
                FocusRecordKey = request.RecordId is Guid recordId && recordId != Guid.Empty
                    ? $"MedicalRecord:{recordId:N}"
                    : null,
                InlineFiles = new[]
                {
                    new ProgressLetterInlineFile
                    {
                        FileName = string.IsNullOrWhiteSpace(request.DisplayName) ? "scan" : request.DisplayName,
                        Content = request.InlineContent
                    }
                }
            }, ownerViewId);
            return;
        }

        if (request.FileId != Guid.Empty)
        {
            await slot.OpenFileAsync(FileDataPreviewSource.Key, request.FileId, ownerViewId);
            return;
        }

        if (request.RecordId is Guid savedId && savedId != Guid.Empty && request.PersonId != Guid.Empty)
        {
            await slot.OpenPersonDocumentCopiesAsync(new PersonDocumentCopiesSlotRequest
            {
                PersonIds = new[] { request.PersonId },
                OpenPreviewOnly = true,
                AllowEditFiles = false,
                FocusRecordKey = $"MedicalRecord:{savedId:N}",
                FocusDisplayName = request.DisplayName
            }, ownerViewId);
        }
    }
}
