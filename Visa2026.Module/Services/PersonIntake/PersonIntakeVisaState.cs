using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Visa state for the person intake wizard.
/// Codes come from <see cref="VisaStateEvaluator"/>.
/// </summary>
public static class PersonIntakeVisaState
{
    public const string Active = "Active";
    public const string ExpiringSoon = "ExpiringSoon";
    public const string ExpiringSoonNotRequired = "ExpiringSoonNotRequired";
    public const string ExtensionRequired = "ExtensionApplicationRequired";
    public const string Extended = "Extended";
    public const string Changed = "Changed";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
    public const string Previous = "Archived";

    public static string Code(Visa visa, StateEvaluationSettings settings)
    {
        if (visa == null)
            return string.Empty;

        settings ??= StateEvaluationSettings.FromObjectSpace(null);
        return VisaStateEvaluator.Evaluate(visa, settings).StateCode ?? string.Empty;
    }

    /// <summary>Days remaining is meaningful for the current visa, including one past its date.</summary>
    public static bool ShowsDays(string code) =>
        code is Active or ExpiringSoon or ExpiringSoonNotRequired or ExtensionRequired or Extended or Changed or Expired;
}
