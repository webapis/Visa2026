using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller ensures that StateChangeRules with a 'Delete' trigger are executed
    // when an object is deleted.
    public class StateChangeTrackingDeleteController : ViewController
    {
        public StateChangeTrackingDeleteController()
        {
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
                if (obj is BaseObject baseObj) StateChangeTrackingHelper.TrackOnDelete(baseObj);
            }
        }
    }
}