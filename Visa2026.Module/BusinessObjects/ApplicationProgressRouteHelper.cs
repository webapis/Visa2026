using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Allowed <see cref="ApplicationState"/> codes and suggested next steps
    /// for an <see cref="ApplicationType"/> progress route.
    /// </summary>
    public static class ApplicationProfileInstanceProgressRouteHelper
    {
        public static MinistryReviewDepth NormalizeMinistryReviewDepth(
            ApplicationProfileInstanceProgressRouteKind route,
            MinistryReviewDepth depth) =>
            route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
                ? MinistryReviewDepth.None
                : depth == MinistryReviewDepth.None
                    ? MinistryReviewDepth.FirstMinistryOnly
                    : depth;

        private static readonly string[] SharedStateCodes =
        [
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            ApplicationProfileInstanceProgressStateCodes.ProcessRejected,
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled
        ];

        public static IReadOnlyList<string> GetAllowedStateCodes(ApplicationProfileInstance? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return GetAllStateCodes();

            var legCount = ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(application);
            return GetAllowedStateCodes(route.Value, legCount);
        }

        public static IReadOnlyList<string> GetAllowedStateCodes(ApplicationType? applicationType) =>
            GetAllowedStateCodes(
                applicationType?.ApplicationProfileInstanceProgressRoute ?? ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                MapLegacyDepthToLegCount(applicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly));

        public static IReadOnlyList<string> GetAllowedStateCodes(
            ApplicationProfileInstanceProgressRouteKind route,
            MinistryReviewDepth depth) =>
            GetAllowedStateCodes(route, MapLegacyDepthToLegCount(depth));

        public static IReadOnlyList<string> GetAllowedStateCodes(
            ApplicationProfileInstanceProgressRouteKind route,
            int ministryLegCount)
        {
            if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
                return SharedStateCodes.ToArray();

            var legCount = Math.Clamp(ministryLegCount, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount);
            // leg 1: started+approved+rejected (3); legs 2+: approved+rejected (2 each)
            var list = new List<string>(SharedStateCodes.Length + 3 + Math.Max(0, legCount - 1) * 2);
            list.AddRange(SharedStateCodes);
            list.AddRange(ApplicationProfileInstanceProgressLegCodes.GetReviewStateCodesUpToLegCount(legCount));
            return list;
        }

        public static bool IsStateCodeAllowed(ApplicationProfileInstance? application, string? stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode))
                return false;

            return GetAllowedStateCodes(application)
                .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsStateCodeAllowed(ApplicationType? applicationType, string? stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode))
                return false;

            return GetAllowedStateCodes(applicationType)
                .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsStateAllowed(ApplicationProfileInstance? application, ApplicationState? state) =>
            state != null && IsStateCodeAllowed(application, state.Code);

        public static bool TryValidateProgressStep(ApplicationProfileInstanceProgress? progress, out string? errorMessage)
        {
            errorMessage = null;
            if (progress?.ApplicationProfileInstance == null)
                return true;

            var app = progress.ApplicationProfileInstance;
            if (progress.State != null && !IsStateAllowed(app, progress.State))
            {
                errorMessage = VisaUiMessages.Format(
                    "ApplicationProfileInstanceProgress.StateNotAllowedForRoute",
                    progress.State.Code ?? progress.State.ToString(),
                    FormatProgressRouteLabel(GetTypePickerRouteFilter(app)));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Suggested first explicit progress step while still at office (no progress, or legacy prep only).
        /// </summary>
        public static string? GetSuggestedNextStateAfterOfficePreparation(ApplicationProfileInstance? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return null;

            if (ApplicationProfileInstanceProgressProfileResolver.RequiresApprovalLegProfile(application)
                && !ApplicationProfileInstanceProgressProfileResolver.HasConfiguredMinistryLegs(application))
                return null;

            if (ApplicationProfileInstanceProgressProfileResolver.RequiresProjectContract(application)
                && application?.ProjectContract == null)
                return null;

            return route.Value == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
                ? ApplicationProfileInstanceProgressStateCodes.ProcessStarted
                : ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1);
        }

        /// <summary>Legacy tuple API — LocationCode is ignored (progress is state-only).</summary>
        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            ApplicationProfileInstance? application)
        {
            var state = GetSuggestedNextStateAfterOfficePreparation(application);
            if (state == null)
                return null;

            var location = string.Equals(state, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
                ? ApplicationProfileInstanceProgressLocationCodes.AtMigrationService
                : ApplicationProfileInstanceProgressLegCodes.AtMinistry(1);
            return (state, location);
        }

        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            ApplicationType? applicationType) =>
            applicationType == null
                ? null
                : GetSuggestedNextAfterOfficePreparation(new ApplicationProfileInstance { ApplicationType = applicationType });

        private static IReadOnlyList<string> GetAllStateCodes() =>
            SharedStateCodes
                .Concat(ApplicationProfileInstanceProgressLegCodes.GetReviewStateCodesUpToLegCount(ApplicationProfileInstanceProgressLegCodes.MaxLegCount))
                .ToArray();

        private static int MapLegacyDepthToLegCount(MinistryReviewDepth depth) =>
            depth == MinistryReviewDepth.FirstAndSecondMinistry ? 2 : 1;

        private static string FormatProgressRouteLabel(ApplicationProfileInstanceProgressRouteKind? route) =>
            route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
                ? VisaUiMessages.Get("ApplicationProfileInstanceProgressRoute.DirectToMigrationService")
                : route == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
                    ? VisaUiMessages.Get("ApplicationProfileInstanceProgressRoute.ViaMinistries")
                    : VisaUiMessages.Get("ApplicationProfileInstanceProgressRoute.Unknown");

        public static void TryApplySuggestedDefaultsAfterOfficePreparation(ApplicationProfileInstanceProgress progress)
        {
            if (progress.ApplicationProfileInstance == null || progress.State != null)
                return;

            var objectSpace = ObjectSpaceHelper.Get(progress.ApplicationProfileInstance) ?? ObjectSpaceHelper.Get(progress);
            if (objectSpace == null)
                return;

            var siblings = progress.ApplicationProfileInstance.ProgressHistory?
                .Where(p => p != progress && !objectSpace.IsObjectToDelete(p))
                .ToList()
                ?? [];

            // Implied office: no history yet, or only a legacy IS_BEING_PREPARED seed row.
            if (siblings.Count == 0)
            {
                // ok — first explicit step
            }
            else if (siblings.Count == 1 && IsInitialOfficePreparation(siblings[0]))
            {
                // ok — leaving legacy prep
            }
            else
            {
                return;
            }

            var suggested = GetSuggestedNextStateAfterOfficePreparation(progress.ApplicationProfileInstance);
            if (string.IsNullOrWhiteSpace(suggested))
                return;

            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == suggested);
            if (state == null)
                return;

            progress.State = state;
        }

        private static bool IsInitialOfficePreparation(ApplicationProfileInstanceProgress progress) =>
            progress.State != null
            && string.Equals(progress.State.Code, ApplicationProfileInstanceProgressDefaults.InitialStateCode, StringComparison.OrdinalIgnoreCase);

        public static ApplicationProfileInstanceProgressRouteKind? GetTypePickerRouteFilter(ApplicationProfileInstance? application) =>
            ApplicationProfileConfigurationResolver.GetProgressRoute(application);
    }
}