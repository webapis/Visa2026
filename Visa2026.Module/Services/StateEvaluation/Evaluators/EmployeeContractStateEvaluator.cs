using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class EmployeeContractStateEvaluator
    {
        private const string BoType = "Employee Contract";

        public static BoStateResult Evaluate(EmployeeContract contract, StateEvaluationSettings settings)
        {
            if (contract == null)
                return Make("None", StateSeverity.None, null, null, "Contract: None");

            var id = contract.ID;
            var days = contract.DaysRemaining;

            if (!contract.IsActive)
                return Make("Archived", StateSeverity.None, days, id, "Contract: Archived");

            if (!contract.ExpirationDate.HasValue)
                return Make("Active", StateSeverity.None, days, id, "Contract: Active (no expiry)");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Contract: Expired ({Math.Abs(days)} days ago)");

            if (IsExpiringSoon(contract, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Contract: Expiring Soon ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Contract: Active ({days} days remaining)");
        }

        private static bool IsExpiringSoon(EmployeeContract contract, StateEvaluationSettings settings)
        {
            if (!contract.ExpirationDate.HasValue) return false;

            var totalDays = (contract.ExpirationDate.Value.Date - contract.ContractStartDate.Date).Days;
            if (totalDays > 0)
            {
                var elapsed = (DateTime.Today - contract.ContractStartDate.Date).Days;
                return (double)elapsed / totalDays >= (double)settings.ExpirationWarningThreshold;
            }

            return contract.DaysRemaining <= settings.DefaultExpiringSoonDays;
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
