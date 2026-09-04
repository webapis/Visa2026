using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class PersonDetailViewController : ObjectViewController<DetailView, Person>
    {
        protected override void OnActivated()
        {
            base.OnActivated();

            bool allowEdit = false; // Default to locked

            // Allow editing only when creating a new Person from an ApplicationRosterMergeLine
            // where the ApplicationProfileInstance category is 'Both'.
            if (View.ObjectSpace.IsNewObject(View.CurrentObject))
            {
                if (View.ObjectSpace.Owner is Link link &&
                    link.ListView.CollectionSource is PropertyCollectionSource propertyCollectionSource &&
                    propertyCollectionSource.MasterObject is ApplicationRosterMergeLine appItem)
                {
                    if (appItem.ApplicationProfileInstance?.ApplicationType?.Category == ApplicationTypeCategory.Both)
                    {
                        allowEdit = true;
                    }
                }
            }

            var isEmployeeEditor = View.FindItem("IsEmployee") as PropertyEditor;
            if (isEmployeeEditor != null)
            {
                isEmployeeEditor.AllowEdit["PersonTypeIsFixedByContext"] = allowEdit;
            }
        }

    }
}