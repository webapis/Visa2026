using System;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : ViewController<ObjectView>
    {
        private SingleChoiceAction mailMergeAction;
        private bool hookAttempted = false;
        private ILogger<ShowMailMergeController> logger;

        protected override void OnActivated()
        {
            base.OnActivated();
            logger = Application.ServiceProvider.GetService<ILogger<ShowMailMergeController>>();
            View.CurrentObjectChanged += View_CurrentObjectChanged;
            View.SelectionChanged += View_SelectionChanged;

            // Don't try to find the action here — MainWindow controllers may not be
            // fully populated yet. Defer to the first time UpdateVisibility is called,
            // which happens after the view and its current object are ready.
            UpdateVisibility();
        }

        private void EnsureActionHooked()
        {
            if (mailMergeAction != null || hookAttempted) return;
            hookAttempted = true;

            var candidateIds = new[] { "ShowRichTextMailMerge", "RichTextMailMerge", "MailMerge", "ShowMailMerge" };

            // 1. Try finding by ID first
            mailMergeAction = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .OfType<SingleChoiceAction>()
                .FirstOrDefault(a => candidateIds.Contains(a.Id));

            // 2. If not found, try finding the Controller by type name (Robustness for Blazor/Win)
            if (mailMergeAction == null)
            {
                var mmController = Frame.Controllers.Cast<Controller>().FirstOrDefault(c => c.GetType().Name.Contains("RichTextMailMergeController"));
                if (mmController != null)
                {
                    mailMergeAction = mmController.Actions.OfType<SingleChoiceAction>().FirstOrDefault();
                    if (mailMergeAction != null)
                        logger?.LogInformation($"ShowMailMergeController: Found action '{mailMergeAction.Id}' via controller '{mmController.GetType().Name}'.");
                }
            }
            
            // Diagnostic: Log if still missing
            if (mailMergeAction == null)
            {
                var allIds = Frame.Controllers.Cast<Controller>().SelectMany(c => c.Actions).Select(a => a.Id);
                logger?.LogInformation($"ShowMailMergeController: No mail merge action found. Available Actions: [{string.Join(", ", allIds)}]");
            }

            if (mailMergeAction == null)
            {
                logger?.LogWarning("ShowMailMergeController: Mail merge action not found on Frame.");
                return;
            }

            logger?.LogInformation($"ShowMailMergeController: Successfully hooked into '{mailMergeAction.Id}'.");
            mailMergeAction.ItemsChanged += Action_ItemsChanged;
        }

        private void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            // Reset hook attempt on object change — the action might appear later
            if (mailMergeAction == null) hookAttempted = false;
            UpdateVisibility();
        }
        
        private void View_SelectionChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void Action_ItemsChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            EnsureActionHooked();

            if (mailMergeAction == null) return;

            var cacheService = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (cacheService == null)
            {
                logger?.LogWarning("IMailMergeVisibilityCacheService is not available.");
                return;
            }

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;

            // In ListView, if no object is selected/focused, we cannot evaluate criteria
            if (currentObject == null) return;

            logger?.LogInformation($"UpdateVisibility: type='{targetType.Name}', items={mailMergeAction.Items.Count}");

            foreach (ChoiceActionItem item in mailMergeAction.Items)
            {
                string templateName = item.Caption?.Trim();
                var rules = cacheService.GetVisibilityRules(templateName, targetType).ToList();
                logger?.LogInformation($"  Template='{templateName}' | Rules={rules.Count}");

                bool isVisible = true;
                bool hasAppliedRules = false;

                foreach (var rule in rules)
                {
                    hasAppliedRules = true;
                    if (!string.IsNullOrEmpty(rule.VisibilityCriteria))
                    {
                        try
                        {
                            bool fit = ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria)) ?? false;
                            logger?.LogInformation($"    Criteria='{rule.VisibilityCriteria}' -> {fit}");
                            if (!fit) { isVisible = false; break; }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, $"Error evaluating criteria '{rule.VisibilityCriteria}' for '{templateName}'.");
                            isVisible = false;
                            break;
                        }
                    }
                }

                if (hasAppliedRules)
                    item.Active["VisibilityCriteria"] = isVisible;
                else
                    item.Active.RemoveItem("VisibilityCriteria");
            }
        }

        protected override void OnDeactivated()
        {
            if (mailMergeAction != null)
            {
                mailMergeAction.ItemsChanged -= Action_ItemsChanged;
                mailMergeAction = null;
            }
            if (View != null)
            {
                View.CurrentObjectChanged -= View_CurrentObjectChanged;
                View.SelectionChanged -= View_SelectionChanged;
            }
            hookAttempted = false;
            base.OnDeactivated();
        }
    }
}