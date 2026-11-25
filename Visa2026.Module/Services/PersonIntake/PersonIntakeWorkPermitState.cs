using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Work-permit line state for the employee intake wizard.
/// Codes come from <see cref="WorkPermitItemStateEvaluator"/>.
/// </summary>
public static class PersonIntakeWorkPermitState
{
    public const string Active = "Active";
    public const string ExpiringSoon = "ExpiringSoon";
    public const string ExtensionRequired = "ExtensionApplicationRequired";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
    public const string Previous = "Archived";

    public static string Code(WorkPermitItem item, StateEvaluationSettings settings)
    {
        if (item == null)
            return string.Empty;

        settings ??= StateEvaluationSettings.FromObjectSpace(null);
        return WorkPermitItemStateEvaluator.Evaluate(item, settings).StateCode ?? string.Empty;
    }

    /// <summary>Days remaining is meaningful for the current permit, including one past its date.</summary>
    public static bool ShowsDays(string code) =>
        code is Active or ExpiringSoon or ExtensionRequired or Expired;
}
