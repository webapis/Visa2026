using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds the document-copies link column only (same as <see cref="PersonDocumentCopiesListViewColumnUpdater"/>).
/// Parent data columns are enforced at runtime by <see cref="HeaderParentListViewConfigurator"/>.
/// </summary>
public sealed class HeaderDocumentCopiesListViewColumnUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private static readonly string[] TargetViewIds =
    [
        "WorkPermitItem_ListView",
        "InvitationItem_ListView",
        "RejectionItem_ListView",
        "BorderZoneItem_ListView",
        "WorkPermit_ListView",
        "Invitation_ListView",
        "Rejection_ListView",
        "BorderZone_ListView",
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
        const string columnId = nameof(Invitation.DocumentCopiesListLink);
        var column = listView.Columns[columnId] ?? listView.Columns.AddNode<IModelColumn>(columnId);
        column.PropertyName = columnId;
        column.Width = 56;
        column.Index = 1;
        column.Caption = " ";
    }
}
