using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class AddressOfResidenceStateEvaluator
    {
        private const string BoType = "Address";

        public static BoStateResult Evaluate(AddressOfResidence address, StateEvaluationSettings settings)
        {
            if (address == null)
                return Make("None", StateSeverity.None, null, null, "Address: None");

            var id = address.ID;
            var days = address.DaysRemaining;

            if (!address.IsActive)
                return Make("Archived", StateSeverity.None, days, id, "Address: Archived");

            if (!address.ExpirationDate.HasValue)
                return Make("Active", StateSeverity.None, days, id, "Address: Active (no expiry)");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Address: Expired ({Math.Abs(days)} days ago)");

            if (IsExpiringSoon(address, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Address: Expiring Soon ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Address: Active ({days} days remaining)");
        }

        private static bool IsExpiringSoon(AddressOfResidence address, StateEvaluationSettings settings)
        {
            if (!address.ExpirationDate.HasValue || !address.StartDate.HasValue) return false;

            var totalDays = (address.ExpirationDate.Value.Date - address.StartDate.Value.Date).Days;
            if (totalDays > 0)
            {
                var elapsed = (DateTime.Today - address.StartDate.Value.Date).Days;
                return (double)elapsed / totalDays >= (double)settings.ExpirationWarningThreshold;
            }

            return address.DaysRemaining <= settings.DefaultExpiringSoonDays;
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
