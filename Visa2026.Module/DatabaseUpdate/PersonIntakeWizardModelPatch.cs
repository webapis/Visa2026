using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// After model merge: ensure intake wizard item + slim layout on Employee/Family intake DetailViews.
/// </summary>
public static class PersonIntakeWizardModelPatch
{
    private static readonly string[] ViewIds =
    [
        PersonDetailViewIds.Employee,
        PersonDetailViewIds.FamilyMember
    ];

    private const string WizardHostGroupId = "PersonIntakeWizardHost";

    public static void Apply(IModelApplication? modelApplication)
    {
        if (modelApplication?.Views == null)
            return;

        const string propertyName = nameof(Person.PersonIntakeWizardUi);

        foreach (var viewId in ViewIds)
        {
            if (modelApplication.Views[viewId] is not IModelDetailView detailView)
                continue;

            EnsureWizardItem(detailView, propertyName);
            RebuildWizardLayout(detailView, propertyName);

            if (detailView is ModelNode detailNode)
            {
                TrySet(detailNode, "CustomCSSClassName", "visa-person-intake-detail");
                TrySet(detailNode, "FreezeLayout", true);
                TrySet(detailNode, "CustomizationFormEnabled", false);
            }
        }
    }

    private static void EnsureWizardItem(IModelDetailView detailView, string propertyName)
    {
        IModelMemberViewItem? item = detailView.Items[propertyName] as IModelMemberViewItem;
        if (item == null && detailView.Items is ModelNode)
        {
            item = detailView.Items.AddNode<IModelMemberViewItem>(propertyName);
            item.PropertyName = propertyName;
        }

        if (item == null)
            return;

        item.Caption = string.Empty;
        item.Index = -3000;
        if (item is ModelNode itemNode)
        {
            TrySet(itemNode, "AllowEdit", "True");
            TrySet(itemNode, "ShowCaption", "False");
            TrySet(itemNode, "PropertyEditorAlias", PersonIntakeWizardEditorAliases.Wizard);
        }
    }

    private static void RebuildWizardLayout(IModelDetailView detailView, string propertyName)
    {
        if (detailView.Layout == null || detailView.Items[propertyName] == null)
            return;

        // Prefer existing wizard layout item (Model.xafml). Only rebuild when missing.
        if (FindLayoutViewItem(detailView.Layout, propertyName) != null)
            return;

        try
        {
            while (detailView.Layout.Count > 0)
                detailView.Layout[0].Remove();
        }
        catch
        {
            // Fall through and try additive ensure.
        }

        var main = detailView.Layout.AddNode<IModelLayoutGroup>("Main");
        TrySet((ModelNode)main, "RelativeSize", 100d);
        TrySet((ModelNode)main, "CustomCSSClassName", "xaf-fill-root");

        var simple = main.AddNode<IModelLayoutGroup>("SimpleEditors");
        TrySet((ModelNode)simple, "ShowCaption", false);
        TrySet((ModelNode)simple, "RelativeSize", 100d);
        TrySet((ModelNode)simple, "CustomCSSClassName", "xaf-fill-available");

        var host = simple.AddNode<IModelLayoutGroup>(WizardHostGroupId);
        TrySet((ModelNode)host, "ShowCaption", false);
        TrySet((ModelNode)host, "RelativeSize", 100d);
        TrySet((ModelNode)host, "CustomCSSClassName", "xaf-fill-available");

        var layoutItem = host.AddNode<IModelLayoutViewItem>(propertyName);
        layoutItem.ViewItem = detailView.Items[propertyName];
        layoutItem.Index = 0;
        layoutItem.RelativeSize = 100;
        layoutItem.ShowCaption = false;
        TrySet((ModelNode)layoutItem, "CustomCSSClassName", "visa-person-intake-wizard-host xaf-fill-available");
    }

    private static IModelLayoutViewItem? FindLayoutViewItem(IModelNode? root, string viewItemId)
    {
        foreach (var item in EnumerateLayoutViewItems(root))
        {
            if (item.ViewItem?.Id == viewItemId || string.Equals(item.Id, viewItemId, StringComparison.Ordinal))
                return item;
        }

        return null;
    }

    private static IEnumerable<IModelLayoutViewItem> EnumerateLayoutViewItems(IModelNode? node)
    {
        if (node == null)
            yield break;

        if (node is IModelLayoutViewItem layoutViewItem)
            yield return layoutViewItem;

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            yield break;

        foreach (ModelNode child in modelNode.Nodes)
        {
            if (child == null)
                continue;
            foreach (var nested in EnumerateLayoutViewItems(child))
                yield return nested;
        }
    }

    private static void TrySet(ModelNode node, string name, object value)
    {
        try
        {
            node.SetValue(name, value);
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (InvalidCastException)
        {
        }
    }
}