using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.StateEvaluation
{
    public class StateEvaluationSettings
    {
        public decimal ExpirationWarningThreshold { get; }
        public int DefaultExpiringSoonDays { get; }

        public StateEvaluationSettings(decimal expirationWarningThreshold, int defaultExpiringSoonDays)
        {
            ExpirationWarningThreshold = expirationWarningThreshold;
            DefaultExpiringSoonDays = defaultExpiringSoonDays;
        }

        public static StateEvaluationSettings FromSystemSettings(SystemSettings? s) =>
            new StateEvaluationSettings(
                s?.ExpirationWarningThreshold ?? SystemSettings.DefaultExpirationWarningThreshold,
                s?.DefaultExpiringSoonDays ?? SystemSettings.DefaultDefaultExpiringSoonDays
            );
    }
}