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
    public class ShowMailMergeController : ViewController<DetailView>
    {
        private SingleChoiceAction mailMergeAction;
        private bool hookAttempted = false;
        private ILogger<ShowMailMergeController> logger;

        protected override void OnActivated()
        {
            base.OnActivated();
            logger = Application.ServiceProvider.GetService<ILogger<ShowMailMergeController>>();
            View.CurrentObjectChanged += View_CurrentObjectChanged;

            // Don't try to find the action here — MainWindow controllers may not be
            // fully populated yet. Defer to the first time UpdateVisibility is called,
            // which happens after the view and its current object are ready.
            UpdateVisibility();
        }

        private void EnsureActionHooked()
        {
            if (mailMergeAction != null || hookAttempted) return;
            hookAttempted = true;

            // Diagnostic: log any relevant action IDs present on Frame
            var allIds = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .Select(a => a.Id)
                .ToList();

            var relevantIds = allIds
                .Where(id => id.ToLower().Contains("mail") || id.ToLower().Contains("merge") || id.ToLower().Contains("rich"))
                .ToList();

            if (relevantIds.Any())
                logger?.LogInformation($"ShowMailMergeController: Frame relevant actions: [{string.Join(", ", relevantIds)}]");
            else
                logger?.LogInformation($"ShowMailMergeController: No mail/merge/rich actions on Frame. All action IDs: [{string.Join(", ", allIds)}]");

            var candidateIds = new[] { "ShowRichTextMailMerge", "RichTextMailMerge", "MailMerge", "ShowMailMerge" };

            mailMergeAction = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .OfType<SingleChoiceAction>()
                .FirstOrDefault(a => candidateIds.Contains(a.Id));

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

        private void Action_ItemsChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            EnsureActionHooked();

            if (mailMergeAction == null || View?.CurrentObject == null) return;

            var cacheService = Application.ServiceProvider.GetService<IMailMergeVisibilityCacheService>();
            if (cacheService == null)
            {
                logger?.LogWarning("IMailMergeVisibilityCacheService is not available.");
                return;
            }

            var targetType = View.ObjectTypeInfo.Type;
            var currentObject = View.CurrentObject;

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
            }
            hookAttempted = false;
            base.OnDeactivated();
        }
    }
}