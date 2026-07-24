using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.Model;

/// <summary>
/// Person detail nested collections that mirror application workflow output are browse-only on the person form.
/// </summary>
public sealed class PersonNestedListViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    /// <summary>Nested path: type lives on the parent <c>Application</c>, not on <c>ApplicationItem</c>.</summary>
    internal const string ApplicationTypePropertyName = "Application.ApplicationType";

    /// <summary>Nested path: date lives on the parent <c>Application</c>, not on <c>ApplicationItem</c>.</summary>
    internal const string ApplicationDatePropertyName = "Application.ApplicationDate";

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in PersonNestedCollectionLayout.ReadOnlyNestedListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            ConfigureReadOnlyNestedListView(listView);
        }

        if (views[PersonNestedCollectionLayout.ApplicationItemsListView] is IModelListView applicationItemsListView)
            EnsureApplicationDateAndTypeColumns(applicationItemsListView);
    }

    internal static void ConfigureReadOnlyNestedListView(IModelListView listView)
    {
        listView.AllowNew = false;
        listView.AllowDelete = false;
        listView.AllowEdit = false;
        listView.AllowLink = false;
        listView.AllowUnlink = false;
    }

    /// <summary>
    /// Officers need application date and type on Person → Application items (issued);
    /// items from different applications differ.
    /// </summary>
    internal static void EnsureApplicationDateAndTypeColumns(IModelListView listView)
    {
        var applicationColumn = listView.Columns["Application"];
        var baseIndex = applicationColumn?.Index is int applicationIndex and >= 0
            ? applicationIndex
            : 1;

        var dateColumn = listView.Columns[ApplicationDatePropertyName]
            ?? listView.Columns.AddNode<IModelColumn>(ApplicationDatePropertyName);
        dateColumn.PropertyName = ApplicationDatePropertyName;
        dateColumn.Index = baseIndex + 1;

        var typeColumn = listView.Columns[ApplicationTypePropertyName]
            ?? listView.Columns.AddNode<IModelColumn>(ApplicationTypePropertyName);
        typeColumn.PropertyName = ApplicationTypePropertyName;
        typeColumn.Index = baseIndex + 2;
    }
}