using DevExpress.ExpressApp;
using Visa2026.Module.Services.StateEvaluation;

namespace Visa2026.Module.BusinessObjects
{
    public static class VisaValidityStateHelper
    {
        public static VisaValidityState Resolve(Visa? visa, IObjectSpace? objectSpace = null)
        {
            if (visa == null)
            {
                return VisaValidityState.Valid;
            }

            if (visa.IsCancelled)
            {
                return VisaValidityState.Cancelled;
            }

            if (visa.IsChanged)
            {
                return VisaValidityState.Changed;
            }

            if (!visa.ExpirationDate.HasValue)
            {
                return VisaValidityState.Valid;
            }

            if (visa.DaysRemaining < 0)
            {
                return VisaValidityState.Expired;
            }

            var settings = StateEvaluationSettings.FromObjectSpace(objectSpace);
            if (ExpirationEvaluationHelper.IsExpiringSoon(visa, ExpirationAlertBusinessObjectKeys.Visa, settings))
            {
                return VisaValidityState.Expiring;
            }

            return VisaValidityState.Valid;
        }
    }
}
