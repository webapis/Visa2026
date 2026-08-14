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
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonLink;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationWorkspaceEditorAliases.Workspace, false)]
public class ApplicationWorkspacePropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationWorkspaceQueryService? _queryService;
    private IApplicationWorkspacePersonUiActions? _personUiActions;
    private IOfficerShellCaseProgressService? _caseProgressService;
    private IApplicationProfileInstancePersonLinkQueryService? _personLinkQueryService;

    public ApplicationWorkspacePropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationWorkspaceModel ComponentModel => (ApplicationWorkspaceModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationWorkspaceQueryService>();
        _personUiActions = application.ServiceProvider?.GetService<IApplicationWorkspacePersonUiActions>();
        _caseProgressService = application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>();
        _personLinkQueryService = application.ServiceProvider?.GetService<IApplicationProfileInstancePersonLinkQueryService>();
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
        CaseTab = "overview",
        CaseTabChanged = EventCallback.Factory.Create<string>(this, OnCaseTabChanged),
        LinkedRecordTileClicked = EventCallback.Factory.Create<string>(this, OnLinkedRecordTileClicked),
        IssuedHeaderNewRequested = EventCallback.Factory.Create<string>(this, OnIssuedHeaderNewRequested),
        IssuedHeaderOpenRequested = EventCallback.Factory.Create<ApplicationWorkspaceIssuedHeaderOpenRequest>(this, OnIssuedHeaderOpenRequested),
        BackToListRequested = EventCallback.Factory.Create(this, BackToListAsync),
        OpenResminamalarRequested = EventCallback.Factory.Create(this, OpenResminamalarAsync),
        OpenPersonDetailByIndexRequested = EventCallback.Factory.Create<int>(this, OpenPersonDetailByIndexAsync),
        SaveProgressNotesRequested = EventCallback.Factory.Create<string>(this, SaveProgressNotesAsync),
        UploadMinistryLetterRequested = EventCallback.Factory.Create<OfficerShellCaseProgressFileUpload>(this, UploadMinistryLetterAsync),
        AdvanceProgressRequested = EventCallback.Factory.Create<OfficerShellCaseProgressAdvanceRequest>(this, AdvanceCaseProgressAsync),
        PersonLinkSearchRequested = EventCallback.Factory.Create<string>(this, SearchPersonLinkCandidatesAsync),
        LinkPersonFromPickerRequested = EventCallback.Factory.Create<Guid>(this, LinkPersonFromPickerAsync),
        ClosePersonLinkPickerRequested = EventCallback.Factory.Create(this, ClosePersonLinkPickerAsync),
        PersonLinkCandidates = Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>(),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ApplyApplicationProfileInstanceIdFromContext();

        var applicationId = ResolveApplicationProfileInstanceId();
        if (applicationId == Guid.Empty)
            return;

        var model = ComponentModel;
        if (model == null || model.IsLoading)
            return;

        if (model.Snapshot == null || model.Snapshot.ApplicationProfileInstanceId != applicationId)
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
            var applicationId = ResolveApplicationProfileInstanceId();
            if (_application == null || applicationId == Guid.Empty)
            {
                model.Snapshot = new ApplicationWorkspaceSnapshot { ApplicationProfileInstanceId = applicationId };
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
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
        var applicationId = ResolveApplicationProfileInstanceId();
        var rosterLocked = model.Snapshot?.CaseChrome.ResolvedLinksLocked == true;
        var canLink = applicationId != Guid.Empty && _application?.MainWindow != null && !rosterLocked;
        model.CanLinkPerson = canLink;
        model.CanUnlinkPerson = canLink;

        var personTab = model.Snapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        model.CanOpenPersonDetail = personTab != null
            && model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowPersonIds.Count;
        model.CanOpenDocumentCopies = personTab != null
            && model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowApplicationProfileInstancePersonIds.Count;
    }

    private async Task LinkPersonAsync()
    {
        var model = ComponentModel;
        if (model == null || ResolveApplicationProfileInstanceId() == Guid.Empty)
            return;

        model.ShowPersonLinkPicker = true;
        model.PersonLinkStatusMessage = null;
        model.PersonLinkStatusIsError = false;
        await SearchPersonLinkCandidatesAsync(string.Empty);
    }

    private Task UnlinkPersonAsync()
    {
        if (_application?.MainWindow == null)
            return Task.CompletedTask;

        var applicationId = ResolveApplicationProfileInstanceId();
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

        var applicationId = ResolveApplicationProfileInstanceId();
        var personTab = model.Snapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        if (personTab == null || personTab.RowApplicationProfileInstancePersonIds.Count == 0 || applicationId == Guid.Empty)
            return Task.CompletedTask;

        IReadOnlyList<Guid> rowIds;
        if (model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowApplicationProfileInstancePersonIds.Count)
        {
            rowIds = [personTab.RowApplicationProfileInstancePersonIds[model.SelectedPersonRowIndex]];
        }
        else
        {
            rowIds = personTab.RowApplicationProfileInstancePersonIds.Where(id => id != Guid.Empty).Distinct().ToList();
        }

        ApplicationWorkspaceDocumentCopiesOpenHelper.TryOpen(
            _application,
            applicationId,
            rowIds,
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
            ResolveApplicationProfileInstanceId(),
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

    private Task OnCaseTabChanged(string tab)
    {
        if (ComponentModel != null)
        {
            ComponentModel.CaseTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab;
            if (!string.Equals(ComponentModel.CaseTab, "people", StringComparison.OrdinalIgnoreCase))
                ComponentModel.PeopleLinkedRecordFocusKey = null;
        }

        return Task.CompletedTask;
    }

    private Task OnLinkedRecordTileClicked(string tabKey)
    {
        if (ComponentModel == null)
            return Task.CompletedTask;

        ComponentModel.CaseTab = "people";
        ComponentModel.PeopleLinkedRecordFocusKey = string.IsNullOrWhiteSpace(tabKey) ? null : tabKey.Trim();
        return Task.CompletedTask;
    }

    private Task OnIssuedHeaderNewRequested(string key)
    {
        var applicationId = ResolveApplicationProfileInstanceId();
        if (_application == null || applicationId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate(
            _application,
            _application.MainWindow,
            applicationId,
            key);
        return Task.CompletedTask;
    }

    private Task OnIssuedHeaderOpenRequested(ApplicationWorkspaceIssuedHeaderOpenRequest request)
    {
        if (_application == null || request == null || request.Id == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceIssuedHeaderOpenHelper.TryOpen(
            _application,
            _application.MainWindow,
            request.Key,
            request.Id);
        return Task.CompletedTask;
    }

    private Task BackToListAsync()
    {
        View?.Close();
        return Task.CompletedTask;
    }

    private Task OpenResminamalarAsync()
    {
        var applicationId = ResolveApplicationProfileInstanceId();
        if (_application == null || applicationId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceResminamalarOpenHelper.TryOpen(
            _application,
            applicationId,
            VisaPreviewSlotViewHelper.ResolveOwnerViewId(View));

        return Task.CompletedTask;
    }

    private async Task OpenPersonDetailByIndexAsync(int rowIndex)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = rowIndex;
        UpdateActionState(model);
        await OpenPersonDetailAsync();
    }

    private async Task SaveProgressNotesAsync(string notes)
    {
        var model = ComponentModel;
        var applicationId = ResolveApplicationProfileInstanceId();
        if (model == null || _application == null || applicationId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.SaveOfficerNotes(objectSpace, applicationId, notes);
            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not save notes.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Notes saved.";
            model.ProgressStatusIsError = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
        }
    }

    private async Task UploadMinistryLetterAsync(OfficerShellCaseProgressFileUpload upload)
    {
        var model = ComponentModel;
        var applicationId = ResolveApplicationProfileInstanceId();
        if (model == null || _application == null || applicationId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.SetMinistryLetter(
                objectSpace,
                applicationId,
                upload.FileName,
                upload.Content);

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not upload ministry letter.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Ministry letter uploaded.";
            model.ProgressStatusIsError = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
        }
    }

    private async Task AdvanceCaseProgressAsync(OfficerShellCaseProgressAdvanceRequest request)
    {
        var model = ComponentModel;
        var applicationId = ResolveApplicationProfileInstanceId();
        if (model == null || _application == null || applicationId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.Advance(
                objectSpace,
                applicationId,
                request.StateCode,
                request.Notes);

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not advance progress.";
                model.ProgressStatusIsError = true;
                await LoadAsync();
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Progress advanced.";
            model.ProgressStatusIsError = false;
            model.CaseTab = "progress";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
            await LoadAsync();
        }
    }

    private async Task SearchPersonLinkCandidatesAsync(string searchText)
    {
        var model = ComponentModel;
        var applicationId = ResolveApplicationProfileInstanceId();
        if (model == null || _application == null || applicationId == Guid.Empty)
            return;

        model.PersonLinkIsSearching = true;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(Person));
            var service = _personLinkQueryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfileInstancePersonLinkQueryService>()
                ?? new ApplicationProfileInstancePersonLinkQueryService();

            model.PersonLinkCandidates = service.SearchCandidates(objectSpace, applicationId, searchText);
        }
        catch (Exception ex)
        {
            model.PersonLinkStatusMessage = ex.Message;
            model.PersonLinkStatusIsError = true;
            model.PersonLinkCandidates = Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();
        }
        finally
        {
            model.PersonLinkIsSearching = false;
        }
    }

    private async Task LinkPersonFromPickerAsync(Guid personId)
    {
        var model = ComponentModel;
        var applicationId = ResolveApplicationProfileInstanceId();
        if (model == null || _application == null || applicationId == Guid.Empty || personId == Guid.Empty)
            return;

        model.PersonLinkIsLinking = true;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
            var person = objectSpace.GetObjectByKey<Person>(personId);
            if (application == null || person == null)
            {
                model.PersonLinkStatusMessage = "Person or application not found.";
                model.PersonLinkStatusIsError = true;
                return;
            }

            if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal");
                model.PersonLinkStatusIsError = true;
                return;
            }

            var linked = ApplicationProfileInstancePersonService.LinkPerson(objectSpace, application, person);
            if (linked == null)
            {
                model.PersonLinkStatusMessage = "Could not link the selected person.";
                model.PersonLinkStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ShowPersonLinkPicker = false;
            model.PersonLinkCandidates = Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();
            model.PersonLinkStatusMessage = $"{person.FullName} linked.";
            model.PersonLinkStatusIsError = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            model.PersonLinkStatusMessage = ex.Message;
            model.PersonLinkStatusIsError = true;
        }
        finally
        {
            model.PersonLinkIsLinking = false;
        }
    }

    private Task ClosePersonLinkPickerAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.ShowPersonLinkPicker = false;
        model.PersonLinkCandidates = Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();
        model.PersonLinkStatusMessage = null;
        model.PersonLinkStatusIsError = false;
        return Task.CompletedTask;
    }

    private void ApplyApplicationProfileInstanceIdFromContext()
    {
        if (CurrentObject is not ApplicationWorkspaceHost host || host.ApplicationProfileInstanceId != Guid.Empty)
            return;

        var pending = _application != null
            ? ApplicationWorkspacePendingOpenGate.Get(_application)
            : Guid.Empty;
        if (pending != Guid.Empty)
            host.ApplicationProfileInstanceId = pending;
    }

    private Guid ResolveApplicationProfileInstanceId()
    {
        ApplyApplicationProfileInstanceIdFromContext();
        if (CurrentObject is ApplicationWorkspaceHost host && host.ApplicationProfileInstanceId != Guid.Empty)
            return host.ApplicationProfileInstanceId;

        return _application != null
            ? ApplicationWorkspacePendingOpenGate.Get(_application)
            : Guid.Empty;
    }
}
