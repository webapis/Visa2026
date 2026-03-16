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
        private ILogger<ShowMailMergeController> logger;

        protected override void OnActivated()
        {
            base.OnActivated();
            logger = Application.ServiceProvider.GetService<ILogger<ShowMailMergeController>>();

            // Log all available action IDs on this Frame so we can find the correct one
            var allActionIds = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .Select(a => a.Id)
                .Where(id => id.ToLower().Contains("mail") || id.ToLower().Contains("merge") || id.ToLower().Contains("rich"))
                .ToList();

            if (allActionIds.Any())
                logger?.LogInformation($"ShowMailMergeController: Mail/Merge/Rich actions on this Frame: [{string.Join(", ", allActionIds)}]");
            else
                logger?.LogInformation($"ShowMailMergeController: No mail/merge/rich actions found on this Frame for view '{View?.Id}'");

            // Search by common known IDs — extend this list based on what the log prints above
            var candidateIds = new[] { "ShowRichTextMailMerge", "RichTextMailMerge", "MailMerge", "ShowMailMerge" };

            mailMergeAction = Frame.Controllers
                .Cast<Controller>()
                .SelectMany(c => c.Actions)
                .OfType<SingleChoiceAction>()
                .FirstOrDefault(a => candidateIds.Contains(a.Id));

            if (mailMergeAction == null)
            {
                logger?.LogWarning($"ShowMailMergeController: Mail merge action not found on Frame for view '{View?.Id}'. Visibility rules will not be applied.");
                return;
            }

            logger?.LogInformation($"ShowMailMergeController: Found action '{mailMergeAction.Id}'. Hooking in.");
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
            base.OnDeactivated();
        }
    }
}