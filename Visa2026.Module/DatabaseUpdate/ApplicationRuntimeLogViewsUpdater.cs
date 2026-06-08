using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationRuntimeLogViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;

        if (views["ApplicationRuntimeLog_ListView"] is IModelListView listView)
        {
            listView.AllowNew = false;
            listView.AllowDelete = false;
            listView.AllowEdit = false;

            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.OccurredAtUtc), 0);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.Severity), 1);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.ErrorCode), 2);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.Category), 3);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.Message), 4);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.UserName), 5);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.DeploymentEnvironment), 6);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.CorrelationId), 7);
            EnsureListColumn(listView, nameof(ApplicationRuntimeLog.ResolutionStatus), 8);
        }

        if (views["ApplicationRuntimeLog_DetailView"] is IModelDetailView detailView)
        {
            detailView.AllowEdit = false;
            detailView.AllowDelete = false;
            detailView.AllowNew = false;
        }
    }

    private static void EnsureListColumn(IModelListView listView, string propertyName, int index)
    {
        var column = listView.Columns[propertyName]
            ?? listView.Columns.AddNode<IModelColumn>(propertyName);
        column.PropertyName = propertyName;
        column.Index = index;
    }
}
