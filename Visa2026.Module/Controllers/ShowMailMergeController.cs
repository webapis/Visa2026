using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    public class ShowMailMergeController : ViewController
    {
        private SingleChoiceAction mailMergeAction;
        private ILogger<ShowMailMergeController> logger;

        protected override void OnActivated()
        {
            base.OnActivated();
            logger = Application.ServiceProvider.GetService<ILogger<ShowMailMergeController>>();

            // Search the current Frame's controllers for the mail merge action by ID.
            // This avoids a direct type reference to RichTextMailMergeController which
            // can vary by DX version and requires an additional assembly reference.
            mailMergeAction = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .OfType<SingleChoiceAction>()
                .FirstOrDefault(a => a.Id == "ShowRichTextMailMerge");

            if (mailMergeAction == null)
            {
                logger?.LogWarning("ShowMailMergeController: 'ShowRichTextMailMerge' action not found on this Frame. Visibility rules will not be applied.");
                return;
            }

            logger?.LogInformation("ShowMailMergeController: Successfully hooked into 'ShowRichTextMailMerge' action.");

            mailMergeAction.ItemsChanged += Action_ItemsChanged;
            View.CurrentObjectChanged += View_CurrentObjectChanged;

            UpdateVisibility();
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
            if (mailMergeAction == null || View?.CurrentObject == null) return;

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
                    if (!string.IsNullOrEmpty(rule.VisibilityCriteria))
                    {
                        try
                        {
                            bool fit = ObjectSpace.IsObjectFitForCriteria(currentObject, CriteriaOperator.Parse(rule.VisibilityCriteria)) ?? false;
                            logger?.LogInformation($"  Criteria: \"{rule.VisibilityCriteria}\" -> {fit}");
                            if (!fit)
                            {
                                isVisible = false;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, $"Error evaluating criteria '{rule.VisibilityCriteria}' for template '{templateName}'.");
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
                mailMergeAction = null;
            }
            if (View != null)
            {
                View.CurrentObjectChanged -= View_CurrentObjectChanged;
            }

            base.OnDeactivated();
        }
    }
}