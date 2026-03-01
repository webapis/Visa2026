using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.Controllers
{
    public class SyncRulesController : ViewController
    {
        public SyncRulesController()
        {
            TargetObjectType = typeof(SyncRule);
            TargetViewType = ViewType.ListView;

            SimpleAction resetRulesAction = new SimpleAction(this, "ResetSyncRules", PredefinedCategory.Tools);
            resetRulesAction.Caption = "Reset Rules";
            resetRulesAction.ConfirmationMessage = "Are you sure you want to reset the default Sync Rules? This will overwrite configurations for standard rules.";
            resetRulesAction.ImageName = "Action_ResetViewSettings";
            resetRulesAction.Execute += ResetRulesAction_Execute;
        }

        private void ResetRulesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[SyncRulesController] 'Reset Rules' action started.");
            // Use a separate ObjectSpace to perform the update transaction independently
            IObjectSpace os = Application.CreateObjectSpace(typeof(SyncRule));
            try
            {
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Created new ObjectSpace.");
                // Instantiate the updater with the new ObjectSpace
                // Version is not strictly used in our custom logic, so a dummy version is fine
                var updater = new SyncRulesUpdater(os, new Version(1, 0, 0, 0));
                
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Calling UpdateDatabaseAfterUpdateSchema...");
                // Trigger the logic
                updater.UpdateDatabaseAfterUpdateSchema();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] UpdateDatabaseAfterUpdateSchema returned.");

                // Refresh the View to reflect changes
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Refreshing View ObjectSpace...");
                if (View is ListView listView)
                {
                    listView.CollectionSource.Reload();
                }
                View.ObjectSpace.Refresh();
                View.Refresh();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] View ObjectSpace refreshed.");
                
                Application.ShowViewStrategy.ShowMessage("Sync Rules have been reset to defaults.", InformationType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRulesController] EXCEPTION: {ex}");
                Application.ShowViewStrategy.ShowMessage($"Error resetting rules: {ex.Message}", InformationType.Error);
            }
            finally
            {
                os.Dispose();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] ObjectSpace disposed.");
            }
        }
    }
}