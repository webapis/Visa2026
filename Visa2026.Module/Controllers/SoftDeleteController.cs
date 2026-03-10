using DevExpress.Data.Filtering;
using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using System.Linq;

namespace Visa2026.Module.Controllers
{
    public class SoftDeleteController : ViewController
    {
        private SimpleAction softDeleteAction;
        private SimpleAction restoreAction;
        private SimpleAction showDeletedAction;
        private DeleteObjectsViewController deleteController;
        private bool isShowingDeleted = false;

        public SoftDeleteController()
        {
            TargetObjectType = typeof(ISoftDelete);

            // Action: Remove (Soft Delete)
            softDeleteAction = new SimpleAction(this, "SoftDelete", PredefinedCategory.Edit);
            softDeleteAction.Caption = "Remove";
            softDeleteAction.ConfirmationMessage = "Are you sure you want to remove the selected record(s)?";
            softDeleteAction.ImageName = "Action_Delete";
            softDeleteAction.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            softDeleteAction.Execute += SoftDeleteAction_Execute;

            // Action: Restore
            restoreAction = new SimpleAction(this, "RestoreObject", PredefinedCategory.Edit);
            restoreAction.Caption = "Restore";
            restoreAction.ConfirmationMessage = "Are you sure you want to restore the selected record(s)?";
            restoreAction.ImageName = "Action_Restore";
            restoreAction.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            restoreAction.Execute += RestoreAction_Execute;

            // Action: Show/Hide Deleted (Toggle)
            showDeletedAction = new SimpleAction(this, "ToggleShowDeleted", PredefinedCategory.View);
            showDeletedAction.Caption = "Show Deleted";
            showDeletedAction.ImageName = "Action_Filter"; 
            showDeletedAction.TargetViewType = ViewType.ListView;
            showDeletedAction.Execute += ShowDeletedAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            deleteController = Frame.GetController<DeleteObjectsViewController>();

            // Check if the user is an administrator
            bool isAdministrator = false;
            if (SecuritySystem.CurrentUser is PermissionPolicyUser user)
            {
                isAdministrator = user.Roles.Any(r => r.IsAdministrative);
            }
            // Only show the restore action for administrators
            restoreAction.Active["IsAdministrator"] = isAdministrator;
            // Only show the "Show Deleted" action for administrators
            showDeletedAction.Active["IsAdministrator"] = isAdministrator;

            if (View is ListView listView)
            {
                if (View.Id.Contains("RecycleBin"))
                {
                    // RECYCLE BIN MODE: Show ONLY deleted items
                    listView.CollectionSource.Criteria["ExcludeDeleted"] = null;
                    listView.CollectionSource.Criteria["ShowOnlyDeleted"] = CriteriaOperator.Parse("IsDeleted = ?", true);

                    // Enable Hard Delete in Recycle Bin
                    if (deleteController != null) deleteController.DeleteAction.Active.RemoveItem("SoftDeleteImplemented");
                    
                    // Disable Soft Delete and Toggle in Recycle Bin
                    softDeleteAction.Active["RecycleBin"] = false;
                    showDeletedAction.Active["RecycleBin"] = false;
                }
                else
                {
                    // NORMAL MODE: Apply default filter (Hide deleted)
                    UpdateFilter();

                    // Disable Hard Delete in normal views
                    if (deleteController != null) deleteController.DeleteAction.Active["SoftDeleteImplemented"] = false;

                    softDeleteAction.Active["RecycleBin"] = true;
                    showDeletedAction.Active["RecycleBin"] = true;
                }
            }
            View.SelectionChanged += View_SelectionChanged;
            UpdateActionsState();
        }

        protected override void OnDeactivated()
        {
            if (View != null) View.SelectionChanged -= View_SelectionChanged;
            if (deleteController != null)
            {
                deleteController.DeleteAction.Active.RemoveItem("SoftDeleteImplemented");
            }
            restoreAction.Active.RemoveItem("IsAdministrator");
            showDeletedAction.Active.RemoveItem("IsAdministrator");
            base.OnDeactivated();
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            UpdateActionsState();
        }

        private void View_SelectionChanged(object sender, EventArgs e)
        {
            UpdateActionsState();
        }

        private void UpdateActionsState()
        {
            bool hasDeleted = false;
            bool hasActive = false;

            foreach (object selected in View.SelectedObjects)
            {
                if (selected is ISoftDelete obj)
                {
                    if (obj.IsDeleted) hasDeleted = true;
                    else hasActive = true;
                }
            }

            // "Remove" is active only if we have active objects selected
            softDeleteAction.Enabled["Selection"] = hasActive && !hasDeleted;
            
            // "Restore" is active only if we have deleted objects selected
            restoreAction.Enabled["Selection"] = hasDeleted && !hasActive;
        }

        private void SoftDeleteAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            IObjectSpace os = Application.CreateObjectSpace(View.ObjectTypeInfo.Type);
            var currentUser = os.GetObject(SecuritySystem.CurrentUser as ApplicationUser);
            foreach (object selectedObj in e.SelectedObjects)
            {
                var obj = os.GetObject(selectedObj) as ISoftDelete;
                if (obj != null)
                {
                    obj.IsDeleted = true;
                    obj.DateDeleted = DateTime.Now;
                    obj.DeletedBy = currentUser;
                    if (obj is BaseObject baseObj) CrossObjectSyncHelper.SyncOnPropertyChanged(baseObj, nameof(ISoftDelete.IsDeleted));
                }
            }
            os.CommitChanges();
            // Refresh the view's object space to see the changes made in the separate object space.
            View.ObjectSpace.Refresh();
        }

        private void RestoreAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            IObjectSpace os = Application.CreateObjectSpace(View.ObjectTypeInfo.Type);
            foreach (object selectedObj in e.SelectedObjects)
            {
                var obj = os.GetObject(selectedObj) as ISoftDelete;
                if (obj != null)
                {
                    obj.IsDeleted = false;
                    obj.DateDeleted = null;
                    obj.DeletedBy = null;
                    if (obj is BaseObject baseObj) CrossObjectSyncHelper.SyncOnPropertyChanged(baseObj, nameof(ISoftDelete.IsDeleted));
                }
            }
            os.CommitChanges();
            // Refresh the view's object space to see the changes made in the separate object space.
            View.ObjectSpace.Refresh();
        }

        private void ShowDeletedAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            isShowingDeleted = !isShowingDeleted;
            showDeletedAction.Caption = isShowingDeleted ? "Hide Deleted" : "Show Deleted";
            UpdateFilter();
        }

        private void UpdateFilter()
        {
            if (View is ListView listView && !View.Id.Contains("RecycleBin"))
            {
                if (isShowingDeleted)
                {
                    // Show All (Active + Deleted)
                    listView.CollectionSource.Criteria.Remove("ExcludeDeleted");
                }
                else
                {
                    // Show Only Active
                    listView.CollectionSource.Criteria["ExcludeDeleted"] = CriteriaOperator.Parse("IsDeleted = ?", false);
                }
            }
        }
    }
}