#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.PersonDossier;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), PersonDossierEditorAliases.Dossier, false)]
public class PersonDossierPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IVisaPreviewSlotService? _uploadSlotService;
    private string? _uploadOccupantKey;

    public PersonDossierPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override PersonDossierModel ComponentModel => (PersonDossierModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new PersonDossierModel
    {
        IsLoading = true,
        LoadingProgressPercent = 0,
        LoadingMessage = VisaUiMessages.Get("PersonDossier.Chrome.LoadingPreparing"),
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        OpenCopiesRequested = EventCallback.Factory.Create(this, OpenCopies),
        ExportRequested = EventCallback.Factory.Create(this, QueueExport),
        OpenApplicationRequested = EventCallback.Factory.Create<Guid>(this, OpenApplicationWorkspace),
        PreviewRecordRequested = EventCallback.Factory.Create<PersonDossierRecord>(this, PreviewRecordAsync),
        UploadRecordRequested = EventCallback.Factory.Create<PersonDossierRecord>(this, UploadRecordAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ApplyPersonIdFromContext();

        var personId = ResolvePersonId();
        if (personId == Guid.Empty)
            return;

        var model = ComponentModel;
        if (model == null)
            return;

        if (model.IsLoading)
            return;

        if (model.Snapshot == null || model.Snapshot.PersonId != personId)
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;
        model.IsLoading = true;
        SetLoadStage(model, 5, "PersonDossier.Chrome.LoadingPreparing");
        // Let Blazor paint the progress panel before synchronous DB work (Yield alone is not enough).
        await Task.Delay(16);

        try
        {
            SetLoadStage(model, 20, "PersonDossier.Chrome.LoadingPerson");
            await Task.Delay(1);

            // The snapshot holds only scalars, so the object space can be released immediately.
            using var objectSpace = _application?.CreateObjectSpace(typeof(Person));
            var personId = ResolvePersonId();
            var person = objectSpace == null || personId == Guid.Empty
                ? null
                : objectSpace.GetObjectByKey<Person>(personId);

            SetLoadStage(model, 45, "PersonDossier.Chrome.LoadingIdentity");
            await Task.Delay(1);

            SetLoadStage(model, 70, "PersonDossier.Chrome.LoadingSections");
            await Task.Yield();

            model.Snapshot = objectSpace == null
                ? new PersonDossierSnapshot()
                : PersonDossierResolver.Resolve(objectSpace, person);

            SetLoadStage(model, 95, "PersonDossier.Chrome.LoadingFinishing");
            await Task.Yield();
        }
        finally
        {
            model.LoadingProgressPercent = 100;
            model.LoadingMessage = string.Empty;
            model.IsLoading = false;
        }
    }

    private static void SetLoadStage(PersonDossierModel model, int percent, string messageKey)
    {
        model.LoadingProgressPercent = percent;
        model.LoadingMessage = VisaUiMessages.Get(messageKey);
    }

    private void OpenCopies()
    {
        var personId = ResolvePersonId();
        if (personId == Guid.Empty)
            return;

        var slotService = _application?.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
            return;

        // Owner is the dossier view, otherwise the slot closes as soon as the officer got here.
        slotService.OpenPersonDocumentCopiesAsync(
            new PersonDocumentCopiesSlotRequest { PersonIds = new[] { personId } },
            PersonDossierViewIds.DetailView);
    }

    private Task PreviewRecordAsync(PersonDossierRecord record)
    {
        if (record == null || !record.HasPreview)
            return Task.CompletedTask;

        var slotService = _application?.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
            return Task.CompletedTask;

        if (record.HasHeaderPreview)
        {
            return slotService.OpenHeaderDocumentCopiesAsync(
                new HeaderDocumentCopiesSlotRequest
                {
                    Family = record.PreviewFamily!.Value,
                    ParentId = record.PreviewParentId!.Value,
                    FocusDisplayName = record.Cells.Count > 0 ? record.Cells[0] : null,
                    OpenPreviewOnly = true,
                },
                PersonDossierViewIds.DetailView);
        }

        return slotService.OpenPersonDocumentCopiesAsync(
            new PersonDocumentCopiesSlotRequest
            {
                PersonIds = new[] { record.PersonCopyPersonId!.Value },
                FocusRecordKey = record.PersonCopyRecordKey,
                FocusDisplayName = string.IsNullOrWhiteSpace(record.PersonCopyDisplayName)
                    ? (record.Cells.Count > 0 ? record.Cells[0] : null)
                    : record.PersonCopyDisplayName,
                OpenPreviewOnly = true,
            },
            PersonDossierViewIds.DetailView);
    }

    private async Task UploadRecordAsync(PersonDossierRecord record)
    {
        if (_application == null || record == null || !record.CanUpload)
            return;

        if (record.CanUploadHeader)
        {
            var headerId = record.UploadHeaderId!.Value;
            var kind = record.UploadIssuedKind ?? IssueIssuedHeaderKind.Invitation;
            var slotService = _application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
            if (slotService != null
                && record.UploadApplicationProfileInstanceId is Guid appId
                && appId != Guid.Empty)
            {
                var request = new IssueIssuedHeaderSlotRequest
                {
                    ApplicationProfileInstanceId = appId,
                    Kind = kind,
                    CatalogKey = IssueIssuedHeaderComposeService.CatalogKeyFor(kind),
                    ExistingHeaderId = headerId,
                };
                WatchUploadSlot(slotService, VisaPreviewSlotOccupantKeys.ForIssueIssuedHeader(request));
                await slotService.OpenIssueIssuedHeaderAsync(request, PersonDossierViewIds.DetailView);
                return;
            }

            // Issued letter with no case: the header DetailView Documents tab is the upload surface.
            var headerType = kind == IssueIssuedHeaderKind.WorkPermit ? typeof(WorkPermit) : typeof(Invitation);
            OpenDetail(headerType, headerId);
            return;
        }

        if (record.CanUploadDetail)
            OpenDetail(record.UploadDetailType!, record.UploadDetailId!.Value);
    }

    private void OpenDetail(Type objectType, Guid objectId)
    {
        if (_application == null || objectId == Guid.Empty)
            return;

        var objectSpace = _application.CreateObjectSpace(objectType);
        var target = objectSpace.GetObjectByKey(objectType, objectId);
        if (target == null)
        {
            objectSpace.Dispose();
            return;
        }

        var detailView = _application.CreateDetailView(objectSpace, target);
        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));
    }

    private void WatchUploadSlot(IVisaPreviewSlotService slotService, string occupantKey)
    {
        UnwatchUploadSlot();
        _uploadSlotService = slotService;
        _uploadOccupantKey = occupantKey;
        slotService.StateChanged += OnUploadSlotStateChanged;
    }

    private void UnwatchUploadSlot()
    {
        if (_uploadSlotService != null)
            _uploadSlotService.StateChanged -= OnUploadSlotStateChanged;
        _uploadSlotService = null;
        _uploadOccupantKey = null;
    }

    private void OnUploadSlotStateChanged()
    {
        var service = _uploadSlotService;
        if (service == null
            || string.Equals(service.State.OccupantKey, _uploadOccupantKey, StringComparison.Ordinal))
            return;

        // Upload panel closed or replaced — reload so the Copy column shows Preview.
        UnwatchUploadSlot();
        _ = LoadAsync();
    }

    public override void BreakLinksToControl(bool unwireEventsOnly)
    {
        UnwatchUploadSlot();
        base.BreakLinksToControl(unwireEventsOnly);
    }

    private void OpenApplicationWorkspace(Guid instanceId)
    {
        if (_application == null || instanceId == Guid.Empty)
            return;

        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(_application, instanceId);
        if (workspaceView == null)
            return;

        // New tab — keep the dossier open beside the case workspace.
        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(workspaceView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));
    }

    private void QueueExport()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.ExportMessage = null;

        var personId = ResolvePersonId();
        var services = _application?.ServiceProvider;
        if (personId == Guid.Empty || services == null)
        {
            model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.ErrorNoPerson");
            return;
        }

        var enqueueService = services.GetService<PersonExportBatchEnqueueService>();
        if (enqueueService == null)
        {
            model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.ErrorFailed");
            return;
        }

        using var objectSpace = _application!.CreateObjectSpace(typeof(Person));
        var person = objectSpace.GetObjectByKey<Person>(personId);
        string requestedBy = SecuritySystem.CurrentUserName ?? string.Empty;

        if (!enqueueService.TryEnqueuePerson(
                objectSpace,
                person,
                requestedBy,
                CultureInfo.CurrentUICulture.Name,
                out var result,
                out var errorMessageKey)
            || result == null)
        {
            model.ExportMessage = VisaUiMessages.Get(errorMessageKey ?? "PersonDossier.Export.ErrorFailed");
            return;
        }

        // Hand the id to the global toast, which lives outside this component tree.
        services.GetService<IPersonExportBatchTrackNotifier>()?.TrackQueuedBatch(result.BatchId, requestedBy);

        model.IsExportQueued = true;
        model.ExportMessage = VisaUiMessages.Get("PersonDossier.Export.Queued");
    }

    private void ApplyPersonIdFromContext()
    {
        if (CurrentObject is not PersonDossierHost host || host.PersonId != Guid.Empty)
            return;

        var pending = _application != null
            ? PersonDossierPendingOpenGate.Get(_application)
            : Guid.Empty;
        if (pending != Guid.Empty)
            host.PersonId = pending;
    }

    private Guid ResolvePersonId()
    {
        ApplyPersonIdFromContext();
        if (CurrentObject is PersonDossierHost host && host.PersonId != Guid.Empty)
            return host.PersonId;

        return _application != null
            ? PersonDossierPendingOpenGate.Get(_application)
            : Guid.Empty;
    }
}