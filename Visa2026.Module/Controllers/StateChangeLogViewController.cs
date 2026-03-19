using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller targets any DetailView and looks for a nested ListView of StateChangeLog objects.
    // It then filters this ListView to show only the logs relevant to the master object of the DetailView.
    // This allows adding a "History" tab to any business object without modifying its class,
    // by adding a DashboardViewItem pointing to the StateChangeLog_ListView via the Model Editor.
    public class StateChangeLogViewController : ViewController<DetailView>
    {
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();

            // Find all ListPropertyEditors in the current DetailView.
            // This handles cases where a collection property might be used.
            foreach (ListPropertyEditor editor in View.GetItems<ListPropertyEditor>())
            {
                if (editor.Frame?.View is ListView listView && listView.ObjectTypeInfo.Type == typeof(StateChangeLog))
                {
                    ApplyFilter(listView);
                }
            }

            // Find all DashboardViewItems which can host a ListView.
            // This is the primary way to show history without modifying business objects.
            foreach (DashboardViewItem dbi in View.GetItems<DashboardViewItem>())
            {
                // If the inner view is already created, apply the filter.
                if (dbi.InnerView is ListView listView && listView.ObjectTypeInfo.Type == typeof(StateChangeLog))
                {
                    ApplyFilter(listView);
                }
                else if (dbi.InnerView == null)
                {
                    // If the inner view is not created yet, subscribe to the event.
                    dbi.ControlCreated += DashboardViewItem_ControlCreated;
                }
            }
        }

        private void DashboardViewItem_ControlCreated(object sender, EventArgs e)
        {
            if (sender is DashboardViewItem dbi && dbi.InnerView is ListView listView && listView.ObjectTypeInfo.Type == typeof(StateChangeLog))
            {
                ApplyFilter(listView);
                // Unsubscribe to avoid memory leaks.
                dbi.ControlCreated -= DashboardViewItem_ControlCreated;
            }
        }

        private void ApplyFilter(ListView listView)
        {
            if (View.CurrentObject is BaseObject masterObject)
            {
                // Get the type and ID of the master object.
                // We use ObjectSpace.GetObjectType in case of proxy objects.
                Type masterObjectType = ObjectSpace.GetObjectType(masterObject);
                string masterObjectKey = masterObject.ID.ToString();

                // Create the filter criteria:
                // e.g., [TargetBoTypeFullName] = 'Visa2026.Module.BusinessObjects.Visa' AND [TargetObjectId] = 'some-guid'
                var criteria = CriteriaOperator.And(
                    new BinaryOperator(nameof(StateChangeLog.TargetBoTypeFullName), masterObjectType.FullName),
                    new BinaryOperator(nameof(StateChangeLog.TargetObjectId), masterObjectKey)
                );

                // Apply the filter to the ListView's collection source.
                // Using a key ensures we don't interfere with other criteria.
                listView.CollectionSource.Criteria["StateChangeLogHistoryFilter"] = criteria;
            }
            else
            {
                // If there's no master object, or it's not a BaseObject, show nothing.
                listView.CollectionSource.Criteria["StateChangeLogHistoryFilter"] = CriteriaOperator.Parse("1=0");
            }
        }

        protected override void OnDeactivated()
        {
            // Unsubscribe from events to prevent memory leaks.
            foreach (DashboardViewItem dbi in View.GetItems<DashboardViewItem>())
            {
                dbi.ControlCreated -= DashboardViewItem_ControlCreated;
            }
            base.OnDeactivated();
        }
    }
}