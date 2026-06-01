using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    public static class ExpirationAlertRuleHelper
    {
        public static ExpirationAlertRule? TryGetRule(IObjectSpace? objectSpace, string businessObjectKey)
        {
            if (objectSpace == null || string.IsNullOrWhiteSpace(businessObjectKey))
            {
                return null;
            }

            var normalized = businessObjectKey.Trim();
            return objectSpace.GetObjectsQuery<ExpirationAlertRule>()
                .FirstOrDefault(r => r.BusinessObjectKey == normalized);
        }

        public static int GetExpiringSoonDays(IObjectSpace? objectSpace, string businessObjectKey)
        {
            var rule = TryGetRule(objectSpace, businessObjectKey);
            if (rule != null && rule.ExpiringSoonDays > 0)
            {
                return rule.ExpiringSoonDays;
            }

            var settings = objectSpace != null ? SystemSettings.TryGetInstance(objectSpace) : null;
            return settings?.DefaultExpiringSoonDays ?? SystemSettings.DefaultDefaultExpiringSoonDays;
        }

        public static int? GetExtensionApplicationRequiredDays(IObjectSpace? objectSpace, string businessObjectKey)
        {
            var rule = TryGetRule(objectSpace, businessObjectKey);
            return rule?.ExtensionApplicationRequiredDays is int days && days > 0 ? days : null;
        }

        public static bool IsExpiringSoon(IExpirationLogic item, string businessObjectKey, IObjectSpace? objectSpace)
        {
            if (item == null || !item.ExpirationDate.HasValue || item.DaysRemaining < 0)
            {
                return false;
            }

            return item.DaysRemaining <= GetExpiringSoonDays(objectSpace, businessObjectKey);
        }

        public static bool IsExtensionApplicationRequired(IExpirationLogic item, string businessObjectKey, IObjectSpace? objectSpace)
        {
            if (item == null || !item.ExpirationDate.HasValue)
            {
                return false;
            }

            var windowDays = GetExtensionApplicationRequiredDays(objectSpace, businessObjectKey);
            if (!windowDays.HasValue)
            {
                return false;
            }

            var days = item.DaysRemaining;
            return days >= 0 && days <= windowDays.Value;
        }

        public static IReadOnlyDictionary<string, ExpirationAlertRule> LoadRulesByKey(IObjectSpace objectSpace)
        {
            return objectSpace.GetObjectsQuery<ExpirationAlertRule>()
                .ToDictionary(r => r.BusinessObjectKey, r => r, StringComparer.OrdinalIgnoreCase);
        }
    }
}
