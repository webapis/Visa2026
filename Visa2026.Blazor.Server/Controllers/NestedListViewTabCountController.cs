using System.Collections;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Appends the nested list view item count to its parent DetailView tab caption (e.g. "Medical History (12)").
/// Root list views show their count via <see cref="ListViewTotalCountController"/> (toolbar label); nested
/// list views inside a tab group use the tab caption because the nested toolbar does not render the
/// RecordsNavigation container. Mirrors the DevExpress "nested list count in tab captions" pattern (Blazor).
/// </summary>
public sealed class DetailViewTabCountController : ViewController<DetailView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        // Force nested items to initialize up front so counts show on tabs the user has not opened yet.
        View.DelayedItemsInitialization = false;
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += LayoutManager_ItemCreated;
    }

    protected override void OnDeactivated()
    {
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= LayoutManager_ItemCreated;
        base.OnDeactivated();
    }

    private void LayoutManager_ItemCreated(object sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.LayoutControlItem is not DxFormLayoutTabPageModel layoutGroup)
            return;
        if (e.ModelLayoutElement.Parent is not IModelTabbedGroup)
            return;

        foreach (var item in (IModelLayoutGroup)e.ModelLayoutElement)
        {
            if (item is not IModelLayoutViewItem layoutViewItem)
                continue;
            if (View.FindItem(layoutViewItem.ViewItem.Id) is not ListPropertyEditor propertyEditor)
                continue;

            var controller = propertyEditor.Frame?.GetController<NestedListViewTabCountController>();
            if (controller is null)
                continue;

            controller.Initialize(layoutGroup);
            propertyEditor.ValueRead += (_, _) => controller.SubscribeToListChanged();
        }
    }
}

/// <summary>
/// Keeps a nested list view's owning tab caption in sync with its item count. Activated only for nested
/// list views; does nothing until <see cref="Initialize"/> is supplied the owning tab page model.
/// </summary>
public sealed class NestedListViewTabCountController : ViewController<ListView>
{
    private DxFormLayoutTabPageModel? layoutGroup;

    public NestedListViewTabCountController()
    {
        TargetViewNesting = Nesting.Nested;
    }

    public void Initialize(DxFormLayoutTabPageModel ownerTab)
    {
        layoutGroup = ownerTab;
        UpdateTabCaption();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        UpdateTabCaption();
        View.CollectionSource.CollectionChanging += CollectionSource_CollectionChanging;
        View.CollectionSource.CollectionChanged += CollectionSource_CollectionChanged;
        View.CollectionSource.CollectionReloaded += CollectionSource_CollectionReloaded;
        SubscribeToListChanged();
    }

    protected override void OnDeactivated()
    {
        View.CollectionSource.CollectionChanging -= CollectionSource_CollectionChanging;
        View.CollectionSource.CollectionChanged -= CollectionSource_CollectionChanged;
        View.CollectionSource.CollectionReloaded -= CollectionSource_CollectionReloaded;
        UnsubscribeFromListChanged();
        base.OnDeactivated();
    }

    internal void SubscribeToListChanged()
    {
        if (GetBindingList(View.CollectionSource.Collection) is IBindingList bindingList)
            bindingList.ListChanged += BindingList_ListChanged;
    }

    private void UnsubscribeFromListChanged()
    {
        if (GetBindingList(View.CollectionSource.Collection) is IBindingList bindingList)
            bindingList.ListChanged -= BindingList_ListChanged;
    }

    private void CollectionSource_CollectionChanging(object sender, EventArgs e) => UnsubscribeFromListChanged();

    private void CollectionSource_CollectionChanged(object sender, EventArgs e)
    {
        UpdateTabCaption();
        SubscribeToListChanged();
    }

    private void CollectionSource_CollectionReloaded(object sender, EventArgs e) => UpdateTabCaption();

    private void BindingList_ListChanged(object sender, ListChangedEventArgs e) => UpdateTabCaption();

    private void UpdateTabCaption()
    {
        if (layoutGroup is null)
            return;

        string baseCaption = StripCount(layoutGroup.Caption);
        int count = View.CollectionSource.GetCount();
        layoutGroup.Caption = count > 0 ? $"{baseCaption} ({count})" : baseCaption;
    }

    private static string StripCount(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return caption;

        int index = caption.LastIndexOf('(');
        return index > 0 ? caption.Remove(index).TrimEnd() : caption;
    }

    private static IBindingList? GetBindingList(object collection) => collection switch
    {
        IBindingList bindingList => bindingList,
        IListSource listSource => listSource.GetList() as IBindingList,
        _ => null,
    };
}
