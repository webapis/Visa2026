using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    public class ClearVisibilityCacheController : ViewController
    {
        public ClearVisibilityCacheController()
        {
            var clearCacheAction = new SimpleAction(this, "ClearVisibilityCache", PredefinedCategory.View)
            {
                Caption = "Clear Visibility Cache",
                ConfirmationMessage = "Are you sure you want to clear the visibility cache for Reports and Mail Merge templates?",
                ImageName = "ModelEditor_Action_Reload"
            };
            clearCacheAction.Execute += ClearCacheAction_Execute;
        }

        protected override void OnActivated()
        {
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

            Application.ShowViewStrategy.ShowMessage("Visibility cache cleared successfully.", InformationType.Success);
        }
    }
}