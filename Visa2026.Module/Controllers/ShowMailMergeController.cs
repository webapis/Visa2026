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

            var candidateIds = new[] { "ShowRichTextMailMerge", "RichTextMailMerge", "MailMerge", "ShowMailMerge", "ShowInDocument" };

            // Robust search: Iterate manually to avoid LINQ casting issues with specific action types
            foreach (Controller controller in Frame.Controllers)
            {
                foreach (ActionBase action in controller.Actions)
                {
                    if (candidateIds.Contains(action.Id))
                    {
                        if (action is SingleChoiceAction sca)
                        {
                            mailMergeAction = sca;
                            logger?.LogInformation($"ShowMailMergeController: Found target action '{action.Id}' in controller '{controller.GetType().Name}'.");
                            break;
                        }
                    }
                }
                if (mailMergeAction != null) break;
            }
            
            // Diagnostic: Log if still missing
            if (mailMergeAction == null)
            {
                var allIds = Frame.Controllers.Cast<Controller>().SelectMany(c => c.Actions).Select(a => a.Id);
                logger?.LogInformation($"ShowMailMergeController: No mail merge action found. Available Actions: [{string.Join(", ", allIds)}]");
            }

            if (mailMergeAction == null)
            {
                logger?.LogWarning("ShowMailMergeController: Mail merge action not found on Frame. Ensure 'RichTextMailMergeDataType' is configured in Startup.cs and the 'RichTextMailMergeData' entity is registered in your DbContext.");
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

            string objectHandle = "Unknown";
            try { objectHandle = ObjectSpace.GetKeyValueAsString(currentObject); } catch { }
            logger?.LogInformation($"UpdateVisibility: Type='{targetType.Name}' Key='{objectHandle}', Items={mailMergeAction.Items.Count}");

            foreach (ChoiceActionItem item in mailMergeAction.Items)
            {
                string templateName = item.Caption?.Trim();
                
                // 1. Try exact match
                var rules = cacheService.GetVisibilityRules(templateName, targetType).ToList();
                
                // 2. If no rules found, try matching without extension (e.g. "Contract.docx" -> "Contract")
                if (!rules.Any() && !string.IsNullOrEmpty(templateName) && templateName.Contains('.'))
                {
                    string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(templateName);
                    rules = cacheService.GetVisibilityRules(nameWithoutExt, targetType).ToList();
                }

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
                            CriteriaOperator op = CriteriaOperator.Parse(rule.VisibilityCriteria);
                            bool fit = ObjectSpace.IsObjectFitForCriteria(currentObject, op) ?? false;
                            logger?.LogInformation($"    Criteria='{rule.VisibilityCriteria}' on '{objectHandle}' -> {fit}");
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