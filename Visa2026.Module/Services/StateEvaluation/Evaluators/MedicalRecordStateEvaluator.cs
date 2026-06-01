using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class MedicalRecordStateEvaluator
    {
        private const string BoType = "Medical Record";

        public static BoStateResult Evaluate(MedicalRecord record, StateEvaluationSettings settings)
        {
            if (record == null)
                return Make("None", StateSeverity.None, null, null, "Medical: None");

            var id = record.ID;
            var days = record.DaysRemaining;

            if (record.Person != null && PersonCurrentItems.GetCurrentMedicalRecord(record.Person) != record)
                return Make("Archived", StateSeverity.None, days, id, "Medical: Archived");

            if (!record.ExpirationDate.HasValue)
                return Make("Active", StateSeverity.None, days, id, "Medical: Active (no expiry)");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Medical: Expired ({Math.Abs(days)} days ago)");

            if (ExpirationEvaluationHelper.IsExpiringSoon(record, ExpirationAlertBusinessObjectKeys.MedicalRecord, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Medical: Expiring Soon ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Medical: Active ({days} days remaining)");
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
