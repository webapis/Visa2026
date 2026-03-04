using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class PersonDetailViewController : ObjectViewController<DetailView, Person>
    {
        protected override void OnActivated()
        {
            base.OnActivated();
            // Lock the IsEmployee property if the object is not new.
            if (View.CurrentObject != null && !View.ObjectSpace.IsNewObject(View.CurrentObject))
            {
                var isEmployeeEditor = View.FindItem("IsEmployee") as PropertyEditor;
                if (isEmployeeEditor != null)
                {
                    isEmployeeEditor.AllowEdit["PersonTypeIsFixed"] = false;
                }
            }
        }
    }
}