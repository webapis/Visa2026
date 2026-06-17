using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures the document-copies link column exists on typed Person ListViews.
/// </summary>
public sealed class PersonDocumentCopiesListViewColumnUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
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

            EnsureLinkColumn(listView);
        }
    }

    private static void EnsureLinkColumn(IModelListView listView)
    {
        const string columnId = nameof(Person.DocumentCopiesListLink);
        var column = listView.Columns[columnId] ?? listView.Columns.AddNode<IModelColumn>(columnId);
        column.PropertyName = columnId;
        column.Width = 56;
        column.Index = 1;
        column.Caption = " ";
    }
}
