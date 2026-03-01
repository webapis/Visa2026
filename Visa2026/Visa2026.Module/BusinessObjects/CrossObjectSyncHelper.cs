using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
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
        public static void SyncOnSave(BaseObject sourceObject)
        {
            if (sourceObject == null) return;

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

            // Execute Dynamic DB Rules
            ExecuteDbRules(sourceObject, SyncTriggerType.Delete);
        }

        private static void ExecuteDbRules(BaseObject sourceObject, SyncTriggerType trigger)
        {
            // We need an ObjectSpace to query the rules.
            if (sourceObject is not IObjectSpaceLink link || link.ObjectSpace == null) return;

            var realType = link.ObjectSpace.GetObjectType(sourceObject);
            var typeName = realType.FullName;

            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper] Executing DB rules for {realType.Name} (ID: {((BaseObject)sourceObject).ID}) on trigger: {trigger}");
            
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
                        var sourceProp = realType.GetProperty(rule.SourceProperty);
                        if (sourceProp != null)
                        {
                            var currentValue = sourceProp.GetValue(sourceObject)?.ToString();
                            // FIX: Use Case-Insensitive comparison to handle "True" vs "true"
                            if (!string.Equals(currentValue, rule.SourceValue, StringComparison.OrdinalIgnoreCase))
                            {
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - SKIPPED: Source property '{rule.SourceProperty}' value '{currentValue}' does not match required value '{rule.SourceValue}'.");
                                CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Info, $"Rule skipped because source property '{rule.SourceProperty}' value '{currentValue}' did not match required value '{rule.SourceValue}'.");
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
                            CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Info, $"Rule skipped because the source object did not match the criteria: {rule.SourceCriteria}");
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
                        string processedString = rule.TargetMatchCriteria;
                        
                        try 
                        {
                            processedString = Regex.Replace(rule.TargetMatchCriteria, @"['""]?@Source\.([\w\.]+)['""]?", match =>
                            {
                                var path = match.Groups[1].Value;
                                var sourceEvaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(realType), CriteriaOperator.Parse(path));
                                var value = sourceEvaluator.Evaluate(sourceObject);
                                parameters.Add(value);
                                return "?";
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - ERROR parsing criteria '{rule.TargetMatchCriteria}': {ex.Message}");
                            CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Error, $"Error parsing criteria: {ex.Message}");
                            continue;
                        }

                        var processedCriteria = CriteriaOperator.Parse(processedString, parameters.ToArray());
                        System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - Processed Criteria: {processedCriteria}");
                        
                        // Evaluate the processed criteria against the target objects
                        var targetType = targets.FirstOrDefault()?.GetType();
                        if (targetType != null && !ReferenceEquals(processedCriteria, null))
                        {
                            var evaluatorContextDescriptor = new EvaluatorContextDescriptorDefault(targetType);
                            var evaluator = new ExpressionEvaluator(evaluatorContextDescriptor, processedCriteria, false);
                            targets = targets.Where(t => (bool)(evaluator.Evaluate(t) ?? false)).ToList();
                        }

                        if (!targets.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - No targets matched criteria '{rule.TargetMatchCriteria}'.");
                            CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Info, $"No targets matched the criteria '{rule.TargetMatchCriteria}'.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]     - Found {targets.Count()} target(s) after applying match criteria '{rule.TargetMatchCriteria}'.");
                        }
                    }

                    // 6. Apply Updates
                    int updatedCount = 0;
                    foreach (var target in targets)
                    {
                        object propOwner = target;
                        PropertyInfo targetProp = null;
                        var propPath = rule.TargetProperty.Split('.');

                        for (int i = 0; i < propPath.Length; i++)
                        {
                            if (propOwner == null)
                            {
                                string failedPath = string.Join(".", propPath.Take(i));
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - WARNING: Path traversal failed. Property '{failedPath}' was null.");
                                CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, $"Path traversal failed because property '{failedPath}' is null.");
                                targetProp = null; // Mark as failed
                                break;
                            }

                            targetProp = propOwner.GetType().GetProperty(propPath[i]);
                            if (targetProp == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - WARNING: Property '{propPath[i]}' not found on type {propOwner.GetType().Name}.");
                                CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, $"Property '{propPath[i]}' not found on type {propOwner.GetType().Name} while traversing path '{rule.TargetProperty}'.");
                                break;
                            }

                            if (i < propPath.Length - 1)
                            {
                                propOwner = targetProp.GetValue(propOwner);
                            }
                        }

                        if (targetProp != null && propOwner != null)
                        {
                            if (targetProp.CanWrite)
                            {
                                var value = TypeDescriptor.GetConverter(targetProp.PropertyType).ConvertFromInvariantString(rule.TargetValue);
                                var targetId = (target is BaseObject bo) ? bo.ID.ToString() : "N/A";
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - UPDATING: Target {target.GetType().Name} (ID: {targetId}), setting property '{rule.TargetProperty}' to '{value}'.");
                                targetProp.SetValue(propOwner, value);
                                updatedCount++;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[CrossObjectSyncHelper]       - WARNING: Final target property '{rule.TargetProperty}' is not writeable.");
                                CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, $"Final target property '{rule.TargetProperty}' is not writeable.");
                            }
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

            if (rule.Logs == null)
            {
                rule.Logs = new List<SyncRuleLog>();
            }
            rule.Logs.Add(log);
        }
    }
}