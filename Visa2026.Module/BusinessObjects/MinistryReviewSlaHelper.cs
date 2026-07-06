using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class MinistryReviewSlaHelper
{
    public static bool TryGetEffectiveSla(IObjectSpace objectSpace, out int maxDaysInReview, out int? warningDaysBeforeMax)
    {
        maxDaysInReview = 0;
        warningDaysBeforeMax = null;

        var settings = MinistryReviewSlaSettings.TryGetInstance(objectSpace);
        if (settings == null || settings.MaxDaysInReview <= 0)
            return false;

        maxDaysInReview = settings.MaxDaysInReview;
        warningDaysBeforeMax = settings.WarningDaysBeforeMax;
        return true;
    }

    public static bool TryValidateConfigured(IObjectSpace objectSpace, out string? errorMessage)
    {
        if (!TryGetEffectiveSla(objectSpace, out var maxDays, out var warningDays))
        {
            errorMessage = VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured");
            return false;
        }

        return TryValidateSlaValues(maxDays, warningDays, out errorMessage);
    }

    public static bool TryValidateSlaValues(int maxDaysInReview, int? warningDaysBeforeMax, out string? errorMessage)
    {
        errorMessage = null;
        if (maxDaysInReview <= 0)
        {
            errorMessage = VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured");
            return false;
        }

        if (warningDaysBeforeMax is > 0 && warningDaysBeforeMax >= maxDaysInReview)
        {
            errorMessage = VisaUiMessages.Get("MinistryReviewSlaSettings.WarningDaysInvalid");
            return false;
        }

        return true;
    }
}
