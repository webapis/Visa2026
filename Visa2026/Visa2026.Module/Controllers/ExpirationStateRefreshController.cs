using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller ensures that the UI (specifically, conditional appearance rules)
    // is updated immediately when a property affecting an object's expiration state changes.
    public class ExpirationStateRefreshController : ViewController<ObjectView>
    {
        // A list of property names that can trigger a refresh.
        private readonly string[] dependentProperties = {
            nameof(IExpirationLogic.IsActive),
            nameof(IExpirationLogic.ExpirationDate),
            "StartDate",
            "IssueDate",
            "ContractStartDate",
            "ValidityDuration" // This affects ExpirationDate in some objects
        };

        public ExpirationStateRefreshController()
        {
            TargetObjectType = typeof(IExpirationLogic);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
            if (View is ListView listView)
            {
                listView.CollectionSource.CollectionReloaded += CollectionSource_CollectionReloaded;
            }
            ObjectSpace.Reloaded += ObjectSpace_Reloaded;
        }

        private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
        {
            if (e.Object is IExpirationLogic && dependentProperties.Contains(e.PropertyName))
            {
                Frame.GetController<AppearanceController>()?.Refresh();
            }
        }

        private void CollectionSource_CollectionReloaded(object sender, EventArgs e)
        private void ObjectSpace_Reloaded(object sender, EventArgs e)
        {
            Frame.GetController<AppearanceController>()?.Refresh();
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
            if (View is ListView listView)
            {
                listView.CollectionSource.CollectionReloaded -= CollectionSource_CollectionReloaded;
            }
            ObjectSpace.Reloaded -= ObjectSpace_Reloaded;
            base.OnDeactivated();
        }
    }
}