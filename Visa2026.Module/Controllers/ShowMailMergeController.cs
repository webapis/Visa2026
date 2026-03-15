using System;
using DevExpress.Data.Filtering;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using Visa2026.Module.Module_Interface;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : ViewController
    {
        private SingleChoiceAction mailMergeAction;
        private ILogger<ShowMailMergeController> logger;
        private bool isActionHooked = false;

        protected override void OnActivated()
        {
            base.OnActivated();
            logger = Application.ServiceProvider.GetService<ILogger<ShowMailMergeController>>();

            // Subscribe to ViewControllersActivated to robustly find the action in the current Frame
            Frame.ViewControllersActivated += Frame_ViewControllersActivated;
            View.CurrentObjectChanged += View_CurrentObjectChanged;

            // Attempt to hook immediately in case controllers are already available
            HookToAction();
        }

        private void Frame_ViewControllersActivated(object sender, EventArgs e)
        {
            HookToAction();
        }

        private void HookToAction()
        {
            // Prevent multiple hooks
            if (isActionHooked)
            {
                return;
            }

            // The 'ShowRichTextMailMerge' action is part of a WindowController, so we must search the MainWindow's controllers.
            if (Application.MainWindow != null)
            {
                mailMergeAction = Application.MainWindow.Controllers.Cast<Controller>()
                    .SelectMany(c => c.Actions)
                    .FirstOrDefault(a => a.Id == "ShowRichTextMailMerge") as SingleChoiceAction;
            }

            // As a fallback, check the current Frame's controllers.
            if (mailMergeAction == null && Frame != null) {
                mailMergeAction = Frame.Controllers.Cast<Controller>()
                    .SelectMany(c => c.Actions)
                    .FirstOrDefault(a => a.Id == "ShowRichTextMailMerge") as SingleChoiceAction;
            }

            if (mailMergeAction != null)
            {
                logger?.LogInformation("Successfully found and hooked into 'ShowRichTextMailMerge' action.");
                mailMergeAction.ItemsChanged += Action_ItemsChanged;
                UpdateVisibility();
                isActionHooked = true;
            }
            else
            {
                logger?.LogWarning("'ShowRichTextMailMerge' action has not been found yet.");
            }
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
            if (mailMergeAction == null || View.CurrentObject == null)
            {
                // This is expected if the action hasn't been found yet.
                return;
            }

            var cacheService = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (cacheService == null)
            {
                logger?.LogWarning("IMailMergeVisibilityCacheService is not available.");
                return;
            }

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;

            logger?.LogInformation($"UpdateVisibility running for type '{targetType.Name}'. Action Items: {mailMergeAction.Items.Count}");

            foreach (ChoiceActionItem item in mailMergeAction.Items)
            {
                string templateName = item.Caption?.Trim(); 
                var rules = cacheService.GetVisibilityRules(templateName, targetType).ToList();
                logger?.LogInformation($"Template: '{templateName}' | Rules Found: {rules.Count}");

                bool isVisible = true;
                bool hasAppliedRules = false;

                foreach (var rule in rules)
                {
                    hasAppliedRules = true;
                    // 1. Check criteria
                    if (!string.IsNullOrEmpty(rule.VisibilityCriteria))
                    {
                        try
                        {
                            bool fit = ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria)) ?? false;
                            logger?.LogInformation($"  Evaluating Criteria: \"{rule.VisibilityCriteria}\" -> Result: {fit}");
                            if (!fit)
                            {
                                isVisible = false;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, $"Error evaluating criteria '{rule.VisibilityCriteria}' for template '{templateName}'.");
                            isVisible = false; // Hide if criteria is invalid
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
            // Unsubscribe from all events            
            if (Frame != null)
            {
                Frame.ViewControllersActivated -= Frame_ViewControllersActivated;
            }

            if (mailMergeAction != null)
            {
                mailMergeAction.ItemsChanged -= Action_ItemsChanged;
            }
            if (View != null)
            {
                View.CurrentObjectChanged -= View_CurrentObjectChanged;
            }

            // Reset state
            isActionHooked = false;
            mailMergeAction = null;

            base.OnDeactivated();
        }
    }
}
