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
            // Lock the IsEmployee property. Its value is determined by the context from which it was created.
            var isEmployeeEditor = View.FindItem("IsEmployee") as PropertyEditor;
            if (isEmployeeEditor != null)
            {
                isEmployeeEditor.AllowEdit["PersonTypeIsFixed"] = false;
            }
        }
    }
}