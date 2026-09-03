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
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationProfilePickerEditorAliases.Picker, false)]
public class ApplicationProfilePickerPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationProfilePickerQueryService? _queryService;
    private IApplicationProfilePickerContext? _context;
    private IApprovalLegCatalogChangeNotifier? _catalogChanged;

    public ApplicationProfilePickerPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationProfilePickerModel ComponentModel => (ApplicationProfilePickerModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IApplicationProfilePickerQueryService>();
        _context = application.ServiceProvider?.GetService<IApplicationProfilePickerContext>();
        _catalogChanged = application.ServiceProvider?.GetService<IApprovalLegCatalogChangeNotifier>();
        if (_catalogChanged != null)
        {
            _catalogChanged.Changed -= OnApprovalLegCatalogChanged;
            _catalogChanged.Changed += OnApprovalLegCatalogChanged;
        }
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationProfilePickerModel
    {
        IsLoading = true,
        Step = 1,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        UseProfileRequested = EventCallback.Factory.Create(this, UseProfileAsync),
        NextStepRequested = EventCallback.Factory.Create(this, NextStepAsync),
        BackStepRequested = EventCallback.Factory.Create(this, BackStepAsync),
        SelectProfileRequested = EventCallback.Factory.Create<Guid>(this, SelectProfile),
        SelectVersionRequested = EventCallback.Factory.Create<Guid>(this, SelectVersion),
        NewApprovalLegRequested = EventCallback.Factory.Create(this, OpenNewApprovalLeg),
        OpenApprovalLegRequested = EventCallback.Factory.Create<Guid>(this, OpenApprovalLeg),
        OpenApprovalLegCatalogRequested = EventCallback.Factory.Create(this, OpenApprovalLegCatalog),
        MakeDefaultApprovalLegRequested = EventCallback.Factory.Create<Guid>(this, SetDefaultApprovalLeg),
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

    private Task LoadAsync() => LoadAsync(showLoading: true);

    private async Task LoadAsync(bool showLoading)
    {
        var model = ComponentModel;
        if (model == null)
            return;

        if (showLoading)
            model.IsLoading = true;
        model.StatusMessage = null;
        model.IsStatusError = false;
        model.IsStatusWarning = false;
        if (showLoading)
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

            model.RouteHint = route.HasValue
                ? $"Showing profiles for {ApplicationProfilePickerDisplayHelper.FormatProgressRoute(route.Value)}."
                : "Choose a profile — configuration applies live; per-ApplicationProfileInstance values get defaults at create.";

            var rows = queryService.GetProfiles(objectSpace, route, seedPersonId: null);
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

        var selected = model.Rows.FirstOrDefault(r => r.ProfileId == model.SelectedProfileId);
        if (selected == null || !selected.RequiresApprovalLegVersion)
        {
            await UseProfileAsync();
            return;
        }

        EnsureSelectedVersion(model);
        model.Step = 2;
        model.StatusMessage = null;
        model.IsStatusError = false;
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

        var selected = model.Rows.FirstOrDefault(r => r.ProfileId == model.SelectedProfileId);
        if (selected?.RequiresApprovalLegVersion == true && model.Step != 2)
        {
            await NextStepAsync();
            return;
        }

        if (selected?.RequiresApprovalLegVersion == true && model.SelectedVersionId == Guid.Empty)
        {
            model.StatusMessage = "Select an approval-leg version.";
            model.IsStatusError = true;
            return;
        }

        await Task.Delay(16);

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

    private async Task SetDefaultApprovalLeg(Guid versionId)
    {
        var model = ComponentModel;
        if (model == null || _application == null || versionId == Guid.Empty)
            return;

        var factory = _application.ServiceProvider?.GetService<INonSecuredObjectSpaceFactory>();
        if (factory == null)
        {
            model.StatusMessage = "Could not set the default approval-leg chain.";
            model.IsStatusError = true;
            return;
        }

        using var objectSpace = factory.CreateNonSecuredObjectSpace<ApplicationProfile>();
        if (!ApplicationProfileApprovalLegVersionHelper.TrySetTemplateDefault(
                objectSpace,
                model.SelectedProfileId,
                versionId,
                out var error))
        {
            model.StatusMessage = error ?? "Could not set the default approval-leg chain.";
            model.IsStatusError = true;
            return;
        }

        var step = model.Step;
        await LoadAsync(showLoading: false);
        model.Step = step;
        model.SelectedVersionId = versionId;
        EnsureSelectedVersion(model);
    }

    private void OpenApprovalLegCatalog()
    {
        if (_application == null)
            return;

        ApplicationProfileWizardApprovalLegCatalogOpenHelper.TryOpen(
            _application,
            onChanged: OnApprovalLegCatalogChanged,
            ownerViewId: VisaPreviewSlotViewHelper.ResolveOwnerViewId(View),
            request: new ApprovalLegCatalogSlotRequest());
    }

    private void OpenNewApprovalLeg()
    {
        if (_application == null)
            return;

        ApplicationProfileWizardApprovalLegCatalogOpenHelper.TryOpen(
            _application,
            onChanged: OnApprovalLegCatalogChanged,
            ownerViewId: VisaPreviewSlotViewHelper.ResolveOwnerViewId(View),
            request: new ApprovalLegCatalogSlotRequest { StartNew = true });
    }

    private void OpenApprovalLeg(Guid versionId)
    {
        if (_application == null || versionId == Guid.Empty)
            return;

        ApplicationProfileWizardApprovalLegCatalogOpenHelper.TryOpen(
            _application,
            onChanged: OnApprovalLegCatalogChanged,
            ownerViewId: VisaPreviewSlotViewHelper.ResolveOwnerViewId(View),
            request: new ApprovalLegCatalogSlotRequest { FocusProfileId = versionId });
    }

    private void OnApprovalLegCatalogChanged()
    {
        _ = ReloadAfterCatalogChangeAsync();
    }

    private async Task ReloadAfterCatalogChangeAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        var step = model.Step;
        var preferId = _catalogChanged?.LastChangedProfileId;
        await LoadAsync(showLoading: false);
        model.Step = step;
        if (preferId is Guid id && id != Guid.Empty)
            model.SelectedVersionId = id;
        EnsureSelectedVersion(model);
    }
}
