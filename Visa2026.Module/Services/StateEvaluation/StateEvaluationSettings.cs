using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation
{
    public class StateEvaluationSettings
    {
        public int DefaultExpiringSoonDays { get; }

        private readonly IReadOnlyDictionary<string, ExpirationAlertRule> _rulesByKey;

        public StateEvaluationSettings(
            int defaultExpiringSoonDays,
            IReadOnlyDictionary<string, ExpirationAlertRule> rulesByKey)
        {
            DefaultExpiringSoonDays = defaultExpiringSoonDays;
            _rulesByKey = rulesByKey ?? new Dictionary<string, ExpirationAlertRule>();
        }

        public static StateEvaluationSettings FromObjectSpace(IObjectSpace? objectSpace)
        {
            if (objectSpace == null)
            {
                return new StateEvaluationSettings(
                    SystemSettings.DefaultDefaultExpiringSoonDays,
                    new Dictionary<string, ExpirationAlertRule>());
            }

            var settings = SystemSettings.TryGetInstance(objectSpace);
            return new StateEvaluationSettings(
                settings?.DefaultExpiringSoonDays ?? SystemSettings.DefaultDefaultExpiringSoonDays,
                ExpirationAlertRuleHelper.LoadRulesByKey(objectSpace));
        }

        /// <summary>Fallback when no <see cref="IObjectSpace"/> is available.</summary>
        public static StateEvaluationSettings FromSystemSettings(SystemSettings? settings) =>
            new(
                settings?.DefaultExpiringSoonDays ?? SystemSettings.DefaultDefaultExpiringSoonDays,
                new Dictionary<string, ExpirationAlertRule>());

        public int GetExpiringSoonDays(string businessObjectKey)
        {
            var rule = TryGetRule(businessObjectKey);
            return rule != null && rule.ExpiringSoonDays > 0
                ? rule.ExpiringSoonDays
                : DefaultExpiringSoonDays;
        }

        public int? GetExtensionApplicationRequiredDays(string businessObjectKey)
        {
            var rule = TryGetRule(businessObjectKey);
            return rule?.ExtensionApplicationRequiredDays is int days && days > 0 ? days : null;
        }

        private ExpirationAlertRule? TryGetRule(string businessObjectKey)
        {
            if (string.IsNullOrWhiteSpace(businessObjectKey))
            {
                return null;
            }

            return _rulesByKey.TryGetValue(businessObjectKey.Trim(), out var rule) ? rule : null;
        }
    }
}
