using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Visa2026.Module.Module_Interface;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : ViewController
    {
        private SingleChoiceAction mailMergeAction;

        protected override void OnActivated()
        {
            base.OnActivated();

            // Hook into the standard Mail Merge controller via Action ID to avoid platform-specific dependencies
            mailMergeAction = Frame.Controllers.Cast<Controller>()
                .SelectMany(c => c.Actions) 
                .FirstOrDefault(a => a.Id == "ShowRichTextMailMerge") as SingleChoiceAction;

            if (mailMergeAction != null)
            {
                mailMergeAction.ItemsChanged += Action_ItemsChanged;
                UpdateVisibility();
            }

            View.CurrentObjectChanged += View_CurrentObjectChanged;
        }

        private void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void Action_ItemsChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (mailMergeAction == null || View.CurrentObject == null) return;

            var cacheService = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (cacheService == null) return;

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;

            // Iterate through the available templates in the action
            foreach (ChoiceActionItem item in mailMergeAction.Items)
            {
                string templateName = item.Caption?.Trim(); 
                var rules = cacheService.GetVisibilityRules(templateName, targetType);

                bool isVisible = true;
                bool hasAppliedRules = false;

                foreach (var rule in rules)
                {
                    hasAppliedRules = true;
                    // 1. Check criteria
                    if (!string.IsNullOrEmpty(rule.VisibilityCriteria))
                    {
                        if (ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria)) == false)
                        {
                            isVisible = false;
                            break;
                        }
                    }
                }

                if (hasAppliedRules)
                {
                    item.Active["VisibilityCriteria"] = isVisible;
                }
                else
                {
                    item.Active.RemoveItem("VisibilityCriteria");
                }
            }
        }

        protected override void OnDeactivated()
        {
            if (mailMergeAction != null)
            {
                mailMergeAction.ItemsChanged -= Action_ItemsChanged;
            }
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            base.OnDeactivated();
        }
    }
}
