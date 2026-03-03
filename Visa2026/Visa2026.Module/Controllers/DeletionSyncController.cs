using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller ensures that SyncRules with a 'Delete' trigger are executed 
    // before the object is passed to the ObjectSpace for deletion.
    // This guarantees that the object's properties and relationships are still valid during rule evaluation.
    public class DeletionSyncController : ViewController<ObjectView>
    {
        private DeleteObjectsViewController deleteController;

        protected override void OnActivated()
        {
            base.OnActivated();
            deleteController = Frame.GetController<DeleteObjectsViewController>();
            if (deleteController != null)
            {
                deleteController.Deleting += DeleteController_Deleting;
            }
        }

        private void DeleteController_Deleting(object sender, DeletingEventArgs e)
        {
            foreach (var obj in e.Objects)
            {
                // Call the sync helper for any object that should trigger 'Delete' rules.
                // The helper itself will find the correct rules based on the object's type.
                if (obj is BaseObject bo) // This line will now compile correctly
                {
                    CrossObjectSyncHelper.SyncOnDelete(bo);
                }
            }
        }

        protected override void OnDeactivated()
        {
            if (deleteController != null)
            {
                deleteController.Deleting -= DeleteController_Deleting;
            }
            base.OnDeactivated();
        }
    }
}