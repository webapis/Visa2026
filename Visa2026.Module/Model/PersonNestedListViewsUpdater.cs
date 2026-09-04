using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.Model;

/// <summary>
/// Person detail nested collections that mirror application workflow output are browse-only on the person form.
/// </summary>
public sealed class PersonNestedListViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    internal const string ApplicationDatePropertyName = "ApplicationDate";
    internal const string ApplicationProfilePropertyName = "ApplicationProfile";
    internal const string ApplicationTypePropertyName = "ApplicationType";

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in PersonNestedCollectionLayout.ReadOnlyNestedListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            ConfigureReadOnlyNestedListView(listView);
        }

        if (views[PersonNestedCollectionLayout.ApplicationProfileInstancesListView] is IModelListView applicationPeopleListView)
            EnsureApplicationDateAndProfileColumns(applicationPeopleListView);
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
    /// Officers need application date and profile on Person → Profile instances (linked);
    /// rows from different applications differ.
    /// </summary>
    internal static void EnsureApplicationDateAndProfileColumns(IModelListView listView)
    {
        var applicationColumn = listView.Columns["ApplicationNumber"]
            ?? listView.Columns["FullApplicationNumber"];
        var baseIndex = applicationColumn?.Index is int applicationIndex and >= 0
            ? applicationIndex
            : 1;

        var dateColumn = listView.Columns[ApplicationDatePropertyName]
            ?? listView.Columns.AddNode<IModelColumn>(ApplicationDatePropertyName);
        dateColumn.PropertyName = ApplicationDatePropertyName;
        dateColumn.Index = baseIndex + 1;

        var profileColumn = listView.Columns[ApplicationProfilePropertyName]
            ?? listView.Columns.AddNode<IModelColumn>(ApplicationProfilePropertyName);
        profileColumn.PropertyName = ApplicationProfilePropertyName;
        profileColumn.Index = baseIndex + 2;
    }

    /// <summary>Legacy ApplicationRosterMergeLine nested list (slice 10 close-out).</summary>
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