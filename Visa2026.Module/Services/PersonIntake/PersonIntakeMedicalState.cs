using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Medical-record state for the person intake wizard.
/// Codes come from <see cref="MedicalRecordStateEvaluator"/>.
/// </summary>
public static class PersonIntakeMedicalState
{
    public const string Active = "Active";
    public const string ExpiringSoon = "ExpiringSoon";
    public const string Expired = "Expired";
    public const string Previous = "Archived";

    public static string Code(MedicalRecord record, StateEvaluationSettings settings)
    {
        if (record == null)
            return string.Empty;

        settings ??= StateEvaluationSettings.FromObjectSpace(null);
        return MedicalRecordStateEvaluator.Evaluate(record, settings).StateCode ?? string.Empty;
    }

    public static bool ShowsDays(string code) =>
        code is Active or ExpiringSoon or Expired;
}
