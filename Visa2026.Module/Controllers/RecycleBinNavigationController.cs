using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.DC;
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

        protected override void OnDeactivated()
        {
            if (navigationController != null)
            {
                navigationController.ItemsInitialized -= NavigationController_ItemsInitialized;
                navigationController.CustomShowNavigationItem -= NavigationController_CustomShowNavigationItem;
            }
            base.OnDeactivated();
        }

        private void NavigationController_ItemsInitialized(object sender, EventArgs e)
        {
            var navigationItems = ((ShowNavigationItemController)sender).ShowNavigationItemAction.Items;

            // Find or create the main "Recycle Bin" navigation group
            var recycleBinGroup = navigationItems.Find("RecycleBinGroup");
            if (recycleBinGroup == null)
            {
                recycleBinGroup = new ChoiceActionItem("RecycleBinGroup", "Recycle Bin", null)
                {
                    ImageName = "Action_Delete"
                };
                // Add it after the 'Default' group if it exists, otherwise at the end.
                var defaultGroup = navigationItems.Find("Default");
                int index = defaultGroup != null ? navigationItems.IndexOf(defaultGroup) + 1 : -1;
                if (index != -1)
                {
                    navigationItems.Insert(index, recycleBinGroup);
                }
                else
                {
                    navigationItems.Add(recycleBinGroup);
                }
            }

            // Find all types that implement ISoftDelete and are visible in the UI
            var softDeleteTypes = XafTypesInfo.Instance.PersistentTypes
                .Where(ti => typeof(ISoftDelete).IsAssignableFrom(ti.Type) && !ti.IsAbstract && ti.IsVisible)
                .Select(ti => new { TypeInfo = ti, Caption = Application.Model.BOModel.GetClass(ti.Type).Caption })
                .OrderBy(x => x.Caption)
                .ToList();

            // Create a sub-item for each type
            foreach (var itemData in softDeleteTypes)
            {
                string itemId = $"RecycleBin_{itemData.TypeInfo.Name}";
                if (recycleBinGroup.Items.Find(itemId) == null)
                {
                    var item = new ChoiceActionItem(itemId, itemData.Caption, null)
                    {
                        Data = itemData.TypeInfo.Type // Store the type for later use
                    };
                    recycleBinGroup.Items.Add(item);
                }
            }
        }

        private void NavigationController_CustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
        {
            var selectedItem = e.ActionArguments.SelectedChoiceActionItem;
            if (selectedItem.Id.StartsWith("RecycleBin_") && selectedItem.Data is Type targetType)
            {
                IObjectSpace objectSpace = Application.CreateObjectSpace(targetType);
                string viewId = $"{targetType.Name}_ListView_RecycleBin";
                var collectionSource = new CollectionSource(objectSpace, targetType);
                e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateListView(viewId, collectionSource, true);
                e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
                e.Handled = true;
            }
        }
    }
}