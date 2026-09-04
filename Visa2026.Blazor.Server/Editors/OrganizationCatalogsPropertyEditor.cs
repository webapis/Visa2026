#nullable enable
using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.OrganizationCatalogs;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), OrganizationCatalogsEditorAliases.Catalogs, false)]
public class OrganizationCatalogsPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;

    public OrganizationCatalogsPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override OrganizationCatalogsModel ComponentModel => (OrganizationCatalogsModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application) =>
        _application = application;

    protected override IComponentModel CreateComponentModel() => new OrganizationCatalogsModel
    {
        IsLoading = true,
        InitialLoadRequested = EventCallback.Factory.Create(this, LoadAsync),
        NewRequested = EventCallback.Factory.Create<string>(this, NewAsync),
        EditRequested = EventCallback.Factory.Create<(string Kind, Guid Id)>(this, EditAsync),
        MakeDefaultRequested = EventCallback.Factory.Create<(string Kind, Guid Id)>(this, MakeDefaultAsync),
    };

    protected override void OnCurrentObjectChanged()
    {
        base.OnCurrentObjectChanged();
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
            if (_application == null)
            {
                model.StatusMessage = "Organization catalogs host is not ready.";
                model.IsStatusError = true;
                return;
            }

            using var objectSpace = _application.CreateObjectSpace(typeof(CompanyProfile));
            model.CompanyRows = OrganizationCatalogHelper.ListCompanyRows(objectSpace);
            model.SignatoryRows = OrganizationCatalogHelper.ListSignatoryRows(objectSpace);
            model.RepresentativeRows = OrganizationCatalogHelper.ListRepresentativeRows(objectSpace);
        }
        finally
        {
            model.IsLoading = false;
        }
    }

    private Task NewAsync(string kind) =>
        OpenEditor(kind, Guid.Empty);

    private Task EditAsync((string Kind, Guid Id) request) =>
        OpenEditor(request.Kind, request.Id);

    private Task OpenEditor(string kind, Guid id)
    {
        if (_application == null)
            return Task.CompletedTask;

        OrganizationCatalogsOpenHelper.TryOpenEditor(
            _application,
            kind,
            id,
            onClosed: () => _ = LoadAsync(),
            onSaved: _saved => { _ = LoadAsync(); });
        return Task.CompletedTask;
    }

    private async Task MakeDefaultAsync((string Kind, Guid Id) request)
    {
        var model = ComponentModel;
        if (model == null || _application == null)
            return;

        using var objectSpace = _application.CreateObjectSpace(typeof(CompanyProfile));
        if (!OrganizationCatalogHelper.TryMakeDefault(objectSpace, request.Kind, request.Id, out var error))
        {
            model.StatusMessage = error;
            model.IsStatusError = true;
            return;
        }

        model.StatusMessage = null;
        model.IsStatusError = false;
        await LoadAsync();
    }
}
