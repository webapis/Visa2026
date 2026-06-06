using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationReportPackageEditorAliases.ListPanel, false)]
public sealed class ApplicationReportPackageListPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication _application;

    public ApplicationReportPackageListPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override ApplicationReportPackageModel ComponentModel =>
        (ApplicationReportPackageModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
    }

    protected override IComponentModel CreateComponentModel()
    {
        var model = new ApplicationReportPackageModel();
        model.RefreshRequested = EventCallback.Factory.Create(this, OnRefreshAsync);
        return model;
    }

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
        ReadValueCore();
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null)
            return;

        ComponentModel.UiCultureName = ResolveUiCultureName();

        if (CurrentObject is not ApplicationReportPackageListHost host || _application == null || host.ApplicationId == Guid.Empty)
        {
            ComponentModel.ApplicationId = Guid.Empty;
            ComponentModel.ApplicationNumber = string.Empty;
            ComponentModel.CatalogEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            return;
        }

        using var appObjectSpace = _application.CreateObjectSpace(typeof(Application));
        var application = appObjectSpace.GetObjectByKey<Application>(host.ApplicationId);
        if (application == null)
        {
            ComponentModel.ApplicationId = Guid.Empty;
            ComponentModel.ApplicationNumber = string.Empty;
            ComponentModel.CatalogEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            return;
        }

        var catalogService = _application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(appObjectSpace, application);

        ComponentModel.ApplicationId = host.ApplicationId;
        ComponentModel.ApplicationNumber = application.FullApplicationNumber ?? string.Empty;
        ComponentModel.CatalogEntries = catalog.Entries;
    }

    private string ResolveUiCultureName()
    {
        if (_application?.ServiceProvider == null)
        {
            return VisaUiMessages.NormalizeCultureName(System.Globalization.CultureInfo.CurrentUICulture.Name);
        }

        var cultureService = _application.ServiceProvider.GetService<IXafCultureInfoService>();
        string cultureName = cultureService?.CurrentUICulture.Name
            ?? System.Globalization.CultureInfo.CurrentUICulture.Name;
        return VisaUiMessages.NormalizeCultureName(cultureName);
    }

    private Task OnRefreshAsync()
    {
        ReadValueCore();
        return Task.CompletedTask;
    }
}
