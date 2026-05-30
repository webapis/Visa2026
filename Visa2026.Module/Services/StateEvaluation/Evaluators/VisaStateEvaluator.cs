using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class VisaStateEvaluator
    {
        private const string BoType = "Visa";

        public static BoStateResult Evaluate(Visa visa, StateEvaluationSettings settings)
        {
            if (visa == null)
                return Make("NoVisa", StateSeverity.None, null, null, "Visa: None");

            var id = visa.ID;
            var days = visa.DaysRemaining;

            if (visa.IsCancelled)
                return Make("Cancelled", StateSeverity.Critical, days, id, "Visa: Cancelled");

            var person = visa.Passport?.Person;
            if (person != null && PersonCurrentItems.GetCurrentVisa(person) != visa)
                return Make("Archived", StateSeverity.None, days, id, "Visa: Archived");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Visa: Expired ({Math.Abs(days)} days ago)");

            if (IsExpiringSoon(visa, settings))
            {
                if (visa.IsExtended)
                    return Make("Extended", StateSeverity.Info, days, id, $"Visa: Extended — expiring in {days} days");

                if (!visa.ExtensionRequired)
                    return Make("ExpiringSoonNotRequired", StateSeverity.Info, days, id, $"Visa: Expiring Soon — Extension Not Required ({days} days remaining)");

                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Visa: Expiring Soon ({days} days remaining)");
            }

            if (visa.IsExtended)
                return Make("Extended", StateSeverity.Info, days, id, $"Visa: Extended — expiring in {days} days");

            if (visa.IsChanged)
                return Make("Changed", StateSeverity.Info, days, id, $"Visa: Changed — expiring in {days} days");

            return Make("Active", StateSeverity.None, days, id, $"Visa: Active ({days} days remaining)");
        }

        private static bool IsExpiringSoon(Visa visa, StateEvaluationSettings settings)
        {
            if (!visa.ExpirationDate.HasValue) return false;

            // Percentage-based: use StartDate as the period start
            var totalDays = (visa.ExpirationDate.Value.Date - visa.StartDate.Date).Days;
            if (totalDays > 0)
            {
                var elapsed = (DateTime.Today - visa.StartDate.Date).Days;
                return (double)elapsed / totalDays >= (double)settings.ExpirationWarningThreshold;
            }

            // Fallback: fixed days
            return visa.DaysRemaining <= settings.DefaultExpiringSoonDays;
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}