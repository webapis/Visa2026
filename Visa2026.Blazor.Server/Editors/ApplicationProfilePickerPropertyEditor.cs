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
using Visa2026.Module.BusinessObjects.ApplicationProfilePicker;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationProfilePickerEditorAliases.Picker, false)]
public class ApplicationProfilePickerPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationProfilePickerQueryService? _queryService;
    private IApplicationProfilePickerContext? _context;

    public ApplicationProfilePickerPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationProfilePickerModel ComponentModel => (ApplicationProfilePickerModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationProfilePickerQueryService>();
        _context = application.ServiceProvider?.GetService<IApplicationProfilePickerContext>();
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationProfilePickerModel
    {
        IsLoading = true,
        Step = 1,
        SelectedPersonIds = new HashSet<Guid>(),
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        UseProfileRequested = EventCallback.Factory.Create(this, UseProfileAsync),
        NextStepRequested = EventCallback.Factory.Create(this, NextStepAsync),
        BackStepRequested = EventCallback.Factory.Create(this, BackStepAsync),
        SelectProfileRequested = EventCallback.Factory.Create<Guid>(this, SelectProfile),
        SelectVersionRequested = EventCallback.Factory.Create<Guid>(this, SelectVersion),
        TogglePersonRequested = EventCallback.Factory.Create<Guid>(this, TogglePerson),
        DuplicateWarningAcknowledgedChanged = EventCallback.Factory.Create<bool>(this, SetDuplicateAcknowledged),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();

        var model = ComponentModel;
        if (model == null || model.IsLoading)
            return;

        _ = LoadAsync();
    }

    private ApplicationProfilePickerOpenContext? OpenContext =>
        _context?.Context ?? (_application != null
            ? ApplicationProfilePickerContextGate.Get(_application)
            : null);

    private bool IsPersonStartFlow =>
        OpenContext?.SeedPersonId is Guid id && id != Guid.Empty;

    private async Task LoadAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.IsLoading = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.IsStatusWarning = false;
        await Task.Delay(16);

        try
        {
            if (_application == null)
            {
                model.StatusMessage = "ApplicationProfileInstance host is not ready. Close and reopen the picker.";
                model.IsStatusError = true;
                return;
            }

            var queryService = _queryService
                ?? _application.ServiceProvider?.GetService<IApplicationProfilePickerQueryService>();
            if (queryService == null)
            {
                model.StatusMessage = "Application Profile picker service is not registered.";
                model.IsStatusError = true;
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
            var openContext = OpenContext;
            var route = openContext?.CreationProgressRoute;
            var seedPersonId = openContext?.SeedPersonId;

            model.IsPersonStartFlow = IsPersonStartFlow;
            model.RouteHint = route.HasValue
                ? $"Showing profiles for {ApplicationProfilePickerDisplayHelper.FormatProgressRoute(route.Value)}."
                : IsPersonStartFlow
                    ? "Pick a profile, then choose who joins this ApplicationProfileInstance roster."
                    : "Choose a profile — configuration applies live; per-ApplicationProfileInstance values get defaults at create.";

            if (seedPersonId is Guid personId && personId != Guid.Empty)
            {
                var seed = objectSpace.GetObjectByKey<Person>(personId);
                model.SeedPersonLabel = seed?.FullName;
            }
            else
            {
                model.SeedPersonLabel = null;
            }

            var rows = queryService.GetProfiles(objectSpace, route, seedPersonId: seedPersonId);
            model.Rows = rows.Select(r => new ApplicationProfilePickerModel.PickerRowModel
            {
                ProfileId = r.ProfileId,
                Name = r.Name,
                MetaLine = r.MetaLine,
                SeedUsageLine = r.SeedUsageLine,
                IsConfigLocked = r.IsConfigLocked,
                HasOpenApplicationForSeedPerson = r.HasOpenApplicationForSeedPerson,
                RequiresApprovalLegVersion = r.RequiresApprovalLegVersion,
                MissingApprovalLegVersions = r.ProgressRoute
                    == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
                    && r.ApprovalLegVersions.Count == 0,
                ApprovalLegVersions = r.ApprovalLegVersions.Select(v => new ApplicationProfilePickerModel.VersionOptionModel
                {
                    VersionId = v.VersionId,
                    Name = v.Name,
                    IsDefault = v.IsDefault,
                    MinistryNames = v.MinistryNames,
                }).ToList(),
            }).ToList();

            if (model.SelectedProfileId == Guid.Empty && model.Rows.Count > 0)
                model.SelectedProfileId = model.Rows[0].ProfileId;

            EnsureSelectedVersion(model);

            if (model.Step == 2 && IsPersonStartFlow)
                LoadPeopleStep(model, objectSpace);
        }
        catch (Exception ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
        finally
        {
            model.IsLoading = false;
            UpdatePeopleStepFlags(model);
        }
    }

    private async Task NextStepAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        if (model.SelectedProfileId == Guid.Empty)
        {
            model.StatusMessage = "Select an Application Profile first.";
            model.IsStatusError = true;
            return;
        }

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(model.SelectedProfileId);
        var seedPersonId = OpenContext?.SeedPersonId;
        var seed = seedPersonId is Guid id ? objectSpace.GetObjectByKey<Person>(id) : null;

        if (profile != null && seed != null
            && profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
            && (seed.ProjectContract == null || seed.ProjectContract.ID == Guid.Empty))
        {
            model.StatusMessage =
                "This profile is via ministry — set a Project contract on the person before starting.";
            model.IsStatusError = true;
            return;
        }

        model.Step = 2;
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.DuplicateWarningAcknowledged = false;
        LoadPeopleStep(model, objectSpace);
        UpdatePeopleStepFlags(model);
        await Task.Delay(16);
    }

    private async Task BackStepAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.Step = 1;
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.IsStatusWarning = false;
        await Task.Delay(16);
    }

    private void LoadPeopleStep(ApplicationProfilePickerModel model, IObjectSpace objectSpace)
    {
        var seedPersonId = OpenContext?.SeedPersonId;
        if (seedPersonId is not Guid personId || personId == Guid.Empty)
            return;

        var seed = objectSpace.GetObjectByKey<Person>(personId);
        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(model.SelectedProfileId);
        if (seed == null || profile == null)
            return;

        var candidates = ApplicationStartFromPersonHelper.GetPeopleCandidates(objectSpace, seed, profile);
        if (model.SelectedPersonIds.Count == 0)
        {
            foreach (var c in candidates.Where(c => c.IsPreSelected))
                model.SelectedPersonIds.Add(c.PersonId);
        }

        model.PeopleRows = candidates.Select(c => new ApplicationProfilePickerModel.PeopleRowModel
        {
            PersonId = c.PersonId,
            FullName = c.FullName,
            RoleLabel = c.RoleLabel,
            PersonalNumber = c.PersonalNumber,
            IsSeedPerson = c.IsSeedPerson,
            IsSuggestedFamily = c.IsSuggestedFamily,
            IsSelected = model.SelectedPersonIds.Contains(c.PersonId),
        }).ToList();
    }

    private void SetDuplicateAcknowledged(bool acknowledged)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.DuplicateWarningAcknowledged = acknowledged;
        UpdatePeopleStepFlags(model);
    }

    private void TogglePerson(Guid personId)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        if (model.SelectedPersonIds.Contains(personId))
            model.SelectedPersonIds.Remove(personId);
        else
            model.SelectedPersonIds.Add(personId);

        model.PeopleRows = model.PeopleRows
            .Select(r => r with { IsSelected = model.SelectedPersonIds.Contains(r.PersonId) })
            .ToList();

        model.DuplicateWarningAcknowledged = false;
        UpdatePeopleStepFlags(model);
    }

    private void UpdatePeopleStepFlags(ApplicationProfilePickerModel? model)
    {
        if (model == null || _application == null || !IsPersonStartFlow || model.Step != 2)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(model.SelectedProfileId);
        if (profile == null)
            return;

        model.HasDuplicateWarning = model.SelectedPersonIds
            .Select(id => objectSpace.GetObjectByKey<Person>(id))
            .Where(p => p != null)
            .Any(p => ApplicationStartFromPersonHelper.HasOpenApplication(objectSpace, p!, profile));

        model.CanCreateFromPeople = model.SelectedPersonIds.Count > 0
            && (!model.HasDuplicateWarning || model.DuplicateWarningAcknowledged);
    }

    private async Task UseProfileAsync()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        if (model.SelectedProfileId == Guid.Empty)
        {
            model.StatusMessage = "Select an Application Profile first.";
            model.IsStatusError = true;
            return;
        }

        await Task.Delay(16);

        if (IsPersonStartFlow)
        {
            if (!ApplicationProfilePickerCompletionHelper.TryCreateApplicationFromPersonStart(
                    _application,
                    model.SelectedProfileId,
                    model.SelectedPersonIds.ToList(),
                    model.SelectedVersionId == Guid.Empty ? null : model.SelectedVersionId,
                    out var errorMessage,
                    out var successMessage))
            {
                model.StatusMessage = errorMessage;
                model.IsStatusError = true;
                return;
            }

            model.StatusMessage = successMessage;
            model.IsStatusError = false;
            model.IsStatusWarning = !string.IsNullOrWhiteSpace(successMessage)
                && successMessage.Contains("open Application", StringComparison.OrdinalIgnoreCase);
            return;
        }

        if (!ApplicationProfilePickerCompletionHelper.TryCreateApplication(
                _application,
                model.SelectedProfileId,
                model.SelectedVersionId == Guid.Empty ? null : model.SelectedVersionId,
                out var createError))
        {
            model.StatusMessage = createError;
            model.IsStatusError = true;
            return;
        }

        model.StatusMessage = null;
        model.IsStatusError = false;
    }

    private void SelectProfile(Guid profileId)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedProfileId = profileId;
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.IsStatusWarning = false;
        EnsureSelectedVersion(model);
    }

    private void SelectVersion(Guid versionId)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.SelectedVersionId = versionId;
        model.StatusMessage = null;
        model.IsStatusError = false;
    }

    private static void EnsureSelectedVersion(ApplicationProfilePickerModel model)
    {
        var selected = model.Rows.FirstOrDefault(r => r.ProfileId == model.SelectedProfileId);
        if (selected == null || !selected.RequiresApprovalLegVersion)
        {
            model.SelectedVersionId = Guid.Empty;
            return;
        }

        if (selected.ApprovalLegVersions.Any(v => v.VersionId == model.SelectedVersionId))
            return;

        var defaultVersion = selected.ApprovalLegVersions.FirstOrDefault(v => v.IsDefault)
            ?? selected.ApprovalLegVersions.FirstOrDefault();
        model.SelectedVersionId = defaultVersion?.VersionId ?? Guid.Empty;
    }
}
