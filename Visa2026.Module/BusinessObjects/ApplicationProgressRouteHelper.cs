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
        [
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressStateCodes.ProcessIssued,
            ApplicationProgressStateCodes.ProcessRejected,
            ApplicationProgressStateCodes.ProcessCancelled
        ];

        public static IReadOnlyList<string> GetAllowedStateCodes(Application? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return GetAllStateCodes();

            var legCount = ApplicationProgressProfileResolver.GetMinistryLegCount(application);
            return GetAllowedStateCodes(route.Value, legCount);
        }

        public static IReadOnlyList<string> GetAllowedStateCodes(ApplicationType? applicationType) =>
            GetAllowedStateCodes(
                applicationType?.ApplicationProgressRoute ?? ApplicationProgressRouteKind.ViaMinistries,
                MapLegacyDepthToLegCount(applicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly));

        public static IReadOnlyList<string> GetAllowedStateCodes(
            ApplicationProgressRouteKind route,
            MinistryReviewDepth depth) =>
            GetAllowedStateCodes(route, MapLegacyDepthToLegCount(depth));

        public static IReadOnlyList<string> GetAllowedStateCodes(
            ApplicationProgressRouteKind route,
            int ministryLegCount)
        {
            if (route == ApplicationProgressRouteKind.DirectToMigrationService)
                return SharedStateCodes.ToArray();

            var legCount = Math.Clamp(ministryLegCount, 1, ApplicationProgressLegCodes.MaxLegCount);
            // leg 1: started+approved+rejected (3); legs 2+: approved+rejected (2 each)
            var list = new List<string>(SharedStateCodes.Length + 3 + Math.Max(0, legCount - 1) * 2);
            list.AddRange(SharedStateCodes);
            list.AddRange(ApplicationProgressLegCodes.GetReviewStateCodesUpToLegCount(legCount));
            return list;
        }

        public static bool IsStateCodeAllowed(Application? application, string? stateCode)
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

        public static bool IsStateAllowed(Application? application, ApplicationState? state) =>
            state != null && IsStateCodeAllowed(application, state.Code);

        public static bool TryValidateProgressStep(ApplicationProgress? progress, out string? errorMessage)
        {
            errorMessage = null;
            if (progress?.Application == null)
                return true;

            var app = progress.Application;
            if (progress.State != null && !IsStateAllowed(app, progress.State))
            {
                errorMessage = VisaUiMessages.Format(
                    "ApplicationProgress.StateNotAllowedForRoute",
                    progress.State.Code ?? progress.State.ToString(),
                    FormatProgressRouteLabel(GetTypePickerRouteFilter(app)));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Suggested first explicit progress step while still at office (no progress, or legacy prep only).
        /// </summary>
        public static string? GetSuggestedNextStateAfterOfficePreparation(Application? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return null;

            if (ApplicationProgressProfileResolver.RequiresApprovalLegProfile(application)
                && (application?.ApprovalLegProfile == null
                    || !ApprovalLegProfileMinistryHelper.HasConfiguredLegs(application.ApprovalLegProfile)))
                return null;

            if (ApplicationProgressProfileResolver.RequiresProjectContract(application)
                && application?.ProjectContract == null)
                return null;

            return route.Value == ApplicationProgressRouteKind.DirectToMigrationService
                ? ApplicationProgressStateCodes.ProcessStarted
                : ApplicationProgressLegCodes.ReviewStarted(1);
        }

        /// <summary>Legacy tuple API — LocationCode is ignored (progress is state-only).</summary>
        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            Application? application)
        {
            var state = GetSuggestedNextStateAfterOfficePreparation(application);
            if (state == null)
                return null;

            var location = string.Equals(state, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
                ? ApplicationProgressLocationCodes.AtMigrationService
                : ApplicationProgressLegCodes.AtMinistry(1);
            return (state, location);
        }

        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            ApplicationType? applicationType) =>
            applicationType == null
                ? null
                : GetSuggestedNextAfterOfficePreparation(new Application { ApplicationType = applicationType });

        private static IReadOnlyList<string> GetAllStateCodes() =>
            SharedStateCodes
                .Concat(ApplicationProgressLegCodes.GetReviewStateCodesUpToLegCount(ApplicationProgressLegCodes.MaxLegCount))
                .ToArray();

        private static int MapLegacyDepthToLegCount(MinistryReviewDepth depth) =>
            depth == MinistryReviewDepth.FirstAndSecondMinistry ? 2 : 1;

        private static string FormatProgressRouteLabel(ApplicationProgressRouteKind? route) =>
            route == ApplicationProgressRouteKind.DirectToMigrationService
                ? VisaUiMessages.Get("ApplicationProgressRoute.DirectToMigrationService")
                : route == ApplicationProgressRouteKind.ViaMinistries
                    ? VisaUiMessages.Get("ApplicationProgressRoute.ViaMinistries")
                    : VisaUiMessages.Get("ApplicationProgressRoute.Unknown");

        public static void TryApplySuggestedDefaultsAfterOfficePreparation(ApplicationProgress progress)
        {
            if (progress.Application == null || progress.State != null)
                return;

            var objectSpace = ObjectSpaceHelper.Get(progress.Application) ?? ObjectSpaceHelper.Get(progress);
            if (objectSpace == null)
                return;

            var siblings = progress.Application.ProgressHistory?
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

            var suggested = GetSuggestedNextStateAfterOfficePreparation(progress.Application);
            if (string.IsNullOrWhiteSpace(suggested))
                return;

            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == suggested);
            if (state == null)
                return;

            progress.State = state;
        }

        private static bool IsInitialOfficePreparation(ApplicationProgress progress) =>
            progress.State != null
            && string.Equals(progress.State.Code, ApplicationProgressDefaults.InitialStateCode, StringComparison.OrdinalIgnoreCase);

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