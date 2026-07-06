using DevExpress.Data;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.Xpo.DB;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Ministry legs nested list on <see cref="ApprovalLegProfile"/>: fixed sequence order, no interactive column sort.
/// </summary>
public sealed class ApprovalLegProfileMinistryLegViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string NestedListViewId = "ApprovalLegProfile_MinistryLegs_ListView";

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        if (views[NestedListViewId] is IModelListView listView)
            ConfigureListView(listView);
    }

    private static void ConfigureListView(IModelListView listView)
    {
        foreach (var column in listView.Columns)
        {
            column.SortIndex = -1;
            column.SortOrder = ColumnSortOrder.None;
        }

        var sortNode = listView.Sorting[nameof(ApprovalLegProfileMinistryLeg.Sequence)]
            ?? listView.Sorting.AddNode<IModelSortProperty>(nameof(ApprovalLegProfileMinistryLeg.Sequence));
        sortNode.PropertyName = nameof(ApprovalLegProfileMinistryLeg.Sequence);
        sortNode.Direction = SortingDirection.Ascending;
    }
}