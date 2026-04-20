using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using AppBO = Visa2026.Module.BusinessObjects.Application;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class VisaExtensionStatusController : ObjectViewController<ListView, VisaExtensionStatus>
    {
        private readonly SimpleAction _openApplicationAction;

        public VisaExtensionStatusController()
        {
            _openApplicationAction = new SimpleAction(this, "OpenApplicationFromVisaStatus", PredefinedCategory.View)
            {
                Caption = "Open Application",
                ImageName = "Action_Edit_Object",
                SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
                ToolTip = "Open the full Application record for the selected row",
            };
            _openApplicationAction.Execute += OnOpenApplication;
        }

        private void OnOpenApplication(object sender, SimpleActionExecuteEventArgs e)
        {
            var status = View.CurrentObject as VisaExtensionStatus;
            if (status?.ApplicationID == null) return;

            var os = Application.CreateObjectSpace(typeof(AppBO));
            var app = os.GetObjectByKey<AppBO>(status.ApplicationID.Value);
            if (app == null) return;

            var svp = new ShowViewParameters
            {
                CreatedView = Application.CreateDetailView(os, app),
                TargetWindow = TargetWindow.NewWindow,
            };
            Application.ShowViewStrategy.ShowView(svp, new ShowViewSource(Frame, _openApplicationAction));
        }
    }
}
