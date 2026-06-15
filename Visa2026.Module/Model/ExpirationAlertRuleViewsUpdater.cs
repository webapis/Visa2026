using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Configuration list: seeded document expiration rules only (no create/delete).
/// </summary>
public sealed class ExpirationAlertRuleViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;

        if (views["ExpirationAlertRule_ListView"] is IModelListView listView)
        {
            listView.Criteria = DocumentExpirationAlertConfigurationKeys.ListViewCriteria;
            listView.AllowNew = false;
            listView.AllowDelete = false;
        }

        if (views["ExpirationAlertRule_DetailView"] is IModelDetailView detailView)
        {
            detailView.AllowNew = false;
            detailView.AllowDelete = false;
        }
    }
}
