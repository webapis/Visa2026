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
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileWizard;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationProfileWizardEditorAliases.Wizard, false)]
public class ApplicationProfileWizardPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IApplicationProfileWizardSession? _session;

    public ApplicationProfileWizardPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ApplicationProfileWizardModel ComponentModel => (ApplicationProfileWizardModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _session = application.ServiceProvider?.GetService<IApplicationProfileWizardSession>();
    }

    protected override IComponentModel CreateComponentModel() => new ApplicationProfileWizardModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        PublishRequested = EventCallback.Factory.Create(this, PublishAsync),
        OpenCompanyRequested = EventCallback.Factory.Create(this, () => OpenOrganization(ApplicationProfileWizardOrganizationOpenHelper.Kind.Company)),
        OpenSignatoryRequested = EventCallback.Factory.Create(this, () => OpenOrganization(ApplicationProfileWizardOrganizationOpenHelper.Kind.Signatory)),
        OpenRepresentativeRequested = EventCallback.Factory.Create(this, () => OpenOrganization(ApplicationProfileWizardOrganizationOpenHelper.Kind.Representative)),
        RefreshOrganizationRequested = EventCallback.Factory.Create(this, RefreshSupportingData),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ApplyProfileIdFromContext();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var model = ComponentModel;
        if (model == null)
            return;

        model.IsLoading = true;
        model.StatusMessage = null;
        await Task.Delay(16);

        try
        {
            ApplyProfileIdFromContext();
            var profileId = ResolveProfileId();
            EnsureProfileObjectSpace(profileId);

            var profile = _session?.GetProfile();

            model.ObjectSpace = _session?.ObjectSpace;
            model.Profile = profile;
            model.IsReadOnly = profile != null
                && _session?.ObjectSpace != null
                && ApplicationProfileLockHelper.IsProfileConfigLocked(profile, _session.ObjectSpace);

            RefreshSupportingData();
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private Task PublishAsync()
    {
        var model = ComponentModel;
        if (model == null || _session?.ObjectSpace == null)
            return Task.CompletedTask;

        var profile = model.Profile ?? _session.GetProfile();
        if (profile == null)
        {
            model.StatusMessage = "Application Profile not found.";
            model.IsStatusError = true;
            return Task.CompletedTask;
        }

        try
        {
            ApplicationProfileWizardPersistHelper.Save(_session.ObjectSpace, profile);
            model.Profile = _session.GetProfile() ?? profile;
            model.StatusMessage = "Profile saved.";
            model.IsStatusError = false;
            model.IsReadOnly = ApplicationProfileLockHelper.IsProfileConfigLocked(profile, _session.ObjectSpace);
        }
        catch (UserFriendlyException ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            model.StatusMessage = ApplicationProfileWizardPersistHelper.FormatCommitError(ex);
            model.IsStatusError = true;
            return Task.CompletedTask;
        }

        return RefreshCatalogAsync();
    }

    private Task RefreshCatalogAsync()
    {
        var reload = _application?.ServiceProvider?.GetService<IApplicationProfileCatalogReload>();
        return reload?.RequestReloadAsync() ?? Task.CompletedTask;
    }

    private void ApplyProfileIdFromContext()
    {
        if (CurrentObject is not ApplicationProfileWizardHost host || host.ApplicationProfileId != Guid.Empty)
            return;

        var pending = _application != null
            ? ApplicationProfileWizardPendingOpenGate.Get(_application)
            : Guid.Empty;
        if (pending != Guid.Empty)
            host.ApplicationProfileId = pending;
    }

    private Guid ResolveProfileId()
    {
        ApplyProfileIdFromContext();
        if (CurrentObject is ApplicationProfileWizardHost host && host.ApplicationProfileId != Guid.Empty)
            return host.ApplicationProfileId;

        return _application != null
            ? ApplicationProfileWizardPendingOpenGate.Get(_application)
            : Guid.Empty;
    }

    private void EnsureProfileObjectSpace(Guid profileId)
    {
        if (_session == null || _application == null)
            return;

        _session.Application = _application;

        var canReuse = _session.ObjectSpace is { IsDisposed: false }
            && _session.ApplicationProfileId == profileId
            && (profileId == Guid.Empty || _session.GetProfile() != null);
        if (canReuse)
            return;

        if (_session.ObjectSpace is { IsDisposed: false } previous)
            previous.Dispose();

        _session.ObjectSpace = null;
        _session.ApplicationProfileId = profileId;
        if (profileId == Guid.Empty)
            return;

        _session.ObjectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
    }

    private void RefreshSupportingData()
    {
        RefreshOrganizationSnapshot();
        RefreshLookupData();
    }

    private void RefreshOrganizationSnapshot()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(CompanyProfile));
        model.OrganizationSnapshot = ApplicationProfileWizardOrganizationSnapshot.Load(objectSpace);
    }

    private void RefreshLookupData()
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(VisaType));
        model.Lookups = ApplicationProfileWizardLookupData.Load(objectSpace);
    }

    private void OpenOrganization(ApplicationProfileWizardOrganizationOpenHelper.Kind kind)
    {
        if (_application == null)
            return;

        ApplicationProfileWizardOrganizationOpenHelper.TryOpen(
            _application,
            kind,
            RefreshOrganizationSnapshot);
    }
}
