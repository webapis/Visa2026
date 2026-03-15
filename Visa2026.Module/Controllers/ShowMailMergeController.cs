
// TODO: This controller is currently disabled because it references 'RichTextMailMergeController',
// which is a platform-specific controller (likely for WinForms) and is not available in the
// platform-agnostic 'Visa2026.Module' project. To re-enable this functionality, this controller
// needs to be moved to a platform-specific project (e.g., a .Win or .Blazor project) and
// updated to use the correct platform-specific mail merge controller and actions.
// For Blazor, the approach to customize mail merge action items is different.

/*
using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Office.Controllers; // This namespace is not available in the shared module
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Visa2026.Module.Module_Interface;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : ViewController
    {
        private RichTextMailMergeController mailMergeController;

        protected override void OnActivated()
        {
            base.OnActivated();

            // Hook into the standard Mail Merge controller
            mailMergeController = Frame.GetController<RichTextMailMergeController>();
            if (mailMergeController != null)
            {
                mailMergeController.ShowRichTextMailMergeAction.ItemsChanged += Action_ItemsChanged;
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
            if (mailMergeController == null || View.CurrentObject == null) return;

            var cacheService = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (cacheService == null) return;

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;
            var currentUser = SecuritySystem.CurrentUser as ApplicationUser;

            // Iterate through the available templates in the action
            foreach (ChoiceActionItem item in mailMergeController.ShowRichTextMailMergeAction.Items)
            {
                string templateName = item.Caption; // The template name matches the caption in the dropdown
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

                    // 2. Check roles
                    if (rule.Roles.Any())
                    {
                        if (currentUser == null || !currentUser.Roles.Any(userRole => rule.Roles.Any(requiredRole => requiredRole.ID == userRole.ID)))
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
            if (mailMergeController != null)
            {
                mailMergeController.ShowRichTextMailMergeAction.ItemsChanged -= Action_ItemsChanged;
            }
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            base.OnDeactivated();
        }
    }
}
*/
namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : DevExpress.ExpressApp.ViewController { }
}
