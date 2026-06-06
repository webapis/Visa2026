using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

[PropertyEditor(typeof(string), ApplicationItemReportPackageEditorAliases.ListPanel, false)]
public sealed class ApplicationItemReportPackageListPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication _application;

    public ApplicationItemReportPackageListPropertyEditor(Type objectType, IModelMemberViewItem model)
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

        if (CurrentObject is not ApplicationItemReportPackageListHost host || _application == null)
        {
            ResetComponentModel();
            return;
        }

        var itemIds = DeserializeItemIds(host.ItemIdsJson);
        if (itemIds.Count == 0)
        {
            ResetComponentModel();
            return;
        }

        using var itemObjectSpace = _application.CreateObjectSpace(typeof(ApplicationItem));
        if (!ApplicationItemReportPackageValidation.TryResolveApplication(
                itemObjectSpace,
                itemIds,
                out var application,
                out _))
        {
            ResetComponentModel();
            return;
        }

        var context = WordReportGenerationContext.ForApplicationItems(itemIds);
        var catalogService = _application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(itemObjectSpace, application!, context);

        ComponentModel.ApplicationId = application!.ID;
        ComponentModel.ApplicationNumber = application.FullApplicationNumber ?? string.Empty;
        ComponentModel.PackageScope = WordReportPackageScope.ApplicationItem;
        ComponentModel.ApplicationItemIds = itemIds;
        ComponentModel.CatalogEntries = catalog.Entries;
    }

    private void ResetComponentModel()
    {
        ComponentModel.ApplicationId = Guid.Empty;
        ComponentModel.ApplicationNumber = string.Empty;
        ComponentModel.PackageScope = WordReportPackageScope.ApplicationItem;
        ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
        ComponentModel.CatalogEntries = Array.Empty<ApplicationWordReportPackageCatalogEntry>();
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

    private static IReadOnlyList<Guid> DeserializeItemIds(string? itemIdsJson)
    {
        if (string.IsNullOrWhiteSpace(itemIdsJson))
            return Array.Empty<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(itemIdsJson)?
                       .Where(id => id != Guid.Empty)
                       .Distinct()
                       .ToList()
                   ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }

    private Task OnRefreshAsync()
    {
        ReadValueCore();
        return Task.CompletedTask;
    }
}
