using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Migration SLA profile detail: nested application types list shows <see cref="LookupBase.NameTm"/> only;
/// Link popup uses a dedicated single-column list view.
/// </summary>
public sealed class ApplicationMigrationSlaProfileViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private const string NestedListViewId = "ApplicationMigrationSlaProfile_ApplicationTypes_ListView";
    private const string LinkListViewId = "ApplicationType_MigrationSlaProfileLink_ListView";
    private const string NameTmMember = nameof(LookupBase.NameTm);

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        EnsureLinkListView(views);

        if (views[NestedListViewId] is not IModelListView nestedListView)
            return;

        nestedListView.AllowNew = false;
        nestedListView.AllowDelete = false;
        nestedListView.AllowLink = true;
        nestedListView.AllowUnlink = true;
        ConfigureSingleColumnListView(nestedListView, NameTmMember);

        if (views[LinkListViewId] is IModelListView linkListView)
            nestedListView.SetValue("LinkToListView", linkListView);
    }

    private static void EnsureLinkListView(IModelViews views)
    {
        if (views[LinkListViewId] is IModelListView existing)
        {
            ConfigureSingleColumnListView(existing, NameTmMember);
            return;
        }

        if (views.Application.BOModel.GetClass(typeof(ApplicationType)) is not IModelClass applicationTypeClass)
            return;

        var linkListView = views.AddNode<IModelListView>(LinkListViewId);
        linkListView.ModelClass = applicationTypeClass;
        linkListView.AllowNew = false;
        linkListView.AllowDelete = false;
        linkListView.AllowEdit = false;
        linkListView.AllowLink = false;
        linkListView.AllowUnlink = false;
        ConfigureSingleColumnListView(linkListView, NameTmMember);
    }

    private static void ConfigureSingleColumnListView(IModelListView listView, string displayProperty)
    {
        foreach (var column in listView.Columns.ToList())
        {
            if (column.PropertyName == displayProperty)
                continue;

            column.Index = -1;
        }

        var displayColumn = listView.Columns[displayProperty]
            ?? listView.Columns.AddNode<IModelColumn>(displayProperty);
        displayColumn.PropertyName = displayProperty;
        displayColumn.Index = 0;
    }
}
