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

            if (item.ExpirationDate.HasValue && startDate.HasValue)
            {
                double totalDays = (item.ExpirationDate.Value.Date - startDate.Value.Date).Days;
                if (totalDays > 0)
                {
                    double elapsedDays = (DateTime.Today - startDate.Value.Date).Days;
                    if (objectSpace != null)
                    {
                        var threshold = (double)SystemSettings.GetInstance(objectSpace).ExpirationWarningThreshold;
                        if (elapsedDays / totalDays >= threshold)
                        {
                            return ExpirationState.ExpiringSoon;
                        }
                    }
                }
                else
                {
                    return ExpirationState.ExpiringSoon;
                }
            }
            return ExpirationState.Active;
        }
    }
}