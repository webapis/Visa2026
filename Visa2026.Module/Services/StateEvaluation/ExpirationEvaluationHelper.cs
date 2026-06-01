using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation
{
    public static class ExpirationEvaluationHelper
    {
        public static bool IsExpiringSoon(IExpirationLogic item, string businessObjectKey, StateEvaluationSettings settings)
        {
            if (item == null || !item.ExpirationDate.HasValue || item.DaysRemaining < 0)
            {
                return false;
            }

            return item.DaysRemaining <= settings.GetExpiringSoonDays(businessObjectKey);
        }

        public static bool IsExtensionApplicationRequired(
            IExpirationLogic item,
            string businessObjectKey,
            StateEvaluationSettings settings)
        {
            if (item == null || !item.ExpirationDate.HasValue || item.DaysRemaining < 0)
            {
                return false;
            }

            var windowDays = settings.GetExtensionApplicationRequiredDays(businessObjectKey);
            if (!windowDays.HasValue)
            {
                return false;
            }

            var expiringSoonDays = settings.GetExpiringSoonDays(businessObjectKey);
            if (item.DaysRemaining <= expiringSoonDays)
            {
                return false;
            }

            return item.DaysRemaining <= windowDays.Value;
        }
    }
}
