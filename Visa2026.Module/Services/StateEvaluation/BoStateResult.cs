using System;

namespace Visa2026.Module.Services.StateEvaluation
{
    public class BoStateResult
    {
        public string BoType { get; }
        public string StateCode { get; }
        public StateSeverity Severity { get; }
        public int? DaysRemaining { get; }
        public Guid? BoId { get; }
        public string Label { get; }

        public BoStateResult(string boType, string stateCode, StateSeverity severity, int? daysRemaining, Guid? boId, string label)
        {
            BoType = boType;
            StateCode = stateCode;
            Severity = severity;
            DaysRemaining = daysRemaining;
            BoId = boId;
            Label = label;
        }
    }
}