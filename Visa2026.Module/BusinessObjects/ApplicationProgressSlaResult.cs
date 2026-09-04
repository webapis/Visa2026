namespace Visa2026.Module.BusinessObjects;

public readonly record struct ApplicationProfileInstanceProgressSlaResult(
    ApplicationProfileInstanceProgressSlaStatus Status,
    int? WorkingDaysInCurrentStep,
    int? MaxDaysInReview,
    int? WarningDaysBeforeMax,
    string? MinistryShortName)
{
    public string? AppearanceStateCode => Status switch
    {
        ApplicationProfileInstanceProgressSlaStatus.Warning => ApplicationProfileInstanceProgressSlaCodes.Warning,
        ApplicationProfileInstanceProgressSlaStatus.Overdue => ApplicationProfileInstanceProgressSlaCodes.Overdue,
        _ => null
    };
}
