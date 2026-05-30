using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    // When a DetailView of a person current-item row is saved and closed,
    // refresh the source ListView so sibling IsActive changes are visible.
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
                    if (IsCurrentPersonItemType(detailView.ObjectTypeInfo.Type))
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

        private static bool IsCurrentPersonItemType(Type type) =>
            typeof(ICurrentPersonItem).IsAssignableFrom(type);
    }
}