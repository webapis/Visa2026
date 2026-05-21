using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    public class ClearVisibilityCacheController : ViewController
    {
        public ClearVisibilityCacheController()
        {
            var clearCacheAction = new SimpleAction(this, "ClearVisibilityCache", PredefinedCategory.View)
            {
                ImageName = "ModelEditor_Action_Reload"
            };
            clearCacheAction.Execute += ClearCacheAction_Execute;
        }

        protected override void OnActivated()
        {
            foreach (ActionBase action in Actions)
            {
                if (action.Id == "ClearVisibilityCache")
                {
                    action.ConfirmationMessage = VisaUiMessages.Get("Confirm.ClearVisibilityCache");
                }
            }

            base.OnActivated();
            // Only show this action when managing Visibility objects
            bool isVisibilityConfig = View.ObjectTypeInfo != null && 
                                      (View.ObjectTypeInfo.Type == typeof(ReportVisibility) || 
                                       View.ObjectTypeInfo.Type == typeof(MailMergeVisibility));
            
            Actions["ClearVisibilityCache"].Active["ObjectType"] = isVisibilityConfig;
        }

        private void ClearCacheAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var reportCache = Application.ServiceProvider.GetService<IReportVisibilityCacheService>();
            if (reportCache != null)
            {
                reportCache.ClearCache();
            }

            var mailMergeCache = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (mailMergeCache != null)
            {
                mailMergeCache.ClearCache();
            }

            Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("Cache.VisibilityCleared"), InformationType.Success);
        }
    }
}