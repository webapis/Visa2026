using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class WorkPermitItemStateEvaluator
    {
        private const string BoType = "WorkPermit";

        public static BoStateResult Evaluate(WorkPermitItem wp, StateEvaluationSettings settings)
        {
            if (wp == null)
                return Make("NoWorkPermit", StateSeverity.None, null, null, "Work Permit: None");

            var id = wp.ID;
            var days = wp.DaysRemaining;

            if (wp.IsCancelled)
                return Make("Cancelled", StateSeverity.Critical, days, id, "Work Permit: Cancelled");

            if (wp.Person != null && PersonCurrentItems.GetCurrentWorkPermitItem(wp.Person) != wp)
                return Make("Archived", StateSeverity.None, days, id, "Work Permit: Archived");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Work Permit: Expired ({Math.Abs(days)} days ago)");

            if (ExpirationEvaluationHelper.IsExpiringSoon(wp, ExpirationAlertBusinessObjectKeys.WorkPermitItem, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Work Permit: Expiring Soon ({days} days remaining)");

            if (ExpirationEvaluationHelper.IsExtensionApplicationRequired(wp, ExpirationAlertBusinessObjectKeys.WorkPermitItem, settings))
                return Make("ExtensionApplicationRequired", StateSeverity.Warning, days, id, $"Work Permit: Extension Application Required ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Work Permit: Active ({days} days remaining)");
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
