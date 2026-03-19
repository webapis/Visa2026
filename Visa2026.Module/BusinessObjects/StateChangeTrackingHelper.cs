using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    public static class StateChangeTrackingHelper
    {
        // Cache rules by Source Type Full Name to reduce DB queries
        private static ConcurrentDictionary<string, List<StateChangeRuleDefinition>> _ruleCache = new();

        public static void InvalidateCache()
        {
            _ruleCache.Clear();
        }

        public static void TrackOnSave(BaseObject sourceObject)
        {
            if (sourceObject == null) return;
            if (sourceObject is not IObjectSpaceLink link || link.ObjectSpace == null) return;

            // Execute generic Save rules
            ExecuteRules(sourceObject, SyncTriggerType.Save);

            // Execute Create or Update specific rules
            if (link.ObjectSpace.IsNewObject(sourceObject))
            {
                ExecuteRules(sourceObject, SyncTriggerType.Create);
            }
            else
            {
                ExecuteRules(sourceObject, SyncTriggerType.Update);
            }
        }

        public static void TrackOnDelete(BaseObject sourceObject)
        {
            if (sourceObject == null) return;
            ExecuteRules(sourceObject, SyncTriggerType.Delete);
        }

        public static void TrackOnPropertyChanged(BaseObject sourceObject, string propertyName)
        {
            if (sourceObject == null) return;
            ExecuteRules(sourceObject, SyncTriggerType.PropertyChanged, propertyName);
        }

        private static void ExecuteRules(BaseObject sourceObject, SyncTriggerType trigger, string changedPropertyName = null)
        {
            if (sourceObject is not IObjectSpaceLink link || link.ObjectSpace == null) return;

            var realType = link.ObjectSpace.GetObjectType(sourceObject);
            var typeName = realType.FullName;

            // 1. Fetch active rules (Cached)
            if (!_ruleCache.TryGetValue(typeName, out var typeRules))
            {
                try
                {
                    var loadedRules = link.ObjectSpace.GetObjectsQuery<StateChangeRule>()
                        .Where(r => r.IsActive && r.SourceTypeFullName == typeName)
                        .Select(r => new StateChangeRuleDefinition
                        {
                            ID = r.ID,
                            Name = r.Name,
                            TriggerType = r.TriggerType,
                            SourceProperty = r.SourceProperty,
                            SourceCriteria = r.SourceCriteria,
                            TargetPath = r.TargetPath,
                            TargetMatchCriteria = r.TargetMatchCriteria,
                            TargetSubPath = r.TargetSubPath,
                            State = r.State,
                            DescriptionTemplate = r.DescriptionTemplate
                        })
                        .ToList();
                    typeRules = loadedRules;
                    _ruleCache.TryAdd(typeName, typeRules);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[StateChangeTrackingHelper] Error fetching rules: {ex.Message}");
                    return;
                }
            }

            // 2. Filter Rules by Trigger
            var rules = typeRules.Where(r => r.TriggerType == trigger);

            if (trigger == SyncTriggerType.PropertyChanged && !string.IsNullOrEmpty(changedPropertyName))
            {
                rules = rules.Where(r => r.SourceProperty == changedPropertyName);
            }

            var rulesList = rules.ToList();
            if (rulesList.Count == 0) return;

            foreach (var rule in rulesList)
            {
                try
                {
                    // 3. Evaluate Source Criteria
                    if (!string.IsNullOrEmpty(rule.SourceCriteria))
                    {
                        var evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(sourceObject), CriteriaOperator.Parse(rule.SourceCriteria), false, new ICustomFunctionOperator[] { new LikeCustomFunction() });
                        if (!(bool)evaluator.Evaluate(sourceObject))
                        {
                            continue;
                        }
                    }

                    // 4. Resolve Target Objects
                    object currentContext = sourceObject;

                    if (!string.Equals(rule.TargetPath, "@Self", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var part in rule.TargetPath.Split('.'))
                        {
                            if (currentContext == null) break;
                            var prop = currentContext.GetType().GetProperty(part);
                            if (prop == null) { currentContext = null; break; }
                            currentContext = prop.GetValue(currentContext);
                        }
                    }

                    if (currentContext == null) continue;

                    IEnumerable<object> targets = (currentContext is System.Collections.IEnumerable list && !(currentContext is string))
                        ? list.Cast<object>()
                        : new[] { currentContext };

                    // 5. Filter Targets (Match Criteria)
                    if (!string.IsNullOrEmpty(rule.TargetMatchCriteria))
                    {
                        string processedString = Regex.Replace(rule.TargetMatchCriteria, @"['""]?@Source\.([\w\.]+)['""]?", match =>
                        {
                            var path = match.Groups[1].Value;
                            var sourceEvaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(realType), CriteriaOperator.Parse(path));
                            var value = sourceEvaluator.Evaluate(sourceObject);
                            return value != null ? $"'{value}'" : "null"; // Ensure value is quoted for string comparison or handled
                        });

                        var processedCriteria = CriteriaOperator.Parse(processedString);
                        var targetType = targets.FirstOrDefault()?.GetType();
                        
                        if (targetType != null && !ReferenceEquals(processedCriteria, null))
                        {
                            var evaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(targetType), processedCriteria, false, new ICustomFunctionOperator[] { new LikeCustomFunction() });
                            targets = targets.Where(t => (bool)(evaluator.Evaluate(t) ?? false)).ToList();
                        }
                    }

                    // 6. Resolve Final Targets using Sub-Path
                    List<object> finalTargets = new List<object>();
                    if (!string.IsNullOrEmpty(rule.TargetSubPath))
                    {
                        foreach (var target in targets)
                        {
                            object finalTarget = target;
                            foreach (var part in rule.TargetSubPath.Split('.'))
                            {
                                if (finalTarget == null) break;
                                var prop = finalTarget.GetType().GetProperty(part);
                                if (prop == null) { finalTarget = null; break; }
                                finalTarget = prop.GetValue(finalTarget);
                            }
                            if (finalTarget != null)
                            {
                                finalTargets.Add(finalTarget);
                            }
                        }
                    }
                    else
                    {
                        finalTargets.AddRange(targets);
                    }

                    // 7. Create Log Entries
                    foreach (var target in finalTargets)
                    {
                        var log = link.ObjectSpace.CreateObject<StateChangeLog>();
                        log.DateTime = DateTime.Now;
                        log.State = rule.State;
                        log.RuleName = rule.Name;
                        log.User = SecuritySystem.CurrentUserName;

                        log.SourceBoType = sourceObject.GetType();
                        log.SourceObjectId = sourceObject.ID.ToString();

                        if (target is BaseObject targetBo)
                        {
                            log.TargetBoType = targetBo.GetType();
                            log.TargetObjectId = targetBo.ID.ToString();
                        }
                        else
                        {
                            log.TargetBoType = target?.GetType();
                        }

                        if (!string.IsNullOrEmpty(rule.DescriptionTemplate))
                        {
                            log.Description = Regex.Replace(rule.DescriptionTemplate, @"@Source\.([\w\.]+)", match =>
                            {
                                try
                                {
                                    var path = match.Groups[1].Value;
                                    var evaluator = new ExpressionEvaluator(new EvaluatorContextDescriptorDefault(realType), CriteriaOperator.Parse(path));
                                    var val = evaluator.Evaluate(sourceObject);
                                    return val?.ToString() ?? "";
                                }
                                catch { return match.Value; }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[StateChangeTrackingHelper] Error executing rule '{rule.Name}': {ex.Message}");
                }
            }
        }
    }

    public class StateChangeRuleDefinition
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public SyncTriggerType TriggerType { get; set; }
        public string SourceProperty { get; set; }
        public string SourceCriteria { get; set; }
        public string TargetPath { get; set; }
        public string TargetMatchCriteria { get; set; }
        public string TargetSubPath { get; set; }
        public string State { get; set; }
        public string DescriptionTemplate { get; set; }
    }
}