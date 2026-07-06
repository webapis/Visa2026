using System;
using DevExpress.ExpressApp;
using Visa2026.Module.Services.StateEvaluation;

namespace Visa2026.Module.BusinessObjects
{
    public interface IExpirationLogic
    {
        DateTime? ExpirationDate { get; }
        int DaysRemaining { get; }
    }

    public static class ExpirationLogicHelper
    {
        /// <summary>
        /// Calendar days until <paramref name="expirationDate"/> (0 on or after expiry, or when <paramref name="forceZero"/>).
        /// </summary>
        public static int CalculateDaysRemaining(DateTime? expirationDate, bool forceZero = false)
        {
            if (forceZero || !expirationDate.HasValue)
            {
                return 0;
            }

            var days = (expirationDate.Value.Date - DateTime.Today).Days;
            return days < 0 ? 0 : days;
        }

        public static int CalculateDaysRemaining(DateTime expirationDate, bool forceZero = false) =>
            CalculateDaysRemaining((DateTime?)expirationDate, forceZero);

        public static bool IsExpired(DateTime? expirationDate) =>
            expirationDate.HasValue && expirationDate.Value.Date < DateTime.Today;

        public static bool IsExpired(IExpirationLogic? item) =>
            item != null && IsExpired(item.ExpirationDate);

        public static int DaysOverdue(DateTime? expirationDate)
        {
            if (!IsExpired(expirationDate))
            {
                return 0;
            }

            return (DateTime.Today - expirationDate!.Value.Date).Days;
        }

        public static ExpirationState CalculateExpirationState(
            IExpirationLogic item,
            string businessObjectKey,
            IObjectSpace? objectSpace)
        {
            if (IsExpired(item))
            {
                return ExpirationState.Expired;
            }

            if (objectSpace == null)
            {
                return ExpirationState.Active;
            }

            var settings = StateEvaluationSettings.FromObjectSpace(objectSpace);
            if (ExpirationEvaluationHelper.IsExpiringSoon(item, businessObjectKey, settings))
            {
                return ExpirationState.ExpiringSoon;
            }

            return ExpirationState.Active;
        }
    }
}
