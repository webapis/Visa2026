using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller is responsible for invalidating the SyncRule cache
    // whenever a SyncRule is created, modified, or deleted.
    public class SyncRuleCacheController : WindowController
    {
        private bool _cacheNeedsInvalidation = false;

        protected override void OnActivated()
        {
            base.OnActivated();
            // Subscribe to the ObjectSpaceCreated event to handle multiple object spaces
            Application.ObjectSpaceCreated += Application_ObjectSpaceCreated;
        }

        private void Application_ObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e)
        {
            SubscribeToObjectSpaceEvents(e.ObjectSpace);
        }

        private void SubscribeToObjectSpaceEvents(IObjectSpace objectSpace)
        {
            objectSpace.Committing += ObjectSpace_Committing;
            objectSpace.Committed += ObjectSpace_Committed;
            objectSpace.Disposed += ObjectSpace_Disposed;
        }

        private void ObjectSpace_Committing(object sender, CancelEventArgs e)
        {
            var objectSpace = (IObjectSpace)sender;
            // Check if any modified or deleted objects are of type SyncRule
            if (objectSpace.GetObjectsToSave(false).OfType<SyncRule>().Any() ||
                objectSpace.GetObjectsToDelete(false).OfType<SyncRule>().Any())
            {
                _cacheNeedsInvalidation = true;
            }
        }

        private void ObjectSpace_Committed(object sender, EventArgs e)
        {
            if (_cacheNeedsInvalidation)
            {
                CrossObjectSyncHelper.InvalidateCache();
                _cacheNeedsInvalidation = false;
                System.Diagnostics.Debug.WriteLine("[SyncRuleCacheController] SyncRule cache invalidated.");
            }
        }

        private void ObjectSpace_Disposed(object sender, EventArgs e) => UnsubscribeFromObjectSpaceEvents((IObjectSpace)sender);

        private void UnsubscribeFromObjectSpaceEvents(IObjectSpace objectSpace)
        {
            objectSpace.Committing -= ObjectSpace_Committing;
            objectSpace.Committed -= ObjectSpace_Committed;
            objectSpace.Disposed -= ObjectSpace_Disposed;
        }

        protected override void OnDeactivated()
        {
            Application.ObjectSpaceCreated -= Application_ObjectSpaceCreated;
            base.OnDeactivated();
        }
    }
}