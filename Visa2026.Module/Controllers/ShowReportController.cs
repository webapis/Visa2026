using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Controller that dynamically filters report visibility based on rules defined in the database.
    /// </summary>
    public class ShowReportController : ViewController
    {
        private PrintSelectionBaseController printSelectionController;

        protected override void OnActivated()
        {
            base.OnActivated();

            // Access the controller responsible for the "Show In Report" action
            printSelectionController = Frame.GetController<PrintSelectionBaseController>();
            if (printSelectionController != null)
            {
                // Subscribe to ItemsChanged to handle cases where the report list is refreshed
                printSelectionController.ShowInReportAction.ItemsChanged += ShowInReportAction_ItemsChanged;
                UpdateReportVisibility();
            }

            // Re-evaluate visibility when the current object changes
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        }

        private void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            UpdateReportVisibility();
        }

        private void ShowInReportAction_ItemsChanged(object sender, EventArgs e)
        {
            UpdateReportVisibility();
        }

        private void UpdateReportVisibility()
        {
            if (printSelectionController == null || View.CurrentObject == null) return;

            var cacheService = Application.ServiceProvider.GetService<IReportVisibilityCacheService>();
            if (cacheService == null) return;

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;
            var currentUser = SecuritySystem.CurrentUser as ApplicationUser;

            foreach (ChoiceActionItem item in printSelectionController.ShowInReportAction.Items)
            {
                string reportName = item.Caption;
                var rules = cacheService.GetReportVisibilities(reportName, targetType);

                bool isVisible = true;
                bool hasAppliedRules = false;

                foreach (var rule in rules)
                {
                    hasAppliedRules = true;
                    if (!string.IsNullOrEmpty(rule.VisibilityCriteria))
                    {
                        // ObjectSpace.IsObjectFitForCriteria returns bool?
                        bool? fit = ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria));
                        if (fit == false)
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
                    // Use RemoveItem to correctly remove a key from the BoolList
                    item.Active.RemoveItem("VisibilityCriteria");
                }
            }
        }

        protected override void OnDeactivated()
        {
            if (printSelectionController != null)
            {
                printSelectionController.ShowInReportAction.ItemsChanged -= ShowInReportAction_ItemsChanged;
            }
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            base.OnDeactivated();
        }
    }
}
