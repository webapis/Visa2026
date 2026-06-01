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

            if (ExpirationEvaluationHelper.IsExpiringSoon(passport, ExpirationAlertBusinessObjectKeys.Passport, settings))
                return Make("ExpiringSoon", StateSeverity.Warning, days, id, $"Passport: Expiring Soon ({days} days remaining)");

            return Make("Active", StateSeverity.None, days, id, $"Passport: Active ({days} days remaining)");
        }

        private static BoStateResult Make(string code, StateSeverity severity, int? days, Guid? id, string label) =>
            new BoStateResult(BoType, code, severity, days, id, label);
    }
}
