using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Allowed <see cref="ApplicationState"/> / <see cref="ApplicationLocation"/> codes and suggested next steps
    /// for an <see cref="ApplicationType"/> progress route (Phase 2 — used by UI/validation in later phases).
    /// </summary>
    public static class ApplicationProgressRouteHelper
    {
        public static MinistryReviewDepth NormalizeMinistryReviewDepth(
            ApplicationProgressRouteKind route,
            MinistryReviewDepth depth) =>
            route == ApplicationProgressRouteKind.DirectToMigrationService
                ? MinistryReviewDepth.None
                : depth == MinistryReviewDepth.None
                    ? MinistryReviewDepth.FirstMinistryOnly
                    : depth;

        private static readonly string[] SharedStateCodes =
        {
            ApplicationProgressStateCodes.IsBeingPrepared,
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressStateCodes.ProcessIssued,
            ApplicationProgressStateCodes.ProcessRejected,
            ApplicationProgressStateCodes.ProcessCancelled
        };

        private static readonly string[] FirstMinistryStateCodes =
        {
            ApplicationProgressStateCodes.Review1Started,
            ApplicationProgressStateCodes.Review1Approved,
            ApplicationProgressStateCodes.Review1Rejected
        };

        private static readonly string[] SecondMinistryStateCodes =
        {
            ApplicationProgressStateCodes.Review2Started,
            ApplicationProgressStateCodes.Review2Approved,
            ApplicationProgressStateCodes.Review2Rejected
        };

        private static readonly string[] SharedLocationCodes =
        {
            ApplicationProgressLocationCodes.AtOffice,
            ApplicationProgressLocationCodes.AtMigrationService
        };

        public static IReadOnlyList<string> GetAllowedStateCodes(ApplicationType? applicationType) =>
            GetAllowedStateCodes(
                applicationType?.ApplicationProgressRoute ?? ApplicationProgressRouteKind.ViaMinistries,
                applicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);

        public static IReadOnlyList<string> GetAllowedStateCodes(
            ApplicationProgressRouteKind route,
            MinistryReviewDepth depth)
        {
            depth = NormalizeMinistryReviewDepth(route, depth);

            if (route == ApplicationProgressRouteKind.DirectToMigrationService)
                return SharedStateCodes.ToArray();

            var list = new List<string>(SharedStateCodes.Length + FirstMinistryStateCodes.Length + SecondMinistryStateCodes.Length);
            list.AddRange(SharedStateCodes);
            list.AddRange(FirstMinistryStateCodes);
            if (depth == MinistryReviewDepth.FirstAndSecondMinistry)
                list.AddRange(SecondMinistryStateCodes);

            return list;
        }

        public static IReadOnlyList<string> GetAllowedLocationCodes(ApplicationType? applicationType) =>
            GetAllowedLocationCodes(
                applicationType?.ApplicationProgressRoute ?? ApplicationProgressRouteKind.ViaMinistries,
                applicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);

        public static IReadOnlyList<string> GetAllowedLocationCodes(
            ApplicationProgressRouteKind route,
            MinistryReviewDepth depth)
        {
            depth = NormalizeMinistryReviewDepth(route, depth);

            if (route == ApplicationProgressRouteKind.DirectToMigrationService)
                return SharedLocationCodes.ToArray();

            var list = new List<string>(4) { ApplicationProgressLocationCodes.AtOffice };
            list.Add(ApplicationProgressLocationCodes.AtMinistry1);
            if (depth == MinistryReviewDepth.FirstAndSecondMinistry)
                list.Add(ApplicationProgressLocationCodes.AtMinistry2);
            list.Add(ApplicationProgressLocationCodes.AtMigrationService);
            return list;
        }

        public static bool IsStateCodeAllowed(ApplicationType? applicationType, string? stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode))
                return false;

            return GetAllowedStateCodes(applicationType)
                .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsLocationCodeAllowed(ApplicationType? applicationType, string? locationCode)
        {
            if (string.IsNullOrWhiteSpace(locationCode))
                return false;

            return GetAllowedLocationCodes(applicationType)
                .Contains(locationCode.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Suggested second progress row after initial office preparation (Phase 4 UI may use this).
        /// </summary>
        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            ApplicationType? applicationType)
        {
            if (applicationType == null)
                return null;

            return applicationType.ApplicationProgressRoute == ApplicationProgressRouteKind.DirectToMigrationService
                ? (ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressLocationCodes.AtMigrationService)
                : (ApplicationProgressStateCodes.Review1Started, ApplicationProgressLocationCodes.AtMinistry1);
        }

        /// <summary>
        /// Route used to filter <see cref="ApplicationType"/> in the type-code picker and quick-code resolve.
        /// </summary>
        public static ApplicationProgressRouteKind? GetTypePickerRouteFilter(Application? application)
        {
            if (application == null)
                return null;

            if (application.CreationProgressRoute.HasValue)
                return application.CreationProgressRoute.Value;

            return application.ApplicationType?.ApplicationProgressRoute;
        }
    }
}
