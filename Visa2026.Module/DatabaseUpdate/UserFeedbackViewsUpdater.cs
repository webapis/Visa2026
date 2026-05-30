using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.Feedback;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Ensures triage fields (Status, FeedbackType) are visible in list and detail views.</summary>
public sealed class UserFeedbackViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        if (views["UserFeedback_ListView"] is not IModelListView listView)
            return;

        EnsureListColumn(listView, nameof(UserFeedback.FeedbackType), index: 0);
        EnsureListColumn(listView, nameof(UserFeedback.Status), index: 1);
        EnsureListColumn(listView, nameof(UserFeedback.Severity), index: 2);
        EnsureListColumn(listView, nameof(UserFeedback.Summary), index: 3);
        EnsureListColumn(listView, nameof(UserFeedback.SubmittedBy), index: 4);
        EnsureListColumn(listView, nameof(UserFeedback.SubmittedAt), index: 5);
    }

    private static void EnsureListColumn(IModelListView listView, string propertyName, int index)
    {
        var column = listView.Columns[propertyName]
            ?? listView.Columns.AddNode<IModelColumn>(propertyName);
        column.PropertyName = propertyName;
        column.Index = index;
    }
}
