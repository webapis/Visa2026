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
            if (profile != null && _session?.ObjectSpace != null)
                ApplicationProfileProgressStateSeeder.EnsureDefaults(profile, _session.ObjectSpace);

            model.IsReadOnly = profile != null
                && ApplicationProfileLockHelper.IsProfileConfigLocked(profile, _session!.ObjectSpace);
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private async Task PublishAsync()
    {
        var model = ComponentModel;
        if (model == null || _session?.ObjectSpace == null)
            return;

        var profile = _session.GetProfile();
        if (profile == null)
        {
            model.StatusMessage = "Application Profile not found.";
            model.IsStatusError = true;
            return;
        }

        try
        {
            ApplicationProfileLockHelper.EnsureConfigurationEditable(profile, _session.ObjectSpace);
            _session.ObjectSpace.CommitChanges();
            model.StatusMessage = "Profile saved.";
            model.IsStatusError = false;
            model.IsReadOnly = ApplicationProfileLockHelper.IsProfileConfigLocked(profile, _session.ObjectSpace);
        }
        catch (UserFriendlyException ex)
        {
            model.StatusMessage = ex.Message;
            model.IsStatusError = true;
        }
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
        _session.ApplicationProfileId = profileId;

        if (_session.ObjectSpace != null)
            return;

        _session.ObjectSpace = _application.CreateObjectSpace(typeof(ApplicationProfile));
    }
}
