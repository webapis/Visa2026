using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ApplicationItemDocumentCopiesEditorAliases.ListPanel, false)]
public sealed class ApplicationItemDocumentCopiesListPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication _application;

    public ApplicationItemDocumentCopiesListPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override ApplicationItemDocumentCopiesModel ComponentModel =>
        (ApplicationItemDocumentCopiesModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
    }

    protected override IComponentModel CreateComponentModel()
    {
        var model = new ApplicationItemDocumentCopiesModel();
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

        if (CurrentObject is not ApplicationItemDocumentCopiesListHost host || _application == null)
        {
            ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
            ComponentModel.MergedGroups = Array.Empty<ApplicationItemLinkedDocumentMergedGroup>();
            return;
        }

        var itemIds = DeserializeItemIds(host.ItemIdsJson);
        if (itemIds.Count == 0)
        {
            ComponentModel.ApplicationItemIds = Array.Empty<Guid>();
            ComponentModel.MergedGroups = Array.Empty<ApplicationItemLinkedDocumentMergedGroup>();
            return;
        }

        using var itemObjectSpace = _application.CreateObjectSpace(typeof(ApplicationItem));
        var items = itemIds
            .Select(id => itemObjectSpace.GetObjectByKey<ApplicationItem>(id))
            .Where(item => item != null)
            .Cast<ApplicationItem>()
            .ToList();

        ComponentModel.ApplicationItemIds = items.Select(item => item.ID).ToList();
        var lines = ApplicationItemLinkedDocumentsResolver.ResolveMany(itemObjectSpace, items);
        ComponentModel.MergedGroups = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);
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
