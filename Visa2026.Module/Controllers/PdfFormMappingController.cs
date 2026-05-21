using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class PdfFormMappingController : ViewController
    {
        public PdfFormMappingController()
        {
            TargetObjectType = typeof(PdfFormMapping);
            TargetViewType = ViewType.ListView;

            SimpleAction refreshMappingsAction = new SimpleAction(this, "RefreshPdfMappingsCache", PredefinedCategory.Tools);
            refreshMappingsAction.ImageName = "Action_Refresh";
            refreshMappingsAction.Execute += RefreshMappingsAction_Execute;
        }

        private void RefreshMappingsAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            PdfMappingHelper.RefreshMappingCache(View.ObjectSpace);
            Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("Cache.PdfMappingsRefreshed"), InformationType.Success);
        }
    }
}