using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.ReportsV2;
using Microsoft.Extensions.DependencyInjection;
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
                // Subscribe to ItemChanged to handle cases where the report list is refreshed
                printSelectionController.ShowInReportAction.ItemsChanged += ShowInReportAction_ItemsChanged;
                UpdateReportVisibility();
            }

            // Re-evaluate visibility when the current object changes (e.g., navigating between records in DetailView)
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        }

        private void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            UpdateReportVisibility();
        }

        private void ShowInReportAction_ItemsChanged(object sender, ActionItemsChangedEventArgs e)
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

            foreach (ChoiceActionItem item in printSelectionController.ShowInReportAction.Items)
            {
                // The item.Caption corresponds to the report name registered in ReportsUpdater
                string reportName = item.Caption;
                var rule = cacheService.GetReportVisibility(reportName, targetType);

                if (rule != null && !string.IsNullOrEmpty(rule.VisibilityCriteria))
                {
                    // Evaluate if the current object (Application) matches the criteria defined in the DB
                    bool isVisible = ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria));
                    item.Active["VisibilityCriteria"] = isVisible;
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