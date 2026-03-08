using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class PdfFormConstantsController : ViewController
    {
        public PdfFormConstantsController()
        {
            TargetObjectType = typeof(PdfFormConstant);
            TargetViewType = ViewType.ListView;

            SimpleAction refreshCacheAction = new SimpleAction(this, "RefreshPdfConstantsCache", PredefinedCategory.Tools);
            refreshCacheAction.Caption = "Refresh Cache";
            refreshCacheAction.ImageName = "Action_Refresh";
            refreshCacheAction.Execute += RefreshCacheAction_Execute;
        }

        private void RefreshCacheAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            PdfFormConstants.RefreshCache(View.ObjectSpace);
            Application.ShowViewStrategy.ShowMessage("PDF Constants cache refreshed successfully.", InformationType.Success);
        }
    }
}