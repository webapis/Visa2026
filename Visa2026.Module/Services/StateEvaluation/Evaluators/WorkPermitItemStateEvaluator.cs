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

            if (IsExpiringSoon(wp, settings))
            {
                if (wp.IsExtended)
                    return Make("Extended", StateSeverity.Info, days, id, $"Work Permit: Extended — expiring in {days} days");

                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Work Permit: Expiring Soon ({days} days remaining)");
            }

            if (wp.IsExtended)
                return Make("Extended", StateSeverity.Info, days, id, $"Work Permit: Extended — expiring in {days} days");

            return Make("Active", StateSeverity.None, days, id, $"Work Permit: Active ({days} days remaining)");
        }

        private static bool IsExpiringSoon(WorkPermitItem wp, StateEvaluationSettings settings)
        {
            var totalDays = (wp.ExpirationDate.Date - wp.StartDate.Date).Days;
            if (totalDays > 0)
            {
                var elapsed = (DateTime.Today - wp.StartDate.Date).Days;
                return (double)elapsed / totalDays >= (double)settings.ExpirationWarningThreshold;
            }

            return wp.DaysRemaining <= settings.DefaultExpiringSoonDays;
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
