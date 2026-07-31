using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Person typed ListViews: dossier + document-copies icon columns before Full Name.
/// </summary>
public sealed class PersonListViewActionColumnsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private static readonly string[] TargetViewIds =
    [
        "Person_ListView_Employees",
        "Person_ListView_FamilyMembers",
        "Person_ListView_TemporaryVisitors",
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var viewId in TargetViewIds)
        {
            if (views[viewId] is not IModelListView listView)
                continue;

            EnsureActionColumns(listView);
        }
    }

    private static void EnsureActionColumns(IModelListView listView)
    {
        EnsureColumn(listView, nameof(Person.DossierListLink), 0, 56, "Dossier");
        EnsureColumn(listView, nameof(Person.DocumentCopiesListLink), 1, 56, "Copies");

        var fullName = listView.Columns[nameof(Person.FullName)]
            ?? listView.Columns.AddNode<IModelColumn>(nameof(Person.FullName));
        fullName.PropertyName = nameof(Person.FullName);
        fullName.Index = 2;
        if (fullName.Width <= 0)
            fullName.Width = 140;
    }

    private static void EnsureColumn(
        IModelListView listView,
        string propertyName,
        int index,
        int width,
        string caption)
    {
        var column = listView.Columns[propertyName]
            ?? listView.Columns.AddNode<IModelColumn>(propertyName);
        column.PropertyName = propertyName;
        column.Index = index;
        column.Width = width;
        column.Caption = caption;
    }
}
