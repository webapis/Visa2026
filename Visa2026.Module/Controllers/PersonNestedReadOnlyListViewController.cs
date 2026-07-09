using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Reinforces browse-only nested Person collections (New/Delete/Link disabled even when model merge lags).
/// </summary>
public sealed class PersonNestedReadOnlyListViewController : ViewController<ListView>
{
    public PersonNestedReadOnlyListViewController()
    {
        TargetViewNesting = Nesting.Nested;
        TargetViewId = string.Join(';', PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ApplyReadOnlyChrome();
    }

    private void ApplyReadOnlyChrome()
    {
        View.AllowNew.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
        View.AllowEdit.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
        View.AllowDelete.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
        Frame.GetController<DeleteObjectsViewController>()?
            .DeleteAction.Active.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
        Frame.GetController<LinkUnlinkController>()?.LinkAction
            .Active.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
        Frame.GetController<LinkUnlinkController>()?.UnlinkAction
            .Active.SetItemValue(nameof(PersonNestedReadOnlyListViewController), false);
    }
}