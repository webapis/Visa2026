using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    public interface IExpirationLogic
    {
        bool IsActive { get; set; }
        DateTime? ExpirationDate { get; }
        int DaysRemaining { get; }
        ExpirationState ExpirationState { get; }
    }

    public static class ExpirationLogicHelper
    {
        public static ExpirationState CalculateExpirationState(IExpirationLogic item, DateTime? startDate, IObjectSpace objectSpace)
        {
            if (!item.IsActive) return ExpirationState.Archived;
            if (item.DaysRemaining < 0) return ExpirationState.Expired;
            
            if (objectSpace == null)
            {
                return ExpirationState.Active;
            }
            
            var settings = SystemSettings.GetInstance(objectSpace);
            
            // Use percentage-based calculation if we have a start date.
            if (item.ExpirationDate.HasValue && startDate.HasValue) 
            {
                double totalDays = (item.ExpirationDate.Value.Date - startDate.Value.Date).Days;
                if (totalDays > 0)
                {
                    double elapsedDays = (DateTime.Today - startDate.Value.Date).Days;
                    var threshold = (double)settings.ExpirationWarningThreshold;
                    if (elapsedDays / totalDays >= threshold)
                    {
                        return ExpirationState.ExpiringSoon;
                    }
                }
                else
                {
                    // If totalDays is zero or negative, it's already expiring.
                    return ExpirationState.ExpiringSoon;
                }
            }
            // Otherwise, use the fixed number of days from settings.
            else if (item.DaysRemaining <= settings.DefaultExpiringSoonDays) {
                return ExpirationState.ExpiringSoon;
            }
            return ExpirationState.Active;
        }
    }
}