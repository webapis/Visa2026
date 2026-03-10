using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // This controller ensures that when a DetailView of a SingleActiveBaseObject is saved and closed,
    // the source ListView is fully refreshed to correctly display the state of sibling objects
    // that may have been programmatically deactivated.
    public class SingleActiveDetailViewCloseController : WindowController
    {
        public SingleActiveDetailViewCloseController()
        {
            TargetWindowType = WindowType.Any;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            Application.ViewShown += Application_ViewShown;
        }

        private void Application_ViewShown(object sender, ViewShownEventArgs e)
        {
            // We are interested when a DetailView is shown in a popup
            // A non-null SourceFrame indicates the view was opened from another view (e.g., a popup from a list view).
            if (e.SourceFrame != null && e.SourceFrame.View is DetailView detailView)
            {
                // And the source is a ListView
                if (e.SourceFrame.View is ListView sourceListView)
                {
                    // And the DetailView's object type inherits from SingleActiveBaseObject
                    if (IsSingleActiveObject(detailView.ObjectTypeInfo.Type))
                    {
                        // When the DetailView is committed, refresh the source ListView
                        detailView.ObjectSpace.Committed += (s, args) => {
                            sourceListView.ObjectSpace.Refresh();
                        };
                    }
                }
            }
        }


        protected override void OnDeactivated()
        {
            Application.ViewShown -= Application_ViewShown;
            base.OnDeactivated();
        }

        private bool IsSingleActiveObject(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SingleActiveBaseObject<,>))
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}