using System.Collections;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Visa2026.Module;

namespace Visa2026.Blazor.Server.Controllers;

internal interface INestedTabCaptionTarget
{
    string Caption { get; set; }
}

internal sealed class NestedTabCaptionTarget : INestedTabCaptionTarget
{
    private readonly Func<string> _getCaption;
    private readonly Action<string> _setCaption;

    private NestedTabCaptionTarget(Func<string> getCaption, Action<string> setCaption)
    {
        _getCaption = getCaption;
        _setCaption = setCaption;
    }

    public string Caption
    {
        get => _getCaption();
        set => _setCaption(value);
    }

    public static INestedTabCaptionTarget? TryCreate(object? layoutControlItem) => layoutControlItem switch
    {
        DxFormLayoutTabPageModel tabPage => new NestedTabCaptionTarget(() => tabPage.Caption, value => tabPage.Caption = value),
        DxFormLayoutGroupModel group => new NestedTabCaptionTarget(() => group.Caption, value => group.Caption = value),
        _ => null,
    };
}

/// <summary>
/// Appends the nested list view item count to its parent DetailView tab caption (e.g. "Medical History (12)").
/// Root list views show their count via <see cref="ListViewTotalCountController"/> (toolbar label); nested
/// list views inside a tab group use the tab caption because the nested toolbar does not render the
/// RecordsNavigation container. Mirrors the DevExpress "nested list count in tab captions" pattern (Blazor).
/// </summary>
public sealed class DetailViewTabCountController : ViewController<DetailView>
{
    private readonly Dictionary<string, INestedTabCaptionTarget> _tabPages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _baseCaptions = new(StringComparer.Ordinal);
    private readonly HashSet<ListPropertyEditor> _wiredEditors = [];

    protected override void OnActivated()
    {
        base.OnActivated();
        View.DelayedItemsInitialization = false;
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += LayoutManager_ItemCreated;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        WireAllListPropertyEditors();
    }

    protected override void OnDeactivated()
    {
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= LayoutManager_ItemCreated;

        _tabPages.Clear();
        _baseCaptions.Clear();
        _wiredEditors.Clear();
        base.OnDeactivated();
    }

    private void LayoutManager_ItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.ModelLayoutElement.Parent is IModelTabbedGroup)
            RegisterTabPage(e.ModelLayoutElement.Id, e.LayoutControlItem, (IModelLayoutGroup)e.ModelLayoutElement);

        if (e.LayoutControlItem is DxFormLayoutTabPagesModel
            && e.ModelLayoutElement is IModelTabbedGroup tabbedGroupModel)
        {
            foreach (var child in tabbedGroupModel)
            {
                if (child is IModelLayoutGroup tabModel)
                    RegisterBaseCaption(tabModel.Id, tabModel);
            }
        }
    }

    private void RegisterTabPage(string layoutTabId, object? layoutControlItem, IModelLayoutGroup modelGroup)
    {
        if (NestedTabCaptionTarget.TryCreate(layoutControlItem) is not { } tabPage)
            return;

        _tabPages[layoutTabId] = tabPage;
        RegisterBaseCaption(layoutTabId, modelGroup);
        TryWireTab(layoutTabId);
    }

    private void RegisterBaseCaption(string layoutTabId, IModelLayoutGroup modelGroup)
    {
        _baseCaptions[layoutTabId] = StripCount(
            DocumentCollectionTabCaptionHelper.TryGetBaseCaption(View.Id, layoutTabId)
            ?? modelGroup.Caption
            ?? layoutTabId);
    }

    private void TryWireTab(string layoutTabId)
    {
        if (!_tabPages.TryGetValue(layoutTabId, out var tabPage))
            return;

        string baseCaption = _baseCaptions.GetValueOrDefault(layoutTabId, layoutTabId);

        foreach (var layoutViewItem in EnumerateLayoutViewItems(FindModelTabGroup(layoutTabId)))
        {
            if (layoutViewItem.ViewItem?.Id is not { } viewItemId)
                continue;
            if (View.FindItem(viewItemId) is not ListPropertyEditor propertyEditor)
                continue;

            WireEditor(propertyEditor, tabPage, baseCaption);
            break;
        }
    }

    private void WireAllListPropertyEditors()
    {
        foreach (var propertyEditor in View.GetItems<ListPropertyEditor>())
            TryWireEditor(propertyEditor);
    }

    private void TryWireEditor(ListPropertyEditor propertyEditor)
    {
        if (_wiredEditors.Contains(propertyEditor))
            return;

        string? layoutTabId = FindTabLayoutId(propertyEditor.Id);
        if (layoutTabId is null || !_tabPages.TryGetValue(layoutTabId, out var tabPage))
            return;

        string baseCaption = _baseCaptions.GetValueOrDefault(layoutTabId, tabPage.Caption);
        WireEditor(propertyEditor, tabPage, baseCaption);
    }

    private void WireEditor(ListPropertyEditor propertyEditor, INestedTabCaptionTarget tabPage, string baseCaption)
    {
        void TryWire()
        {
            var controller = propertyEditor.Frame?.GetController<NestedListViewTabCountController>();
            if (controller is null)
                return;

            controller.Initialize(tabPage, baseCaption);
            controller.SubscribeToListChanged();
            _wiredEditors.Add(propertyEditor);
        }

        TryWire();
        if (_wiredEditors.Contains(propertyEditor))
            return;

        propertyEditor.ValueRead += OnPropertyEditorValueRead;
        void OnPropertyEditorValueRead(object? _, EventArgs __)
        {
            TryWire();
            if (_wiredEditors.Contains(propertyEditor))
                propertyEditor.ValueRead -= OnPropertyEditorValueRead;
        }
    }

    private IModelLayoutGroup? FindModelTabGroup(string layoutTabId) =>
        FindLayoutGroup(View.Model.Layout, layoutTabId);

    private static IModelLayoutGroup? FindLayoutGroup(IModelNode? node, string layoutTabId)
    {
        if (node is IModelLayoutGroup group && group.Id == layoutTabId)
            return group;

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            return null;

        foreach (ModelNode child in modelNode.Nodes)
        {
            var match = FindLayoutGroup(child, layoutTabId);
            if (match is not null)
                return match;
        }

        return null;
    }

    private string? FindTabLayoutId(string viewItemId)
    {
        if (_tabPages.ContainsKey(viewItemId))
            return viewItemId;

        return FindTabParentId(View.Model.Layout, viewItemId);
    }

    private static string? FindTabParentId(IModelNode? node, string viewItemId)
    {
        if (node is IModelLayoutViewItem layoutViewItem
            && layoutViewItem.ViewItem?.Id == viewItemId
            && node.Parent is IModelLayoutGroup tabGroup
            && tabGroup.Parent is IModelTabbedGroup)
        {
            return tabGroup.Id;
        }

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            return null;

        foreach (ModelNode child in modelNode.Nodes)
        {
            var match = FindTabParentId(child, viewItemId);
            if (match is not null)
                return match;
        }

        return null;
    }

    private static IEnumerable<IModelLayoutViewItem> EnumerateLayoutViewItems(IModelLayoutGroup? group)
    {
        if (group is null)
            yield break;

        foreach (var node in group)
        {
            if (node is IModelLayoutViewItem viewItem && viewItem.ViewItem != null)
                yield return viewItem;
            else if (node is IModelLayoutGroup nestedGroup)
            {
                foreach (var nested in EnumerateLayoutViewItems(nestedGroup))
                    yield return nested;
            }
        }
    }

    private static string StripCount(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return caption;

        int index = caption.LastIndexOf('(');
        if (index <= 0)
            return caption;

        string tail = caption[index..].Trim();
        if (tail.Length >= 3 && tail[^1] == ')' && tail[1..^1].All(char.IsDigit))
            return caption[..index].TrimEnd();

        return caption;
    }
}

/// <summary>
/// Keeps a nested list view's owning tab caption in sync with its item count. Activated only for nested
/// list views; does nothing until <see cref="Initialize"/> is supplied the owning tab page model.
/// </summary>
public sealed class NestedListViewTabCountController : ViewController<ListView>
{
    private INestedTabCaptionTarget? _tabCaption;
    private string _baseCaption = string.Empty;

    public NestedListViewTabCountController()
    {
        TargetViewNesting = Nesting.Nested;
    }

    internal void Initialize(INestedTabCaptionTarget ownerTab, string baseCaption)
    {
        _tabCaption = ownerTab;
        _baseCaption = StripCount(baseCaption);
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

    private void CollectionSource_CollectionChanging(object? sender, EventArgs e) => UnsubscribeFromListChanged();

    private void CollectionSource_CollectionChanged(object? sender, EventArgs e)
    {
        UpdateTabCaption();
        SubscribeToListChanged();
    }

    private void CollectionSource_CollectionReloaded(object? sender, EventArgs e) => UpdateTabCaption();

    private void BindingList_ListChanged(object? sender, ListChangedEventArgs e) => UpdateTabCaption();

    private void UpdateTabCaption()
    {
        if (_tabCaption is null || string.IsNullOrEmpty(_baseCaption))
            return;

        int count = View.CollectionSource.GetCount();
        _tabCaption.Caption = count > 0 ? $"{_baseCaption} ({count})" : _baseCaption;
    }

    private static string StripCount(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return caption;

        int index = caption.LastIndexOf('(');
        if (index <= 0)
            return caption;

        string tail = caption[index..].Trim();
        if (tail.Length >= 3 && tail[^1] == ')' && tail[1..^1].All(char.IsDigit))
            return caption[..index].TrimEnd();

        return caption;
    }

    private static IBindingList? GetBindingList(object collection) => collection switch
    {
        IBindingList bindingList => bindingList,
        IListSource listSource => listSource.GetList() as IBindingList,
        _ => null,
    };
}