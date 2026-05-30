using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation.Evaluators
{
    public static class PassportStateEvaluator
    {
        private const string BoType = "Passport";

        public static BoStateResult Evaluate(Passport passport, StateEvaluationSettings settings)
        {
            if (passport == null)
                return Make("NoPassport", StateSeverity.None, null, null, "Passport: None");

            var id = passport.ID;
            var days = passport.DaysRemaining;

            if (passport.Person != null && PersonCurrentItems.GetCurrentPassport(passport.Person) != passport)
                return Make("Archived", StateSeverity.None, days, id, "Passport: Archived");

            if (days < 0)
                return Make("Expired", StateSeverity.Critical, days, id, $"Passport: Expired ({Math.Abs(days)} days ago)");

            if (IsExpiringSoon(passport, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Passport: Expiring Soon ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Passport: Active ({days} days remaining)");
        }

        private static bool IsExpiringSoon(Passport passport, StateEvaluationSettings settings)
        {
            if (!passport.ExpirationDate.HasValue) return false;

            if (passport.IssueDate.HasValue)
            {
                var totalDays = (passport.ExpirationDate.Value.Date - passport.IssueDate.Value.Date).Days;
                if (totalDays > 0)
                {
                    var elapsed = (DateTime.Today - passport.IssueDate.Value.Date).Days;
                    return (double)elapsed / totalDays >= (double)settings.ExpirationWarningThreshold;
                }
            }

            return passport.DaysRemaining <= settings.DefaultExpiringSoonDays;
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
