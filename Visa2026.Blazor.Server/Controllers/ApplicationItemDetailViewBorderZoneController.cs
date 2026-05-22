using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Visa2026.Blazor.Server.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Item detail should show a single <see cref="ApplicationItem.BorderZoneLocation"/> editor.
/// Suppresses duplicate layout/items left from model differences or an extra IsNewNode PropertyEditor.
/// </summary>
public sealed class ApplicationItemDetailViewBorderZoneController : ViewController<DetailView>
{
    private const string BorderZonePropertyName = nameof(ApplicationItem.BorderZoneLocation);

    private static readonly string[] HiddenViewItemIds =
    {
        nameof(ApplicationItem.BorderZoneLocation_NameTm),
        nameof(ApplicationItem.Application_BorderZoneLocation_NameTm),
        "Application.BorderZoneLocation",
    };

    public ApplicationItemDetailViewBorderZoneController()
    {
        TargetObjectType = typeof(ApplicationItem);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        SuppressDuplicateBorderZoneModelNodes();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        HideRedundantBorderZoneViewItems();
    }

    private void SuppressDuplicateBorderZoneModelNodes()
    {
        if (View?.Model is not IModelDetailView detailView)
        {
            return;
        }

        if (detailView.Layout != null)
        {
            var seenLayoutItem = false;
            foreach (var layoutItem in EnumerateLayoutViewItems(detailView.Layout).ToList())
            {
                if (!IsBorderZoneLayoutItem(layoutItem))
                {
                    continue;
                }

                if (seenLayoutItem)
                {
                    RemoveLayoutModelNode(layoutItem);
                }
                else
                {
                    seenLayoutItem = true;
                }
            }
        }

        SuppressDuplicateBorderZonePropertyEditors(detailView);
    }

    private static void SuppressDuplicateBorderZonePropertyEditors(IModelDetailView detailView)
    {
        if (detailView.Items == null)
        {
            return;
        }

        var seenPropertyEditor = false;
        foreach (IModelPropertyEditor propertyEditor in detailView.Items.ToList())
        {
            if (!string.Equals(propertyEditor.PropertyName, BorderZonePropertyName, StringComparison.Ordinal))
            {
                continue;
            }

            if (seenPropertyEditor)
            {
                propertyEditor.Remove();
            }
            else
            {
                seenPropertyEditor = true;
            }
        }
    }

    private void HideRedundantBorderZoneViewItems()
    {
        foreach (var id in HiddenViewItemIds)
        {
            HideViewItem(View.FindItem(id));
        }

        var borderZoneEditors = View.GetItems<PropertyEditor>()
            .Where(IsBorderZonePropertyEditor)
            .ToList();

        if (borderZoneEditors.Count <= 1)
        {
            return;
        }

        var keep = borderZoneEditors.OfType<CommaSeparatedMultiSelectPropertyEditor>().FirstOrDefault()
            ?? borderZoneEditors[0];

        foreach (var editor in borderZoneEditors)
        {
            if (!ReferenceEquals(editor, keep))
            {
                HideViewItem(editor);
            }
        }
    }

    private static bool IsBorderZonePropertyEditor(PropertyEditor editor) =>
        string.Equals(editor.Id, BorderZonePropertyName, StringComparison.Ordinal)
        || string.Equals(editor.PropertyName, BorderZonePropertyName, StringComparison.Ordinal)
        || string.Equals(editor.MemberInfo?.Name, BorderZonePropertyName, StringComparison.Ordinal);

    private static bool IsBorderZoneLayoutItem(IModelLayoutViewItem? layoutItem)
    {
        if (layoutItem == null)
        {
            return false;
        }

        if (string.Equals(layoutItem.Id, BorderZonePropertyName, StringComparison.Ordinal)
            || string.Equals(layoutItem.ViewItem?.Id, BorderZonePropertyName, StringComparison.Ordinal))
        {
            return true;
        }

        return layoutItem.ViewItem is IModelPropertyEditor propertyEditor
            && string.Equals(propertyEditor.PropertyName, BorderZonePropertyName, StringComparison.Ordinal);
    }

    private static IEnumerable<IModelLayoutViewItem> EnumerateLayoutViewItems(IModelNode? node)
    {
        if (node == null)
        {
            yield break;
        }

        if (node is IModelLayoutViewItem layoutItem)
        {
            yield return layoutItem;
        }

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
        {
            yield break;
        }

        foreach (ModelNode child in modelNode.Nodes)
        {
            if (child == null)
            {
                continue;
            }

            foreach (var item in EnumerateLayoutViewItems(child))
            {
                yield return item;
            }
        }
    }

    private static void RemoveLayoutModelNode(IModelLayoutViewItem layoutItem)
    {
        layoutItem.Index = -1;
        if (layoutItem is ModelNode modelNode)
        {
            modelNode.SetValue("Removed", true);
        }
    }

    private static void HideViewItem(ViewItem? item)
    {
        if (item is IAppearanceVisibility visibility)
        {
            visibility.Visibility = ViewItemVisibility.Hide;
        }
    }
}
