using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class SyncDeleteController : ViewController
    {
        public SyncDeleteController()
        {
            // Activate for any view displaying a BaseObject
            TargetObjectType = typeof(BaseObject);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            ObjectSpace.ObjectDeleting += ObjectSpace_ObjectDeleting;
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.ObjectDeleting -= ObjectSpace_ObjectDeleting;
            base.OnDeactivated();
        }

        private void ObjectSpace_ObjectDeleting(object sender, ObjectsManipulatingEventArgs e)
        {
            foreach (var obj in e.Objects)
            {
                if (obj is BaseObject baseObj)
                {
                    // 1. Try to call the specific 'OnDeleting' method (e.g., defined in Visa.cs)
                    var method = baseObj.GetType().GetMethod("OnDeleting");
                    if (method != null)
                    {
                        method.Invoke(baseObj, null);
                    }
                    else
                    {
                        // 2. Fallback: Call the helper directly if no specific hook exists
                        CrossObjectSyncHelper.SyncOnDelete(baseObj);
                    }
                }
            }
        }
    }
}