using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Blazor.Server.Localization;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
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

        if (CurrentObject is not ApplicationReportPackageListHost host || _application == null || host.ApplicationProfileInstanceId == Guid.Empty)
        {
            ComponentModel.ApplicationProfileInstanceId = Guid.Empty;
            ComponentModel.ApplicationNumber = string.Empty;
            ComponentModel.PackageScope = WordReportPackageScope.ApplicationProfileInstance;
            ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
            ComponentModel.CatalogEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.RecycleBinEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.SharedEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.ShowRecycleBin = false;
            return;
        }

        using var appObjectSpace = _application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var application = appObjectSpace.GetObjectByKey<ApplicationProfileInstance>(host.ApplicationProfileInstanceId);
        if (application == null)
        {
            ComponentModel.ApplicationProfileInstanceId = Guid.Empty;
            ComponentModel.ApplicationNumber = string.Empty;
            ComponentModel.PackageScope = WordReportPackageScope.ApplicationProfileInstance;
            ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
            ComponentModel.CatalogEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.RecycleBinEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.SharedEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
            ComponentModel.ShowRecycleBin = false;
            return;
        }

        var context = WordReportGenerationContext.ForApplication();
        var catalogService = _application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(appObjectSpace, application, context);

        ComponentModel.ApplicationProfileInstanceId = host.ApplicationProfileInstanceId;
        ComponentModel.ApplicationNumber = application.FullApplicationNumber ?? string.Empty;
        ComponentModel.PackageScope = WordReportPackageScope.ApplicationProfileInstance;
        ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
        ComponentModel.CatalogEntries = catalog.Entries;
        ComponentModel.RecycleBinEntries = catalog.RecycleBinEntries;
        ComponentModel.SharedEntries = catalog.SharedEntries;
        ComponentModel.ShowRecycleBin = catalog.HasProfileNestedCatalog;
    }

    private string ResolveUiCultureName() =>
        _application?.ServiceProvider == null
            ? VisaUiCultureResolver.Resolve()
            : VisaUiCultureResolver.Resolve(_application.ServiceProvider);

    private Task OnRefreshAsync()
    {
        ReadValueCore();
        return Task.CompletedTask;
    }
}
