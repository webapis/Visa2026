using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Controller that dynamically filters report visibility based on rules defined in the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When every predefined report is deactivated (<c>item.Active["VisibilityCriteria"] = false</c>),
    /// the platform hides the whole <see cref="PrintSelectionBaseController.ShowInReportAction"/> (the “Show In Report”
    /// button disappears). That often appears only after selecting a row because
    /// <see cref="UpdateReportVisibility"/> returns early when there is no current/selected object, so criteria
    /// are not applied and the action stays in its default state.
    /// </para>
    /// <para>
    /// If the button vanishes after selection, check <see cref="ReportVisibility.VisibilityCriteria"/> against the
    /// real <c>Application.ApplicationType.Name</c> for that <see cref="ApplicationItem"/> — e.g. a “Visa” application
    /// will not match criteria written only for <c>App_Inv</c>, so every item report can become inactive at once.
    /// </para>
    /// <para>
    /// Assigning <c>item.Active["VisibilityCriteria"]</c> when the value is unchanged still notifies the action list;
    /// on Blazor that can cause the toolbar or layout to “tremble”. Only write when different, and only
    /// <c>RemoveItem</c> when the key exists.
    /// </para>
    /// </remarks>
    public class ShowReportController : ViewController
    {
        private PrintSelectionBaseController printSelectionController;

        /// <summary>
        /// Prevents re-entrancy when <see cref="PrintSelectionBaseController.ShowInReportAction"/> raises
        /// <c>ItemsChanged</c> while we assign <c>item.Active["VisibilityCriteria"]</c> — that can look like a
        /// “recursive” refresh or flicker in the Blazor UI.
        /// </summary>
        private bool isUpdatingReportVisibility;

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

            // Re-evaluate visibility when the current object or selection changes
            View.CurrentObjectChanged += View_CurrentObjectChanged;
            if (View is ListView listView)
                listView.SelectionChanged += View_CurrentObjectChanged;
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
            if (printSelectionController == null || isUpdatingReportVisibility) return;

            var cacheService = Application.ServiceProvider.GetService<IReportVisibilityCacheService>();
            if (cacheService == null) return;

            var targetType = View.ObjectTypeInfo.Type;

            // When header checkbox selects all rows, CurrentObject becomes null.
            // Fall back to the first selected object so criteria can still be evaluated.
            var currentObject = View.CurrentObject
                ?? (View is ListView lv && lv.SelectedObjects.Count > 0
                    ? lv.SelectedObjects[0]
                    : null);

            if (currentObject == null) return;
            TouchCriteriaNavigations(currentObject);

            var currentUser = SecuritySystem.CurrentUser as ApplicationUser;

            isUpdatingReportVisibility = true;
            try
            {
                foreach (ChoiceActionItem item in printSelectionController.ShowInReportAction.Items)
                {
                    var rules = GetReportRules(item, targetType, cacheService).ToList();

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
                        // Skip redundant writes — BoolList updates refresh Blazor chrome and can feel like UI “trembling”.
                        if (!item.Active.Contains("VisibilityCriteria") || item.Active["VisibilityCriteria"] != isVisible)
                            item.Active["VisibilityCriteria"] = isVisible;
                    }
                    else if (item.Active.Contains("VisibilityCriteria"))
                    {
                        item.Active.RemoveItem("VisibilityCriteria");
                    }
                }
            }
            finally
            {
                isUpdatingReportVisibility = false;
            }
        }

        /// <summary>
        /// <see cref="ReportVisibility"/> rows are keyed by the report registration name in the database, while
        /// UI captions may use <c>ReportDataV2.DisplayName</c> (or localized text). Try several keys so rules still apply.
        /// </summary>
        private static IEnumerable<ReportVisibility> GetReportRules(
            ChoiceActionItem item,
            Type targetType,
            IReportVisibilityCacheService cacheService)
        {
            foreach (var key in EnumerateReportLookupKeys(item))
            {
                var rules = cacheService.GetReportVisibilities(key, targetType);
                if (rules.Any())
                    return rules;
            }

            return Enumerable.Empty<ReportVisibility>();
        }

        private static IEnumerable<string> EnumerateReportLookupKeys(ChoiceActionItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Id))
                yield return item.Id.Trim();

            var data = item.Data;
            if (data != null)
            {
                foreach (var prop in new[] { "DisplayName", "Name" })
                {
                    var v = TryGetPublicStringProperty(data, prop);
                    if (!string.IsNullOrWhiteSpace(v))
                        yield return v!.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Caption))
                yield return item.Caption.Trim();
        }

        private static string? TryGetPublicStringProperty(object target, string propertyName)
        {
            var p = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return p?.GetValue(target) as string;
        }

        /// <summary>
        /// Criteria like <c>[Application.ApplicationType.Name] = 'App_Inv'</c> need navigations loaded on EF proxies.
        /// </summary>
        private void TouchCriteriaNavigations(object obj)
        {
            try
            {
                if (obj is ApplicationItem ai)
                {
                    _ = ai.Application?.ApplicationType?.Name;
                }
                else if (obj is Application app)
                {
                    _ = app.ApplicationType?.Name;
                }
            }
            catch
            {
                // Criteria evaluation still runs; this only hints the context to load associations.
            }
        }

        protected override void OnDeactivated()
        {
            if (printSelectionController != null)
            {
                printSelectionController.ShowInReportAction.ItemsChanged -= ShowInReportAction_ItemsChanged;
            }
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            if (View is ListView listView)
                listView.SelectionChanged -= View_CurrentObjectChanged;
            base.OnDeactivated();
        }
    }
}
