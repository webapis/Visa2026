using System;
using DevExpress.ExpressApp;
using Visa2026.Module.Services.StateEvaluation;

namespace Visa2026.Module.BusinessObjects
{
    public interface IExpirationLogic
    {
        DateTime? ExpirationDate { get; }
        int DaysRemaining { get; }
    }

    public static class ExpirationLogicHelper
    {
        public static ExpirationState CalculateExpirationState(
            IExpirationLogic item,
            string businessObjectKey,
            IObjectSpace? objectSpace)
        {
            if (item.DaysRemaining < 0)
            {
                return ExpirationState.Expired;
            }

            if (objectSpace == null)
            {
                return ExpirationState.Active;
            }

            var settings = StateEvaluationSettings.FromObjectSpace(objectSpace);
            if (ExpirationEvaluationHelper.IsExpiringSoon(item, businessObjectKey, settings))
            {
                return ExpirationState.ExpiringSoon;
            }

            return ExpirationState.Active;
        }
    }
}
