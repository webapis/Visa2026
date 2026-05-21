using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers
{
    public class SyncRulesController : ViewController
    {
        private readonly SimpleAction resetRulesAction;
        private readonly SimpleAction validateRulesAction;

        public SyncRulesController()
        {
            TargetObjectType = typeof(SyncRule);
            TargetViewType = ViewType.ListView;

            resetRulesAction = new SimpleAction(this, "ResetSyncRules", PredefinedCategory.Tools);
            resetRulesAction.ImageName = "Action_ResetViewSettings";
            resetRulesAction.Execute += ResetRulesAction_Execute;

            validateRulesAction = new SimpleAction(this, "ValidateSyncRules", PredefinedCategory.Tools);
            validateRulesAction.ImageName = "Action_Validation";
            validateRulesAction.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            validateRulesAction.Execute += ValidateRulesAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            resetRulesAction.ConfirmationMessage = VisaUiMessages.Get("Confirm.ResetSyncRules");
        }

        private void ResetRulesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[SyncRulesController] 'Reset Rules' action started.");
            // Use a separate ObjectSpace to perform the update transaction independently
            IObjectSpace os = Application.CreateObjectSpace(typeof(SyncRule));
            try
            {
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Created new ObjectSpace.");
                // Instantiate the updater with the new ObjectSpace
                // Version is not strictly used in our custom logic, so a dummy version is fine
                var updater = new SyncRulesUpdater(os, new Version(1, 0, 0, 0));
                
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Calling UpdateDatabaseAfterUpdateSchema...");
                // Trigger the logic
                updater.UpdateDatabaseAfterUpdateSchema();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] UpdateDatabaseAfterUpdateSchema returned.");

                // Refresh the View to reflect changes
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] Refreshing View ObjectSpace...");
                View.ObjectSpace.Refresh();
                if (View is ListView listView)
                {
                    listView.CollectionSource.Reload();
                }
                View.Refresh();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] View ObjectSpace refreshed.");
                
                Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("SyncRules.ResetSuccess"), InformationType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRulesController] EXCEPTION: {ex}");
                Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Format("SyncRules.ResetError", ex.Message), InformationType.Error);
            }
            finally
            {
                os.Dispose();
                System.Diagnostics.Debug.WriteLine("[SyncRulesController] ObjectSpace disposed.");
            }
        }

        private void ValidateRulesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            int validCount = 0;
            int invalidCount = 0;

            foreach (SyncRule rule in e.SelectedObjects)
            {
                List<string> errors = ValidateRule(rule);
                if (errors.Count > 0)
                {
                    invalidCount++;
                    sb.AppendLine(VisaUiMessages.Format("SyncRules.RuleHeader", rule.Name));
                    foreach (var err in errors)
                    {
                        sb.AppendLine($" - {err}");
                    }
                    sb.AppendLine();
                }
                else
                {
                    validCount++;
                }
            }

            string message;
            InformationType type;

            if (invalidCount == 0)
            {
                message = VisaUiMessages.Format("SyncRules.AllValid", validCount);
                type = InformationType.Success;
            }
            else
            {
                message = VisaUiMessages.Format("SyncRules.ValidationSummary", validCount, invalidCount, sb.ToString());
                type = InformationType.Error;
            }

            Application.ShowViewStrategy.ShowMessage(message, type);
        }

        private List<string> ValidateRule(SyncRule rule)
        {
            List<string> errors = new List<string>();

            if (rule.SourceType == null)
            {
                errors.Add(VisaUiMessages.Get("SyncRules.SourceTypeMissing"));
                return errors;
            }

            // Validate Source Property
            if (!string.IsNullOrEmpty(rule.SourceProperty))
            {
                var prop = rule.SourceType.GetProperty(rule.SourceProperty);
                if (prop == null)
                {
                    errors.Add(VisaUiMessages.Format("SyncRules.SourcePropertyNotFound", rule.SourceProperty, rule.SourceType.Name));
                }
            }

            // Validate Target Path
            Type currentType = rule.SourceType;
            if (!string.IsNullOrEmpty(rule.TargetPath))
            {
                foreach (var part in rule.TargetPath.Split('.'))
                {
                    var prop = currentType.GetProperty(part);
                    if (prop == null)
                    {
                        errors.Add(VisaUiMessages.Format("SyncRules.TargetPathBroken", part, currentType.Name));
                        currentType = null;
                        break;
                    }
                    currentType = prop.PropertyType;

                    // Unwrap collection types
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(currentType) && currentType != typeof(string))
                    {
                        if (currentType.IsGenericType)
                        {
                            currentType = currentType.GetGenericArguments()[0];
                        }
                        else if (currentType.IsArray)
                        {
                            currentType = currentType.GetElementType();
                        }
                    }
                }
            }
            else
            {
                errors.Add(VisaUiMessages.Get("SyncRules.TargetPathEmpty"));
            }

            // Validate Target Type compatibility
            if (currentType != null && rule.TargetType != null)
            {
                if (!rule.TargetType.IsAssignableFrom(currentType))
                {
                    errors.Add(VisaUiMessages.Format("SyncRules.TargetTypeMismatch", currentType.Name, rule.TargetType.Name));
                }
            }

            // Validate Target Property
            if (rule.TargetType != null && !string.IsNullOrEmpty(rule.TargetProperty))
            {
                Type targetPropOwner = rule.TargetType;
                foreach (var part in rule.TargetProperty.Split('.'))
                {
                    var prop = targetPropOwner.GetProperty(part);
                    if (prop == null)
                    {
                        errors.Add(VisaUiMessages.Format("SyncRules.TargetPropertyBroken", part, targetPropOwner.Name));
                        targetPropOwner = null;
                        break;
                    }
                    targetPropOwner = prop.PropertyType;
                }
            }
            else if (string.IsNullOrEmpty(rule.TargetProperty))
            {
                errors.Add(VisaUiMessages.Get("SyncRules.TargetPropertyEmpty"));
            }

            return errors;
        }
    }
}