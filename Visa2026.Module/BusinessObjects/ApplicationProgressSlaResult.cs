namespace Visa2026.Module.BusinessObjects;

public readonly record struct ApplicationProgressSlaResult(
    ApplicationProgressSlaStatus Status,
    int? WorkingDaysInCurrentStep,
    int? MaxDaysInReview,
    int? WarningDaysBeforeMax,
    string? MinistryShortName)
{
    public string? AppearanceStateCode => Status switch
    {
        ApplicationProgressSlaStatus.Warning => ApplicationProgressSlaCodes.Warning,
        ApplicationProgressSlaStatus.Overdue => ApplicationProgressSlaCodes.Overdue,
        _ => null
    };
}
