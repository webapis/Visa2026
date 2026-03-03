using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class RecycleBinNavigationController : WindowController
    {
        private ShowNavigationItemController navigationController;

        protected override void OnActivated()
        {
            base.OnActivated();
            navigationController = Frame.GetController<ShowNavigationItemController>();
            if (navigationController != null)
            {
                navigationController.ItemsInitialized += NavigationController_ItemsInitialized;
                navigationController.CustomShowNavigationItem += NavigationController_CustomShowNavigationItem;
            }
        }

        private void NavigationController_ItemsInitialized(object sender, EventArgs e)
        {
            var applicationGroup = navigationController.ShowNavigationItemAction.Items.Find("Application");
            if (applicationGroup != null)
            {
                if (applicationGroup.Items.Find("RecycleBin") == null)
                {
                    var recycleBinItem = new ChoiceActionItem("RecycleBin", "Recycle Bin", null)
                    {
                        ImageName = "Action_Delete"
                    };
                    applicationGroup.Items.Add(recycleBinItem);
                }
            }
        }

        private void NavigationController_CustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
        {
            if (e.ActionArguments.SelectedChoiceActionItem.Id == "RecycleBin")
            {
                IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(ApplicationItem));
                string viewId = "ApplicationItem_ListView_RecycleBin";
                e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateListView(viewId, new CollectionSource(objectSpace, typeof(ApplicationItem)), true);
                e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
                e.Handled = true;
            }
        }
    }
}