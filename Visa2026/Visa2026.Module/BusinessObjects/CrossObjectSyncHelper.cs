using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering.Helpers;

namespace Visa2026.Module.BusinessObjects
{
    public static class CrossObjectSyncHelper
    {
        // Registry to hold the rules for each type
        private static readonly Dictionary<Type, List<ISyncRule>> _rules = new();

        // Static constructor to register the default rules for existing objects
        static CrossObjectSyncHelper()
        {
            RegisterRule<WorkPermitItem>(
                onSave: wpi =>
                {
                    if (wpi.WorkPermit?.Application != null && wpi.Employee != null)
                    {
                        var appItem = wpi.WorkPermit.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == wpi.Employee.ID);
                        if (appItem != null) appItem.WorkPermitItemIsIssued = true;
                    }
                },
                onDelete: wpi =>
                {
                    if (wpi.WorkPermit?.Application != null && wpi.Employee != null)
                    {
                        var appItem = wpi.WorkPermit.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == wpi.Employee.ID);
                        if (appItem != null) appItem.WorkPermitItemIsIssued = false;
                    }
                });

            RegisterRule<InvitationItem>(
                onSave: ii =>
                {
                    if (ii.Invitation?.Application != null && ii.Person != null)
                    {
                        var appItem = ii.Invitation.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ii.Person.ID);
                        if (appItem != null) appItem.InvitationItemIsIssued = true;
                    }
                },
                onDelete: ii =>
                {
                    if (ii.Invitation?.Application != null && ii.Person != null)
                    {
                        var appItem = ii.Invitation.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ii.Person.ID);
                        if (appItem != null) appItem.InvitationItemIsIssued = false;
                    }
                });

            RegisterRule<RejectionItem>(
                onSave: ri =>
                {
                    if (ri.Rejection?.Application != null && ri.Person != null)
                    {
                        var appItem = ri.Rejection.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ri.Person.ID);
                        if (appItem != null) appItem.RejectionIssued = true;
                    }
                },
                onDelete: ri =>
                {
                    if (ri.Rejection?.Application != null && ri.Person != null)
                    {
                        var appItem = ri.Rejection.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == ri.Person.ID);
                        if (appItem != null) appItem.RejectionIssued = false;
                    }
                });

            RegisterRule<Visa>(
                onSave: visa =>
                {
                    // 1. Update InvitationItem
                    if (visa.HasInvitation && visa.Invitation != null && visa.Passport?.Person != null)
                    {
                        var invitationItem = visa.Invitation.InvitationItems.FirstOrDefault(invItem => invItem.Person?.ID == visa.Passport.Person.ID);
                        if (invitationItem != null) invitationItem.IsUsed = true;
                    }
                    // 2. Update ApplicationItem
                    if (visa.Application != null && visa.Passport?.Person != null)
                    {
                        var appItem = visa.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == visa.Passport.Person.ID);
                        if (appItem != null) appItem.VisaIssued = true;
                    }
                },
                onDelete: visa =>
                {
                    // 1. Revert InvitationItem
                    if (visa.HasInvitation && visa.Invitation != null && visa.Passport?.Person != null)
                    {
                        var invitationItem = visa.Invitation.InvitationItems.FirstOrDefault(invItem => invItem.Person?.ID == visa.Passport.Person.ID);
                        if (invitationItem != null) invitationItem.IsUsed = false;
                    }
                    // 2. Revert ApplicationItem
                    if (visa.Application != null && visa.Passport?.Person != null)
                    {
                        var appItem = visa.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == visa.Passport.Person.ID);
                        if (appItem != null) appItem.VisaIssued = false;
                    }
                });
        }

        /// <summary>
        /// Registers a new synchronization rule for a specific business object type.
        /// </summary>
        public static void RegisterRule<T>(Action<T> onSave, Action<T> onDelete) where T : BaseObject
        {
            if (!_rules.ContainsKey(typeof(T)))
            {
                _rules[typeof(T)] = new List<ISyncRule>();
            }
            _rules[typeof(T)].Add(new SyncRule<T>(onSave, onDelete));
        }

        public static void SyncOnSave(BaseObject sourceObject)
        {
            if (sourceObject == null) return;
            var type = sourceObject.GetType();

            // Iterate through registered rules and execute those that match the object type
            foreach (var kvp in _rules)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    foreach (var rule in kvp.Value)
                    {
                        rule.OnSave(sourceObject);
                    }
                }
            }

            // Execute Dynamic DB Rules
            ExecuteDbRules(sourceObject, SyncTriggerType.Save);

            // Execute Update-specific rules if the object is not new
            if (sourceObject is IObjectSpaceLink link && !link.ObjectSpace.IsNewObject(sourceObject))
            {
                ExecuteDbRules(sourceObject, SyncTriggerType.Update);
            }
        }

        public static void SyncOnDelete(BaseObject sourceObject)
        {
            if (sourceObject == null) return;
            var type = sourceObject.GetType();

            foreach (var kvp in _rules)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    foreach (var rule in kvp.Value)
                    {
                        rule.OnDelete(sourceObject);
                    }
                }
            }

            // Execute Dynamic DB Rules
            ExecuteDbRules(sourceObject, SyncTriggerType.Delete);
        }

        private static void ExecuteDbRules(BaseObject sourceObject, SyncTriggerType trigger)
        {
            // We need an ObjectSpace to query the rules.
            if (sourceObject is not IObjectSpaceLink link || link.ObjectSpace == null) return;

            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper] Executing DB rules for {sourceObject.GetType().Name} (ID: {((BaseObject)sourceObject).ID}) on trigger: {trigger}");

            var typeName = sourceObject.GetType().FullName;
            
            // 1. Fetch active rules for this type and trigger
            // Note: In a high-load scenario, you might want to cache these rules to avoid DB hits on every save.
            var rules = link.ObjectSpace.GetObjectsQuery<SyncRule>()
                .Where(r => r.IsActive && r.SourceTypeFullName == typeName && r.TriggerType == trigger)
                .ToList();

            if (rules.Count == 0) return;

            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper] Found {rules.Count} matching DB rule(s).");

            foreach (var rule in rules)
            {
                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]  -> Evaluating rule: '{rule.Name}' (ID: {rule.ID})");
                try
                {
                    // 1.5 Check Source Property Value (if defined)
                    if (!string.IsNullOrEmpty(rule.SourceProperty) && !string.IsNullOrEmpty(rule.SourceValue))
                    {
                        var sourceProp = sourceObject.GetType().GetProperty(rule.SourceProperty);
                        if (sourceProp != null)
                        {
                            var currentValue = sourceProp.GetValue(sourceObject)?.ToString();
                            if (currentValue != rule.SourceValue)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - SKIPPED: Source property '{rule.SourceProperty}' value '{currentValue}' does not match required value '{rule.SourceValue}'.");
                                continue;
                            }
                        }
                    }

                    // 2. Check Source Criteria (if defined)
                    if (!string.IsNullOrEmpty(rule.SourceCriteria))
                    {
                        var evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(sourceObject), CriteriaOperator.Parse(rule.SourceCriteria));
                        if (!(bool)evaluator.Evaluate(sourceObject)) {
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - SKIPPED: Source object does not match criteria '{rule.SourceCriteria}'.");
                            continue;
                        }
                    }

                    // 3. Navigate to Target (Handle nested paths like "WorkPermit.Application.ApplicationItems")
                    object currentContext = sourceObject;
                    foreach (var part in rule.TargetPath.Split('.'))
                    {
                        if (currentContext == null) break;
                        var prop = currentContext.GetType().GetProperty(part);
                        if (prop == null) { currentContext = null; break; }
                        currentContext = prop.GetValue(currentContext);
                    }

                    if (currentContext == null) {
                        System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - SKIPPED: Target path '{rule.TargetPath}' resulted in a null object.");
                        CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, $"Target path '{rule.TargetPath}' resulted in a null object.");
                        continue;
                    }

                    // 4. Handle Collections vs Single Objects
                    IEnumerable<object> targets;
                    if (currentContext is System.Collections.IEnumerable list && !(currentContext is string))
                    {
                        targets = list.Cast<object>();
                    }
                    else
                    {
                        targets = new[] { currentContext };
                    }

                    // 5. Filter Targets based on Match Criteria
                    if (!string.IsNullOrEmpty(rule.TargetMatchCriteria))
                    {
                        // Use Regex to replace @Source parameters with values
                        var parameters = new List<object>();
                        var processedString = Regex.Replace(rule.TargetMatchCriteria, @"@Source\.([\w\.]+)", match =>
                        {
                            var path = match.Groups[1].Value;
                            var sourceEvaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(sourceObject.GetType()), CriteriaOperator.Parse(path));
                            var value = sourceEvaluator.Evaluate(sourceObject);
                            parameters.Add(value);
                            return "?";
                        });
                        var processedCriteria = CriteriaOperator.Parse(processedString, parameters.ToArray());
                        
                        // Evaluate the processed criteria against the target objects
                        var targetType = targets.FirstOrDefault()?.GetType();
                        if (targetType != null)
                        {
                            var evaluatorContextDescriptor = new EvaluatorContextDescriptorDefault(targetType);
                            var evaluator = new ExpressionEvaluator(evaluatorContextDescriptor, processedCriteria, false);
                            targets = targets.Where(t => (bool)evaluator.Evaluate(t)).ToList();
                        }
                        System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - Found {targets.Count()} target(s) after applying match criteria '{rule.TargetMatchCriteria}'.");
                    }

                    // 6. Apply Updates
                    int updatedCount = 0;
                    foreach (var target in targets)
                    {
                        var targetProp = target.GetType().GetProperty(rule.TargetProperty);
                        if (targetProp != null && targetProp.CanWrite)
                        {
                            // Convert string value to target type
                            var value = TypeDescriptor.GetConverter(targetProp.PropertyType).ConvertFromInvariantString(rule.TargetValue);
                            var targetId = (target is BaseObject bo) ? bo.ID.ToString() : "N/A";
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - UPDATING: Target {target.GetType().Name} (ID: {targetId}), setting property '{rule.TargetProperty}' to '{value}'.");
                            targetProp.SetValue(target, value);
                            updatedCount++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - WARNING: Target property '{rule.TargetProperty}' not found or not writeable on {target.GetType().Name}.");
                            CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, $"Target property '{rule.TargetProperty}' not found or not writeable on {target.GetType().Name}.");
                        }
                    }

                    if (updatedCount > 0)
                    {
                        CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Success, $"Successfully updated {updatedCount} target(s).");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the save operation
                    System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]  - ERROR executing SyncRule '{rule.Name}' (ID: {rule.ID}): {ex.Message}");
                    CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Error, $"Error executing rule: {ex.Message}", ex.ToString());
                }
            }
        }

        private static void CreateLog(IObjectSpace objectSpace, SyncRule rule, BaseObject sourceObject, SyncRuleLogStatus status, string message, string details = null)
        {
            var log = objectSpace.CreateObject<SyncRuleLog>();
            log.SyncRule = rule;
            log.Date = DateTime.Now;
            log.SourceObjectId = sourceObject.ID.ToString();
            log.Status = status;
            log.Message = message;
            log.Details = details;
        }

        // Internal interface to handle generic rules
        private interface ISyncRule
        {
            void OnSave(BaseObject obj);
            void OnDelete(BaseObject obj);
        }

        // Generic implementation of the rule
        private class SyncRule<T> : ISyncRule where T : BaseObject
        {
            private readonly Action<T> _onSave;
            private readonly Action<T> _onDelete;

            public SyncRule(Action<T> onSave, Action<T> onDelete)
            {
                _onSave = onSave;
                _onDelete = onDelete;
            }

            public void OnSave(BaseObject obj) => _onSave?.Invoke((T)obj);
            public void OnDelete(BaseObject obj) => _onDelete?.Invoke((T)obj);
        }
    }
}