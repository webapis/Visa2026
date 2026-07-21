using System;
using Visa2026.Module.Appearance;

namespace Visa2026.Module.BusinessObjects;

internal readonly struct ApplicationListViewDisplayState
{
    public string PrimaryStateCode { get; init; }
    public string CurrentState { get; init; }
    public DateTime? LatestProgressDate { get; init; }
    public int? WorkingDaysInCurrentStep { get; init; }
    public string ProgressSlaStatement { get; init; }
    public int? WorkingDaysInMigrationStep { get; init; }
    public string MigrationSlaStatement { get; init; }
    public string ProgressSlaAppearanceCode { get; init; }
    public string ListRowAppearanceStateCode { get; init; }
    public string ListRowCssClass { get; init; }

    public static ApplicationListViewDisplayState Resolve(Application application)
    {
        var latest = ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(application);
        var progressSla = ApplicationProgressSlaHelper.Resolve(application, latest);
        var migrationSla = ApplicationMigrationSlaHelper.Resolve(application, latest);

        var primaryStateCode = ApplicationProgressPrimaryStateCodeResolver.ResolveFromLatest(latest) ?? string.Empty;
        var progressSlaCode = progressSla.AppearanceStateCode ?? string.Empty;
        var migrationSlaCode = migrationSla.AppearanceStateCode ?? string.Empty;
        var slaAppearanceCode = !string.IsNullOrEmpty(progressSlaCode) ? progressSlaCode : migrationSlaCode;
        var listRowAppearance = !string.IsNullOrEmpty(slaAppearanceCode) ? slaAppearanceCode : primaryStateCode;
        var listRowCssClass = ResolveListRowCssClass(listRowAppearance);

        return new ApplicationListViewDisplayState
        {
            PrimaryStateCode = primaryStateCode,
            CurrentState = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(latest) ?? string.Empty,
            LatestProgressDate = latest?.Date,
            WorkingDaysInCurrentStep = progressSla.WorkingDaysInCurrentStep,
            ProgressSlaStatement = ApplicationProgressSlaHelper.FormatStatement(progressSla),
            WorkingDaysInMigrationStep = migrationSla.WorkingDaysInCurrentStep,
            MigrationSlaStatement = ApplicationMigrationSlaHelper.FormatStatement(migrationSla),
            ProgressSlaAppearanceCode = slaAppearanceCode,
            ListRowAppearanceStateCode = listRowAppearance,
            ListRowCssClass = listRowCssClass,
        };
    }

    private static string ResolveListRowCssClass(string stateCode)
    {
        if (string.IsNullOrEmpty(stateCode)
            || !BoStateAppearanceColors.TryGet(stateCode, out var appearance))
            return string.Empty;

        return $"{appearance.RowCssClass} visa-progress-row";
    }
}