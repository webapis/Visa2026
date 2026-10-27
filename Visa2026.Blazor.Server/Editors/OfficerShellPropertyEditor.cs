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
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.ApplicationPersonLink;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.OfficerShell;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Services.OrganizationCatalogs;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), OfficerShellEditorAliases.Shell, false)]
public class OfficerShellPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IOfficerShellNavQueryService? _navQueryService;
    private IOfficerShellStagedQueryService? _stagedQueryService;
    private IOfficerShellInProcessQueryService? _inProcessQueryService;
    private IApplicationWorkspaceQueryService? _workspaceQueryService;
    private IApplicationProfileCatalogQueryService? _catalogQueryService;
    private IApplicationProfileOverviewQueryService? _overviewQueryService;
    private IOfficerShellStartProcessService? _startProcessService;
    private IOfficerShellCaseProgressService? _caseProgressService;
    private IApplicationProfileInstancePersonLinkQueryService? _personLinkQueryService;
    private IApplicationWorkspacePersonUiActions? _personUiActions;
    private IReadOnlyList<ApplicationProfileCatalogRow> _allCatalogRows = Array.Empty<ApplicationProfileCatalogRow>();

    public OfficerShellPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override OfficerShellModel ComponentModel => (OfficerShellModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        var sp = application.ServiceProvider;
        _navQueryService = sp?.GetService<IOfficerShellNavQueryService>();
        _stagedQueryService = sp?.GetService<IOfficerShellStagedQueryService>();
        _inProcessQueryService = sp?.GetService<IOfficerShellInProcessQueryService>();
        _workspaceQueryService = sp?.GetService<IApplicationWorkspaceQueryService>();
        _catalogQueryService = sp?.GetService<IApplicationProfileCatalogQueryService>();
        _overviewQueryService = sp?.GetService<IApplicationProfileOverviewQueryService>();
        _startProcessService = sp?.GetService<IOfficerShellStartProcessService>();
        _caseProgressService = sp?.GetService<IOfficerShellCaseProgressService>();
        _personLinkQueryService = sp?.GetService<IApplicationProfileInstancePersonLinkQueryService>();
        _personUiActions = sp?.GetService<IApplicationWorkspacePersonUiActions>();
        if (_personUiActions != null)
            _personUiActions.WorkspaceChanged += OnWorkspaceChanged;
    }

    protected override IComponentModel CreateComponentModel() => new OfficerShellModel
    {
        IsLoading = true,
        SelectedStagedIds = new HashSet<Guid>(),
        StagedGroupCollapsed = new HashSet<string>(),
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        NavigateRequested = EventCallback.Factory.Create<OfficerShellPage>(this, NavigateAsync),
        OpenCaseRequested = EventCallback.Factory.Create<Guid>(this, OpenCaseAsync),
        StartProcessRequested = EventCallback.Factory.Create(this, StartProcessAsync),
        ToggleStagedSelectionRequested = EventCallback.Factory.Create<Guid>(this, ToggleStagedSelectionAsync),
        StagedViewModeChanged = EventCallback.Factory.Create<string>(this, OnStagedViewModeChanged),
        InProcessViewModeChanged = EventCallback.Factory.Create<string>(this, OnInProcessViewModeChanged),
        SearchTextChanged = EventCallback.Factory.Create<string>(this, OnSearchTextChanged),
        StagedFamilyFilterChanged = EventCallback.Factory.Create<string>(this, OnStagedFamilyFilterChanged),
        InProcessFamilyFilterChanged = EventCallback.Factory.Create<string>(this, OnInProcessFamilyFilterChanged),
        StagedPageChanged = EventCallback.Factory.Create<int>(this, OnStagedPageChanged),
        StagedPageSizeChanged = EventCallback.Factory.Create<int>(this, OnStagedPageSizeChanged),
        InProcessPageChanged = EventCallback.Factory.Create<int>(this, OnInProcessPageChanged),
        InProcessPageSizeChanged = EventCallback.Factory.Create<int>(this, OnInProcessPageSizeChanged),
        ToggleStagedGroupCollapsed = EventCallback.Factory.Create<string>(this, OnToggleStagedGroupCollapsed),
        NewProfileRequested = EventCallback.Factory.Create(this, NewProfileAsync),
        SelectProfileRequested = EventCallback.Factory.Create<Guid>(this, SelectProfileAsync),
        ConfigureProfileRequested = EventCallback.Factory.Create(this, ConfigureProfileAsync),
        CatalogSearchTextChanged = EventCallback.Factory.Create<string>(this, OnCatalogSearchChanged),
        TemplatesViewModeChanged = EventCallback.Factory.Create<string>(this, OnTemplatesViewModeChanged),
        TemplatesFamilyFilterChanged = EventCallback.Factory.Create<string>(this, OnTemplatesFamilyFilterChanged),
        TemplatesPageChanged = EventCallback.Factory.Create<int>(this, OnTemplatesPageChanged),
        TemplatesPageSizeChanged = EventCallback.Factory.Create<int>(this, OnTemplatesPageSizeChanged),
        OpenTemplateDetailRequested = EventCallback.Factory.Create<Guid>(this, OpenTemplateDetailAsync),
        BackToTemplateCatalogRequested = EventCallback.Factory.Create(this, BackToTemplateCatalogAsync),
        ConfigureTemplateRequested = EventCallback.Factory.Create<Guid>(this, ConfigureTemplateAsync),
        CaseTabChanged = EventCallback.Factory.Create<string>(this, OnCaseTabChanged),
        LinkedRecordTileClicked = EventCallback.Factory.Create<string>(this, OnLinkedRecordTileClicked),
        IssuedHeaderNewRequested = EventCallback.Factory.Create<string>(this, OnIssuedHeaderNewRequested),
        IssuedHeaderOpenRequested = EventCallback.Factory.Create<ApplicationWorkspaceIssuedHeaderOpenRequest>(this, OnIssuedHeaderOpenRequested),
        BackToInProcessRequested = EventCallback.Factory.Create(this, BackToInProcessAsync),
        DeleteCaseRequested = EventCallback.Factory.Create(this, DeleteCaseAsync),
        LinkPersonRequested = EventCallback.Factory.Create(this, LinkPersonAsync),
        UnlinkPersonRequested = EventCallback.Factory.Create<Guid>(this, UnlinkPersonAsync),
        OpenPersonDetailRequested = EventCallback.Factory.Create(this, OpenPersonDetailAsync),
        OpenDocumentCopiesRequested = EventCallback.Factory.Create(this, OpenDocumentCopiesAsync),
        OpenResminamalarRequested = EventCallback.Factory.Create(this, OpenResminamalarAsync),
        SelectPersonRowRequested = EventCallback.Factory.Create<int>(this, SelectPersonRow),
        OpenPersonDetailByIndexRequested = EventCallback.Factory.Create<int>(this, OpenPersonDetailByIndexAsync),
        RelinkPersonRequested = EventCallback.Factory.Create<Guid>(this, RelinkPersonAsync),
        SaveProgressNotesRequested = EventCallback.Factory.Create<string>(this, SaveProgressNotesAsync),
        UploadMinistryLetterRequested = EventCallback.Factory.Create<OfficerShellCaseProgressFileUpload>(this, UploadMinistryLetterAsync),
        AdvanceProgressRequested = EventCallback.Factory.Create<OfficerShellCaseProgressAdvanceRequest>(this, AdvanceCaseProgressAsync),
        SaveMigrationSubmissionRequested = EventCallback.Factory.Create<OfficerShellCaseMigrationSubmissionRequest>(this, SaveMigrationSubmissionAsync),
        RevertProgressRequested = EventCallback.Factory.Create<OfficerShellCaseProgressRevertRequest>(this, RevertCaseProgressAsync),
        SaveStepRequested = EventCallback.Factory.Create<OfficerShellCaseProgressStepCorrection>(this, SaveStepAsync),
        ChangeFileRequested = EventCallback.Factory.Create<OfficerShellCaseProgressFileChange>(this, ChangeFileAsync),
        PersonLinkSearchRequested = EventCallback.Factory.Create<string>(this, SearchPersonLinkCandidatesAsync),
        LinkPersonFromPickerRequested = EventCallback.Factory.Create<Guid>(this, LinkPersonFromPickerAsync),
        OpenExistingPersonLinkReviewRequested = EventCallback.Factory.Create<Guid>(this, OpenExistingPersonLinkReviewAsync),
        ClosePersonLinkPickerRequested = EventCallback.Factory.Create(this, ClosePersonLinkPickerAsync),
        JoinPersonLinkReviewRequested = EventCallback.Factory.Create(this, JoinPersonLinkReviewAsync),
        CancelPersonLinkReviewRequested = EventCallback.Factory.Create(this, CancelPersonLinkReviewAsync),
        EditPersonLinkReviewRequested = EventCallback.Factory.Create<PersonLinkReviewLine>(this, EditPersonLinkReviewAsync),
        SelectPersonLinkReviewRequested = EventCallback.Factory.Create<PersonLinkReviewChoiceRequest>(this, SelectPersonLinkReviewAsync),
        CreatePersonLinkReviewRequested = EventCallback.Factory.Create<PersonLinkReviewLine>(this, CreatePersonLinkReviewAsync),
        SavePersonLinkReviewCreateRequested = EventCallback.Factory.Create<PersonLinkReviewCreateForm>(this, SavePersonLinkReviewCreateAsync),
        HeaderFieldChanged = EventCallback.Factory.Create<ApplicationWorkspaceCaseHeaderFieldUpdate>(this, SaveHeaderFieldAsync),
        AlternativeAddressCreateRequested = EventCallback.Factory.Create(this, OpenNewAlternativeAddress),
        InvitationSiteCreateRequested = EventCallback.Factory.Create<string>(this, OpenNewInvitationSite),
        OrganizationLetterheadChanged = EventCallback.Factory.Create<ApplicationWorkspaceOrganizationLetterheadUpdate>(this, SaveOrganizationLetterheadAsync),
        OrganizationCatalogEditorRequested = EventCallback.Factory.Create<(string Kind, Guid Id)>(this, OpenOrganizationCatalogEditor),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ApplyPendingOpen();
        _ = LoadAsync();
    }

    private void ApplyPendingOpen()
    {
        if (_application == null)
            return;

        var (page, caseId) = OfficerShellPendingOpenGate.Get(_application);
        var model = ComponentModel;
        if (model == null)
            return;

        model.CurrentPage = page;
        if (model.CaseApplicationProfileInstanceId != caseId)
            model.ShowProgressRevertToHere = false;
        model.CaseApplicationProfileInstanceId = caseId;
    }

    private async Task LoadAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        model.IsLoading = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        await Task.Delay(16);

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));

            var navService = _navQueryService ?? _application.ServiceProvider?.GetService<IOfficerShellNavQueryService>();
            var stagedService = _stagedQueryService ?? _application.ServiceProvider?.GetService<IOfficerShellStagedQueryService>();
            var inProcessService = _inProcessQueryService ?? _application.ServiceProvider?.GetService<IOfficerShellInProcessQueryService>();

            model.NavCounts = navService?.GetCounts(objectSpace) ?? new OfficerShellNavCounts();
            model.StagedRows = stagedService?.GetStagedProfiles(objectSpace) ?? Array.Empty<OfficerShellStagedRow>();
            model.InProcessRows = inProcessService?.GetInProcessProfiles(objectSpace) ?? Array.Empty<OfficerShellInProcessRow>();

            await LoadCatalogAsync(model, objectSpace);

            if (model.CurrentPage == OfficerShellPage.Case && model.CaseApplicationProfileInstanceId != Guid.Empty)
                await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private Task LoadCatalogAsync(OfficerShellModel model, IObjectSpace objectSpace)
    {
        var catalogService = _catalogQueryService
            ?? _application?.ServiceProvider?.GetService<IApplicationProfileCatalogQueryService>();
        if (catalogService == null)
            return Task.CompletedTask;

        _allCatalogRows = catalogService.GetProfiles(objectSpace);
        ApplyCatalogFilter(model);

        // Template overview opens in its own MDI tab; keep the Templates list page mounted.
        model.TemplatesDetailOpen = false;
        model.SelectedProfileId = Guid.Empty;
        model.OverviewSnapshot = null;
        return Task.CompletedTask;
    }

    private async Task LoadWorkspaceAsync(OfficerShellModel model, Guid applicationId, bool showLoading = true)
    {
        if (_application == null || applicationId == Guid.Empty)
            return;

        if (showLoading)
        {
            model.WorkspaceLoading = true;
            model.WorkspaceLoadingProgressPercent = -1;
            model.WorkspaceLoadingMessage = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.Opening");
            await Task.Delay(16);
        }

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _workspaceQueryService
                ?? _application.ServiceProvider?.GetService<IApplicationWorkspaceQueryService>();

            model.WorkspaceSnapshot = service != null
                ? service.Load(objectSpace, applicationId)
                : new ApplicationWorkspaceMockQueryService().Load(objectSpace, applicationId);

            model.SelectedPersonRowIndex = -1;
            UpdateCaseActionState(model);
        }
        finally
        {
            if (showLoading)
            {
                model.WorkspaceLoading = false;
                model.WorkspaceLoadingMessage = string.Empty;
            }
        }
    }

    private void UpdateCaseActionState(OfficerShellModel model)
    {
        var applicationId = model.CaseApplicationProfileInstanceId;
        var rosterLocked = model.WorkspaceSnapshot?.CaseChrome.ResolvedLinksLocked == true;
        var canLink = applicationId != Guid.Empty && _application?.MainWindow != null && !rosterLocked;
        model.CanLinkPerson = canLink;
        model.CanUnlinkPerson = canLink;

        var personTab = model.WorkspaceSnapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        model.CanOpenPersonDetail = personTab != null
            && model.SelectedPersonRowIndex >= 0
            && model.SelectedPersonRowIndex < personTab.RowPersonIds.Count;
        model.CanOpenDocumentCopies = personTab != null
            && personTab.RowApplicationProfileInstancePersonIds.Count > 0
            && (model.SelectedPersonRowIndex < 0
                || model.SelectedPersonRowIndex < personTab.RowApplicationProfileInstancePersonIds.Count);
    }

    private void OnWorkspaceChanged()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = -1;
        if (model.CaseApplicationProfileInstanceId != Guid.Empty)
            _ = LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private async Task NavigateAsync(OfficerShellPage page)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.CurrentPage = page;
        model.StatusMessage = null;

        if (page != OfficerShellPage.Case)
            model.ShowProgressRevertToHere = false;

        if (page != OfficerShellPage.Templates)
            model.TemplatesDetailOpen = false;

        if (page == OfficerShellPage.Case && model.CaseApplicationProfileInstanceId != Guid.Empty)
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private async Task OpenCaseAsync(Guid applicationId)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.CaseApplicationProfileInstanceId = applicationId;
        model.CurrentPage = OfficerShellPage.Case;
        model.CaseTab = "overview";
        model.PeopleLinkedRecordFocusKey = null;
        model.ShowProgressRevertToHere = false;
        await LoadWorkspaceAsync(model, applicationId);
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

    private async Task OnIssuedHeaderNewRequested(string key)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        if (IssueIssuedHeaderComposeService.TryResolveKind(key, out _))
        {
            await IssueIssuedHeaderPreviewSlotOpenHelper.TryOpenComposeAsync(
                _application,
                model.CaseApplicationProfileInstanceId,
                key,
                View?.Id);
            return;
        }

        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase)
            && await IssueIssuedVisaPreviewSlotOpenHelper.TryOpenComposeAsync(
                _application,
                model.CaseApplicationProfileInstanceId,
                View?.Id))
        {
            return;
        }

        ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate(
            _application,
            _application.MainWindow,
            model.CaseApplicationProfileInstanceId,
            key);
    }

    private async Task OnIssuedHeaderOpenRequested(ApplicationWorkspaceIssuedHeaderOpenRequest request)
    {
        if (_application == null || request == null || request.Id == Guid.Empty)
            return;

        var model = ComponentModel;
        if (model != null
            && IssueIssuedHeaderComposeService.TryResolveKind(request.Key, out _)
            && model.CaseApplicationProfileInstanceId != Guid.Empty)
        {
            await IssueIssuedHeaderPreviewSlotOpenHelper.TryOpenAsync(
                _application,
                model.CaseApplicationProfileInstanceId,
                request.Key,
                request.Id,
                View?.Id);
            return;
        }

        if (model != null
            && string.Equals(request.Key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase)
            && model.CaseApplicationProfileInstanceId != Guid.Empty
            && await IssueIssuedVisaPreviewSlotOpenHelper.TryOpenComposeAsync(
                _application,
                model.CaseApplicationProfileInstanceId,
                View?.Id,
                request.Id))
        {
            return;
        }

        ApplicationWorkspaceIssuedHeaderOpenHelper.TryOpen(
            _application,
            _application.MainWindow,
            request.Key,
            request.Id);
    }

    private async Task BackToInProcessAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.CurrentPage = OfficerShellPage.InProcess;
        model.CaseTab = "overview";
        model.SelectedPersonRowIndex = -1;
        model.PeopleLinkedRecordFocusKey = null;
        model.ShowProgressRevertToHere = false;
        model.DeleteCaseErrorMessage = null;
        await LoadAsync();
    }

    private async Task DeleteCaseAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        var applicationId = model.CaseApplicationProfileInstanceId;
        model.DeleteCaseErrorMessage = null;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            if (!ApplicationProfileInstanceDeleteHelper.TryDelete(objectSpace, applicationId, out var error))
            {
                model.DeleteCaseErrorMessage = error;
                return;
            }
        }
        catch (Exception ex)
        {
            model.DeleteCaseErrorMessage = EntitySaveExceptionFormatter.ToOfficerMessage(ex);
            return;
        }

        _application.ServiceProvider?.GetService<IApplicationProfileInstanceDeletedNotifier>()?.NotifyDeleted(applicationId);
        model.CaseApplicationProfileInstanceId = Guid.Empty;
        model.WorkspaceSnapshot = null;
        await BackToInProcessAsync();
    }

    private async Task LinkPersonAsync()
    {
        var model = ComponentModel;
        if (model == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        model.ShowPersonLinkPicker = true;
        model.PersonLinkStatusMessage = null;
        model.PersonLinkStatusIsError = false;
        await SearchPersonLinkCandidatesAsync(string.Empty);
    }

    private async Task SearchPersonLinkCandidatesAsync(string searchText)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        model.PersonLinkIsSearching = true;
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(Person));
            var service = _personLinkQueryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfileInstancePersonLinkQueryService>()
                ?? new ApplicationProfileInstancePersonLinkQueryService();

            model.PersonLinkCandidates = service.SearchCandidates(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                searchText);
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
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty || personId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
            var person = objectSpace.GetObjectByKey<Person>(personId);
            if (application == null || person == null)
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.PersonOrApplicationNotFound");
                model.PersonLinkStatusIsError = true;
                return;
            }

            if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal");
                model.PersonLinkStatusIsError = true;
                return;
            }

            if (ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out var passportBlock))
            {
                model.PersonLinkStatusMessage = passportBlock;
                model.PersonLinkStatusIsError = true;
                return;
            }

            model.PersonLinkReviewExisting = false;
            model.PersonLinkReviewPersonId = personId;
            model.PersonLinkReviewPersonName = person.FullName;
            model.PersonLinkReviewPhotoDataUri = PdfPersonPhotoDataUri.FromBytes(person.Photo);
            model.PersonLinkReviewLines = PersonLinkReview.Build(objectSpace, application, person);
            model.ShowPersonLinkPicker = false;
            model.ShowPersonLinkReview = true;
            model.PersonLinkStatusMessage = null;
            model.PersonLinkStatusIsError = false;
        }
        catch (Exception ex)
        {
            model.PersonLinkStatusMessage = EntitySaveExceptionFormatter.ToOfficerMessage(ex);
            model.PersonLinkStatusIsError = true;
        }

        await Task.CompletedTask;
    }

    private async Task OpenExistingPersonLinkReviewAsync(Guid personId)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty || personId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
            var person = objectSpace.GetObjectByKey<Person>(personId);
            if (application == null || person == null)
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.PersonOrApplicationNotFound");
                model.PersonLinkStatusIsError = true;
                return;
            }

            if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal");
                model.PersonLinkStatusIsError = true;
                return;
            }

            var lines = PersonLinkReview.Build(objectSpace, application, person).ToList();
            PersonLinkReview.PreferCaseLinks(objectSpace, application, person, lines);
            model.PersonLinkReviewExisting = true;
            model.PersonLinkReviewPersonId = personId;
            model.PersonLinkReviewPersonName = person.FullName;
            model.PersonLinkReviewPhotoDataUri = PdfPersonPhotoDataUri.FromBytes(person.Photo);
            model.PersonLinkReviewLines = lines;
            model.ShowPersonLinkPicker = false;
            model.ShowPersonLinkReview = true;
            model.PersonLinkStatusMessage = null;
            model.PersonLinkStatusIsError = false;
        }
        catch (Exception ex)
        {
            model.PersonLinkStatusMessage = EntitySaveExceptionFormatter.ToOfficerMessage(ex);
            model.PersonLinkStatusIsError = true;
        }

        await Task.CompletedTask;
    }

    private async Task JoinPersonLinkReviewAsync()
    {
        var model = ComponentModel;
        var personId = model?.PersonLinkReviewPersonId ?? Guid.Empty;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty || personId == Guid.Empty)
            return;

        model.PersonLinkIsLinking = true;
        await Task.Delay(16);
        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
            var person = objectSpace.GetObjectByKey<Person>(personId);
            if (application == null || person == null)
            {
                model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.PersonOrApplicationNotFound");
                model.PersonLinkStatusIsError = true;
                return;
            }

            var existing = model.PersonLinkReviewExisting;
            if (!existing)
            {
                var linked = ApplicationProfileInstancePersonService.LinkPerson(objectSpace, application, person);
                if (linked == null)
                {
                    model.PersonLinkStatusMessage = VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.CouldNotLink");
                    model.PersonLinkStatusIsError = true;
                    return;
                }
            }

            PersonLinkReview.ApplyJoinLinks(objectSpace, application, person, model.PersonLinkReviewLines);
            objectSpace.CommitChanges();
            ClearPersonLinkReview(model);
            model.ShowPersonLinkPicker = false;
            model.PersonLinkCandidates = Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();
            model.PersonLinkStatusMessage = existing
                ? VisaUiMessages.Format("ApplicationProfileInstance.LinkReview.Updated", person.FullName)
                : VisaUiMessages.Format("ApplicationProfileInstance.LinkReview.Linked", person.FullName);
            model.PersonLinkStatusIsError = false;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.PersonLinkStatusMessage = EntitySaveExceptionFormatter.ToOfficerMessage(ex);
            model.PersonLinkStatusIsError = true;
        }
        finally
        {
            model.PersonLinkIsLinking = false;
        }
    }

    private Task CancelPersonLinkReviewAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        var existing = model.PersonLinkReviewExisting;
        ClearPersonLinkReview(model);
        if (!existing)
            model.ShowPersonLinkPicker = true;
        model.PersonLinkStatusMessage = null;
        model.PersonLinkStatusIsError = false;
        return Task.CompletedTask;
    }

    private Task SelectPersonLinkReviewAsync(PersonLinkReviewChoiceRequest request)
    {
        var model = ComponentModel;
        if (model == null || request == null)
            return Task.CompletedTask;

        var line = model.PersonLinkReviewLines.FirstOrDefault(item => item.Kind == request.Kind);
        if (line == null)
            return Task.CompletedTask;

        PersonLinkReview.ApplyChoice(line, request.ObjectId);
        if (line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence)
        {
            model.PersonLinkReviewDeclineCaseSummaryAddress = line.ObjectId != Guid.Empty;
            model.PersonLinkReviewEditedAddressId = line.ObjectId;
        }

        model.PersonLinkReviewLines = model.PersonLinkReviewLines.ToArray();
        return Task.CompletedTask;
    }

    private Task CreatePersonLinkReviewAsync(PersonLinkReviewLine line)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.PersonLinkReviewPersonId == Guid.Empty)
            return Task.CompletedTask;

        using var objectSpace = _application.CreateObjectSpace(typeof(Person));
        model.PersonLinkReviewCreateForm = PersonLinkReview.BuildCreateForm(objectSpace, line.Kind, line.Label);
        return Task.CompletedTask;
    }

    private Task SavePersonLinkReviewCreateAsync(PersonLinkReviewCreateForm form)
    {
        var model = ComponentModel;
        if (model == null || _application == null || form == null || model.PersonLinkReviewPersonId == Guid.Empty)
            return Task.CompletedTask;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
            var person = objectSpace.GetObjectByKey<Person>(model.PersonLinkReviewPersonId);
            if (person == null)
            {
                form.Error = VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.PersonNotFound");
                model.PersonLinkReviewCreateForm = form;
                return Task.CompletedTask;
            }

            var passportId = model.PersonLinkReviewLines
                .FirstOrDefault(item => item.Kind == ApplicationProfileInstancePersonLinkKind.Passport)?.ObjectId ?? Guid.Empty;
            var savedId = PersonLinkReview.TryCreate(objectSpace, person, form, passportId, out var error);
            if (savedId == Guid.Empty)
            {
                form.Error = error ?? VisaUiMessages.Get("ApplicationProfileInstance.LinkReview.CouldNotSave");
                model.PersonLinkReviewCreateForm = form;
                return Task.CompletedTask;
            }

            objectSpace.CommitChanges();
            var line = model.PersonLinkReviewLines.FirstOrDefault(item => item.Kind == form.Kind);
            if (line != null)
                PersonLinkReview.SelectSaved(objectSpace, application, person, line, savedId);
            if (form.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence)
            {
                model.PersonLinkReviewDeclineCaseSummaryAddress = true;
                model.PersonLinkReviewEditedAddressId = savedId;
            }

            form.Saved = true;
            form.Error = null;
            model.PersonLinkReviewCreateForm = null;
            model.PersonLinkReviewLines = model.PersonLinkReviewLines.ToArray();
        }
        catch (Exception ex)
        {
            form.Error = EntitySaveExceptionFormatter.ToOfficerMessage(ex);
            model.PersonLinkReviewCreateForm = form;
        }

        return Task.CompletedTask;
    }

    private Task RememberCreatedReviewAsync(ApplicationProfileInstancePersonLinkKind kind, Guid savedId)
    {
        var model = ComponentModel;
        if (model == null || _application == null || savedId == Guid.Empty)
            return Task.CompletedTask;

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        var person = objectSpace.GetObjectByKey<Person>(model.PersonLinkReviewPersonId);
        var line = model.PersonLinkReviewLines.FirstOrDefault(item => item.Kind == kind);
        if (person == null || line == null)
            return Task.CompletedTask;

        PersonLinkReview.SelectSaved(objectSpace, application, person, line, savedId);
        if (line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence && line.ObjectId != Guid.Empty)
        {
            model.PersonLinkReviewDeclineCaseSummaryAddress = true;
            model.PersonLinkReviewEditedAddressId = line.ObjectId;
        }

        model.PersonLinkReviewLines = model.PersonLinkReviewLines.ToArray();
        return Task.CompletedTask;
    }

    private Task EditPersonLinkReviewAsync(PersonLinkReviewLine line)
    {
        var model = ComponentModel;
        if (model == null || _application == null || line == null || model.PersonLinkReviewPersonId == Guid.Empty)
            return Task.CompletedTask;

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        var person = objectSpace.GetObjectByKey<Person>(model.PersonLinkReviewPersonId);
        model.PersonLinkReviewCreateForm = PersonLinkReview.BuildEditForm(objectSpace, application, person, line);
        return Task.CompletedTask;
    }

    private static void ClearPersonLinkReview(OfficerShellModel model)
    {
        model.ShowPersonLinkReview = false;
        model.PersonLinkReviewExisting = false;
        model.PersonLinkReviewPersonId = Guid.Empty;
        model.PersonLinkReviewPersonName = null;
        model.PersonLinkReviewPhotoDataUri = null;
        model.PersonLinkReviewLines = Array.Empty<PersonLinkReviewLine>();
        model.PersonLinkReviewDeclineCaseSummaryAddress = false;
        model.PersonLinkReviewEditedAddressId = Guid.Empty;
        model.PersonLinkReviewCreateForm = null;
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
        ClearPersonLinkReview(model);
        return Task.CompletedTask;
    }

    private async Task SaveHeaderFieldAsync(ApplicationWorkspaceCaseHeaderFieldUpdate update)
    {
        var model = ComponentModel;
        if (model == null || _application == null || update == null)
            return;

        if (model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        if (application == null)
            return;

        if (!ApplicationWorkspaceCaseHeaderFieldsHelper.TryApply(application, objectSpace, update, out var error))
        {
            model.HeaderFieldStatusMessage = error;
            model.HeaderFieldStatusIsError = true;
            return;
        }

        if (objectSpace.IsModified)
            objectSpace.CommitChanges();

        model.HeaderFieldStatusMessage = null;
        model.HeaderFieldStatusIsError = false;
        // Keep Done on the page. The opening skeleton removes it between mouse-down and
        // mouse-up, so the first click never switches the button back to Edit.
        await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId, showLoading: false);
    }

    private void OpenNewAlternativeAddress()
    {
        if (_application == null)
            return;

        AlternativeAddressCatalogOpenHelper.TryOpenNew(_application, savedId =>
        {
            if (savedId == Guid.Empty)
                return;

            _ = SaveHeaderFieldAsync(new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationAlternativeAddress,
                Value = savedId.ToString("D"),
            });
        });
    }

    private void OpenNewInvitationSite(string fieldKey)
    {
        if (_application == null || string.IsNullOrWhiteSpace(fieldKey))
            return;

        var fields = ComponentModel?.WorkspaceSnapshot?.CaseView?.HeaderFields;
        var cityId = fields?
            .FirstOrDefault(field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity)
            ?.SelectedId;
        if (cityId is not Guid id || id == Guid.Empty)
            return;

        InvitationSiteCatalogOpenHelper.TryOpenNew(_application, fieldKey, id, savedId =>
        {
            if (savedId == Guid.Empty)
                return;

            _ = SaveHeaderFieldAsync(new ApplicationWorkspaceCaseHeaderFieldUpdate
            {
                Key = fieldKey,
                Value = savedId.ToString("D"),
            });
        });
    }

    private async Task SaveOrganizationLetterheadAsync(ApplicationWorkspaceOrganizationLetterheadUpdate update)
    {
        var model = ComponentModel;
        if (model == null || _application == null || update == null)
            return;

        if (model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        if (application == null)
            return;

        if (update.MakeDefault)
        {
            var defaultId = update.SelectedId ?? Guid.Empty;
            if (!OrganizationCatalogHelper.TryMakeDefault(objectSpace, update.Kind, defaultId, out var defaultError))
            {
                model.OrganizationStatusMessage = defaultError;
                model.OrganizationStatusIsError = true;
                return;
            }
        }
        else if (!OrganizationCatalogHelper.TryAssign(
                     application, objectSpace, update.Kind, update.SelectedId, out var error))
        {
            model.OrganizationStatusMessage = error;
            model.OrganizationStatusIsError = true;
            return;
        }

        objectSpace.SetModified(application);
        if (objectSpace.IsModified)
            objectSpace.CommitChanges();

        model.OrganizationStatusMessage = null;
        model.OrganizationStatusIsError = false;
        await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private void OpenOrganizationCatalogEditor((string Kind, Guid Id) request)
    {
        if (_application == null || string.IsNullOrWhiteSpace(request.Kind))
            return;

        var wasNew = request.Id == Guid.Empty;
        OrganizationCatalogsOpenHelper.TryOpenEditor(
            _application,
            request.Kind,
            request.Id,
            onClosed: wasNew ? null : ReloadCaseOrganization,
            onSaved: savedId =>
            {
                if (wasNew && savedId != Guid.Empty)
                {
                    _ = SaveOrganizationLetterheadAsync(new ApplicationWorkspaceOrganizationLetterheadUpdate
                    {
                        Kind = request.Kind,
                        SelectedId = savedId,
                    });
                    return;
                }

                ReloadCaseOrganization();
            });
    }

    private void ReloadCaseOrganization()
    {
        var model = ComponentModel;
        if (model == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        _ = LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private async Task UnlinkPersonAsync(Guid personId)
    {
        var model = ComponentModel;
        if (model == null
            || _application == null
            || model.CaseApplicationProfileInstanceId == Guid.Empty
            || personId == Guid.Empty)
        {
            return;
        }

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (application == null || person == null)
            return;

        ApplicationProfileInstancePersonService.UnlinkPerson(objectSpace, application, person);
        if (objectSpace.IsModified)
            objectSpace.CommitChanges();

        await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private void SelectPersonRow(int rowIndex)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = model.SelectedPersonRowIndex == rowIndex ? -1 : rowIndex;
        UpdateCaseActionState(model);
    }

    private async Task OpenPersonDetailByIndexAsync(int rowIndex)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedPersonRowIndex = rowIndex;
        UpdateCaseActionState(model);
        await OpenPersonDetailAsync();
    }

    private async Task RelinkPersonAsync(Guid personId)
    {
        var model = ComponentModel;
        if (model == null
            || _application == null
            || model.CaseApplicationProfileInstanceId == Guid.Empty
            || personId == Guid.Empty)
        {
            return;
        }

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(model.CaseApplicationProfileInstanceId);
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (application == null || person == null)
            return;

        if (!ApplicationProfileInstancePersonService.RelinkPerson(objectSpace, application, person))
            return;

        if (objectSpace.IsModified)
            objectSpace.CommitChanges();

        await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
    }

    private async Task SaveProgressNotesAsync(string notes)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.SaveOfficerNotes(objectSpace, model.CaseApplicationProfileInstanceId, notes);
            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not save notes.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Notes saved.";
            model.ProgressStatusIsError = false;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
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
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.SetMinistryLetter(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                upload.FileName,
                upload.Content,
                upload.ProgressId);

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not upload ministry letter.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Ministry letter uploaded.";
            model.ProgressStatusIsError = false;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
        }
    }

    private async Task SaveStepAsync(OfficerShellCaseProgressStepCorrection request)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();
            var result = service.CorrectStep(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                request.ProgressId,
                request.Date,
                request.SubmissionDocumentNumber,
                request.ResultNumber);
            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not save this step.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.StepSaved");
            model.ProgressStatusIsError = false;
            model.ProgressCorrectionRevision++;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
        }
    }

    private async Task ChangeFileAsync(OfficerShellCaseProgressFileChange change)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();
            var applicationId = model.CaseApplicationProfileInstanceId;
            OfficerShellCaseProgressResult result;
            if (change.Remove)
            {
                result = service.RemoveProgressFile(objectSpace, applicationId, change.ProgressId, change.FileId ?? Guid.Empty);
            }
            else if (change.FileId is Guid fileId && fileId != Guid.Empty)
            {
                result = service.ReplaceProgressFile(objectSpace, applicationId, change.ProgressId, fileId, change.FileName, change.Content);
            }
            else
            {
                result = service.AddProgressFile(objectSpace, applicationId, change.ProgressId, change.Slot, change.FileName, change.Content);
            }

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not update the file.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = change.Remove
                ? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.FileRemoved")
                : change.FileId is Guid
                    ? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.FileReplaced")
                    : VisaUiMessages.Get("ApplicationProfileInstance.Workspace.FileAdded");
            model.ProgressStatusIsError = false;
            await LoadWorkspaceAsync(model, applicationId);
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
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.Advance(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                request.StateCode,
                request.Notes,
                request.Date,
                request.LetterFileName,
                request.LetterContent,
                request.ProcessNumber,
                request.ResultNumber,
                request.SubmissionDocumentNumber,
                request.AdditionalFiles.Count == 0
                    ? null
                    : request.AdditionalFiles.Select(file => (file.FileName, file.Content)).ToList());

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not advance progress.";
                model.ProgressStatusIsError = true;
                await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Progress advanced.";
            model.ProgressStatusIsError = false;
            model.ShowProgressRevertToHere = false;
            model.CaseTab = "progress";
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
    }

    private async Task SaveMigrationSubmissionAsync(OfficerShellCaseMigrationSubmissionRequest request)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.SaveMigrationSubmission(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                request.Date,
                request.DocumentNumber,
                request.FileName,
                request.FileContent);

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not save the migration submission.";
                model.ProgressStatusIsError = true;
                return;
            }

            objectSpace.CommitChanges();
            model.ProgressStatusMessage = "Submission saved.";
            model.ProgressStatusIsError = false;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
        }
    }

    private async Task RevertCaseProgressAsync(OfficerShellCaseProgressRevertRequest request)
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return;

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _caseProgressService
                ?? _application.ServiceProvider?.GetService<IOfficerShellCaseProgressService>()
                ?? new OfficerShellCaseProgressService();

            var result = service.Revert(
                objectSpace,
                model.CaseApplicationProfileInstanceId,
                request.StepKey);

            if (!result.Success)
            {
                model.ProgressStatusMessage = result.ErrorMessage ?? "Could not revert progress.";
                model.ProgressStatusIsError = true;
                await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
                return;
            }

            objectSpace.CommitChanges();
            model.ShowProgressRevertToHere = true;
            model.ProgressStatusMessage = string.Equals(
                request.StepKey,
                "office",
                StringComparison.OrdinalIgnoreCase)
                ? "Returned to office preparation."
                : "Progress reverted.";
            model.ProgressStatusIsError = false;
            model.CaseTab = "progress";
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.ProgressStatusMessage = ex.Message;
            model.ProgressStatusIsError = true;
            await LoadWorkspaceAsync(model, model.CaseApplicationProfileInstanceId);
        }
    }

    private async Task OpenPersonDetailAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        var personTab = model.WorkspaceSnapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
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
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return Task.CompletedTask;

        var personTab = model.WorkspaceSnapshot?.Tabs.FirstOrDefault(t => t.Key == "person");
        if (personTab == null || personTab.RowApplicationProfileInstancePersonIds.Count == 0)
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
            model.CaseApplicationProfileInstanceId,
            rowIds,
            VisaPreviewSlotViewHelper.ResolveOwnerViewId(View));

        return Task.CompletedTask;
    }

    private Task OpenResminamalarAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.CaseApplicationProfileInstanceId == Guid.Empty)
            return Task.CompletedTask;

        ApplicationWorkspaceResminamalarOpenHelper.TryOpen(
            _application,
            model.CaseApplicationProfileInstanceId,
            VisaPreviewSlotViewHelper.ResolveOwnerViewId(View));

        return Task.CompletedTask;
    }

    private async Task StartProcessAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.SelectedStagedIds.Count == 0)
            return;

        var selectedIds = model.SelectedStagedIds.ToList();
        model.SelectedStagedIds.Clear();

        try
        {
            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var service = _startProcessService
                ?? _application.ServiceProvider?.GetService<IOfficerShellStartProcessService>()
                ?? new OfficerShellStartProcessService();

            var result = service.Start(objectSpace, selectedIds);
            if (!result.Success)
            {
                model.StatusMessage = result.ErrorMessage ?? "Could not start process.";
                model.IsStatusError = true;
                return;
            }

            objectSpace.CommitChanges();

            model.StatusMessage = result.MergedCount > 1
                ? $"Started process — merged {result.MergedCount} profiles."
                : "Started process.";
            model.IsStatusError = false;

            await LoadAsync();
            await OpenCaseAsync(result.ApplicationProfileInstanceId);
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
    }

    private Task ToggleStagedSelectionAsync(Guid applicationId)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        if (!model.SelectedStagedIds.Add(applicationId))
            model.SelectedStagedIds.Remove(applicationId);

        return Task.CompletedTask;
    }

    private Task OnStagedViewModeChanged(string mode)
    {
        if (ComponentModel != null)
        {
            ComponentModel.StagedViewMode = mode;
            ComponentModel.StagedPage = 1;
        }
        return Task.CompletedTask;
    }

    private Task OnInProcessViewModeChanged(string mode)
    {
        if (ComponentModel != null)
        {
            ComponentModel.InProcessViewMode = mode;
            ComponentModel.InProcessPage = 1;
        }
        return Task.CompletedTask;
    }

    private Task OnSearchTextChanged(string text)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.SearchText = text ?? string.Empty;
        model.StagedPage = 1;
        model.InProcessPage = 1;
        return Task.CompletedTask;
    }

    private Task OnStagedFamilyFilterChanged(string key)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.StagedFamilyFilter = string.IsNullOrWhiteSpace(key) ? OfficerShellTemplateFamily.All : key;
        model.StagedPage = 1;
        return Task.CompletedTask;
    }

    private Task OnInProcessFamilyFilterChanged(string key)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.InProcessFamilyFilter = string.IsNullOrWhiteSpace(key) ? OfficerShellTemplateFamily.All : key;
        model.InProcessPage = 1;
        return Task.CompletedTask;
    }

    private Task OnStagedPageChanged(int page)
    {
        if (ComponentModel != null)
            ComponentModel.StagedPage = page;
        return Task.CompletedTask;
    }

    private Task OnStagedPageSizeChanged(int size)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.StagedPageSize = size;
        model.StagedPage = 1;
        return Task.CompletedTask;
    }

    private Task OnInProcessPageChanged(int page)
    {
        if (ComponentModel != null)
            ComponentModel.InProcessPage = page;
        return Task.CompletedTask;
    }

    private Task OnInProcessPageSizeChanged(int size)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.InProcessPageSize = size;
        model.InProcessPage = 1;
        return Task.CompletedTask;
    }

    private Task OnToggleStagedGroupCollapsed(string key)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        if (!model.StagedGroupCollapsed.Add(key))
            model.StagedGroupCollapsed.Remove(key);
        return Task.CompletedTask;
    }

    private Task OnCatalogSearchChanged(string text)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.CatalogSearchText = text ?? string.Empty;
        model.TemplatesPage = 1;
        ApplyCatalogFilter(model);
        return Task.CompletedTask;
    }

    private void ApplyCatalogFilter(OfficerShellModel model)
    {
        var q = (model.CatalogSearchText ?? string.Empty).Trim();
        model.CatalogRows = string.IsNullOrEmpty(q)
            ? _allCatalogRows
            : _allCatalogRows.Where(r =>
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (r.SelectionCode?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || r.RailLabel.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private Task OnTemplatesViewModeChanged(string mode)
    {
        if (ComponentModel != null)
        {
            ComponentModel.TemplatesViewMode = mode;
            ComponentModel.TemplatesPage = 1;
        }
        return Task.CompletedTask;
    }

    private Task OnTemplatesFamilyFilterChanged(string key)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.TemplatesFamilyFilter = string.IsNullOrWhiteSpace(key) ? OfficerShellTemplateFamily.All : key;
        model.TemplatesPage = 1;
        return Task.CompletedTask;
    }

    private Task OnTemplatesPageChanged(int page)
    {
        if (ComponentModel != null)
            ComponentModel.TemplatesPage = page;
        return Task.CompletedTask;
    }

    private Task OnTemplatesPageSizeChanged(int size)
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.TemplatesPageSize = size;
        model.TemplatesPage = 1;
        return Task.CompletedTask;
    }

    private async Task OpenTemplateDetailAsync(Guid profileId)
    {
        if (_application == null || profileId == Guid.Empty)
            return;

        var overviewView = ApplicationProfileOverviewOpenHelper.CreateOverviewView(_application, profileId);
        if (overviewView == null)
            return;

        // Keep Templates catalog page mounted (same TabbedMDI pattern as instance lists).
        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(overviewView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));

        await Task.CompletedTask;
    }

    private Task BackToTemplateCatalogAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return Task.CompletedTask;

        model.TemplatesDetailOpen = false;
        model.OverviewSnapshot = null;
        return Task.CompletedTask;
    }

    private async Task ConfigureTemplateAsync(Guid profileId)
    {
        if (profileId != Guid.Empty)
            await SelectProfileAsync(profileId);
        await ConfigureProfileAsync();
    }

    private async Task SelectProfileAsync(Guid profileId)
    {
        var model = ComponentModel;
        if (model == null || _application == null || profileId == Guid.Empty)
            return;

        model.SelectedProfileId = profileId;
        model.IsOverviewLoading = true;

        try
        {
            var overviewService = _overviewQueryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfileOverviewQueryService>()
                ?? new ApplicationProfileOverviewQueryService();

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

        // New tab so Application Profile Templates list stays mounted (same as catalog PE).
        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }

    private Task ConfigureProfileAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null || model.SelectedProfileId == Guid.Empty)
            return Task.CompletedTask;

        var wizardView = ApplicationProfileWizardOpenHelper.CreateWizardView(_application, model.SelectedProfileId);
        if (wizardView == null)
            return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(_application.MainWindow, null));

        return Task.CompletedTask;
    }
}
