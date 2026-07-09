using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Item detail keeps the item-level <see cref="ApplicationItem.BorderZoneLocation"/> editor only.
/// Report merge aliases stay hidden; application header border zone is hidden via
/// <c>ApplicationItem_HideApplicationBorderZone</c> on the BO.
/// </summary>
public sealed class ApplicationItemDetailViewBorderZoneController : ViewController<DetailView>
{
    private static readonly string[] HiddenViewItemIds =
    {
        nameof(ApplicationItem.BorderZoneLocation_NameTm),
        nameof(ApplicationItem.Application_BorderZoneLocation_NameTm),
        nameof(ApplicationItem.Item_BorderZoneLocation_NameTm),
    };

    public ApplicationItemDetailViewBorderZoneController()
    {
        TargetObjectType = typeof(ApplicationItem);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        foreach (var id in HiddenViewItemIds)
        {
            HideViewItem(View.FindItem(id));
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
