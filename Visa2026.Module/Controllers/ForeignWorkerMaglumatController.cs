using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class ForeignWorkerMaglumatController : ObjectViewController<ListView, ForeignWorkerMaglumat>
    {
        private readonly SimpleAction _openPersonAction;

        public ForeignWorkerMaglumatController()
        {
            _openPersonAction = new SimpleAction(this, "OpenPersonFromForeignWorkerMaglumat", PredefinedCategory.View)
            {
                Caption = "Open Person",
                ImageName = "BO_Person",
                SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
                ToolTip = "Open the selected person's detail view.",
            };
            _openPersonAction.Execute += OnOpenPerson;
        }

        private void OnOpenPerson(object sender, SimpleActionExecuteEventArgs e)
        {
            var row = View.CurrentObject as ForeignWorkerMaglumat;
            if (row?.PersonID == null)
                return;

            var os = Application.CreateObjectSpace(typeof(Person));
            var person = os.GetObjectByKey<Person>(row.PersonID.Value);
            if (person == null)
                return;

            var svp = new ShowViewParameters
            {
                CreatedView = Application.CreateDetailView(os, person),
                TargetWindow = TargetWindow.NewWindow,
            };
            Application.ShowViewStrategy.ShowView(svp, new ShowViewSource(Frame, _openPersonAction));
        }
    }
}