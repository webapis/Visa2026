using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class SyncRuleController : ViewController
    {
        public SyncRuleController()
        {
            TargetObjectType = typeof(SyncRule);
            
            var cloneAction = new SimpleAction(this, "CloneSyncRule", PredefinedCategory.Edit);
            cloneAction.ImageName = "Clone";
            cloneAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            cloneAction.Execute += CloneAction_Execute;
        }

        private void CloneAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var originalRule = (SyncRule)e.CurrentObject;
            var os = Application.CreateObjectSpace(typeof(SyncRule));
            var newRule = os.CreateObject<SyncRule>();

            newRule.Name = $"{originalRule.Name} - Copy";
            
            // Copy Source Configuration
            newRule.SourceType = originalRule.SourceType;
            newRule.SourceProperty = originalRule.SourceProperty;
            newRule.SourceValue = originalRule.SourceValue;
            newRule.TriggerType = originalRule.TriggerType;
            newRule.SourceCriteria = originalRule.SourceCriteria;
            
            // Copy Target Configuration
            newRule.TargetPath = originalRule.TargetPath;
            newRule.TargetMatchCriteria = originalRule.TargetMatchCriteria;
            newRule.TargetType = originalRule.TargetType;
            newRule.TargetProperty = originalRule.TargetProperty;
            newRule.TargetValue = originalRule.TargetValue;
            
            newRule.IsActive = false; // Default to inactive so user can edit before enabling

            var detailView = Application.CreateDetailView(os, newRule);
            e.ShowViewParameters.CreatedView = detailView;
        }
    }
}