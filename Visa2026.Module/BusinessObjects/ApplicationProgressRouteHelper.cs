using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Localization;

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

        public static IReadOnlyList<string> GetAllowedStateCodes(Application? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return GetAllStateCodes();

            var depth = ApplicationProgressProfileResolver.GetMinistryReviewDepth(application);
            return GetAllowedStateCodes(route.Value, depth);
        }

        public static IReadOnlyList<string> GetAllowedLocationCodes(Application? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return GetAllLocationCodes();

            var depth = ApplicationProgressProfileResolver.GetMinistryReviewDepth(application);
            return GetAllowedLocationCodes(route.Value, depth);
        }

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

        public static bool IsStateCodeAllowed(Application? application, string? stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode))
                return false;

            return GetAllowedStateCodes(application)
                .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsLocationCodeAllowed(Application? application, string? locationCode)
        {
            if (string.IsNullOrWhiteSpace(locationCode))
                return false;

            return GetAllowedLocationCodes(application)
                .Contains(locationCode.Trim(), StringComparer.OrdinalIgnoreCase);
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

        public static bool IsStateAllowed(Application? application, ApplicationState? state) =>
            state != null && IsStateCodeAllowed(application, state.Code);

        public static bool IsLocationAllowed(Application? application, ApplicationLocation? location) =>
            location != null && IsLocationCodeAllowed(application, location.Code);

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

            if (progress.Location != null && !IsLocationAllowed(app, progress.Location))
            {
                errorMessage = VisaUiMessages.Format(
                    "ApplicationProgress.LocationNotAllowedForRoute",
                    progress.Location.Code ?? progress.Location.ToString(),
                    FormatProgressRouteLabel(GetTypePickerRouteFilter(app)));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Suggested second progress row after initial office preparation.
        /// </summary>
        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            Application? application)
        {
            var route = GetTypePickerRouteFilter(application);
            if (!route.HasValue)
                return null;

            if (ApplicationProgressProfileResolver.RequiresProjectContract(application)
                && application?.ProjectContract == null)
                return null;

            return route.Value == ApplicationProgressRouteKind.DirectToMigrationService
                ? (ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressLocationCodes.AtMigrationService)
                : (ApplicationProgressStateCodes.Review1Started, ApplicationProgressLocationCodes.AtMinistry1);
        }

        /// <summary>
        /// Suggested second progress row after initial office preparation (by type only).
        /// </summary>
        public static (string StateCode, string LocationCode)? GetSuggestedNextAfterOfficePreparation(
            ApplicationType? applicationType) =>
            applicationType == null
                ? null
                : GetSuggestedNextAfterOfficePreparation(new Application { ApplicationType = applicationType });

        private static IReadOnlyList<string> GetAllStateCodes() =>
            SharedStateCodes
                .Concat(FirstMinistryStateCodes)
                .Concat(SecondMinistryStateCodes)
                .ToArray();

        private static IReadOnlyList<string> GetAllLocationCodes() =>
            new[]
            {
                ApplicationProgressLocationCodes.AtOffice,
                ApplicationProgressLocationCodes.AtMinistry1,
                ApplicationProgressLocationCodes.AtMinistry2,
                ApplicationProgressLocationCodes.AtMigrationService
            };

        private static string FormatProgressRouteLabel(ApplicationProgressRouteKind? route) =>
            route == ApplicationProgressRouteKind.DirectToMigrationService
                ? VisaUiMessages.Get("ApplicationProgressRoute.DirectToMigrationService")
                : route == ApplicationProgressRouteKind.ViaMinistries
                    ? VisaUiMessages.Get("ApplicationProgressRoute.ViaMinistries")
                    : VisaUiMessages.Get("ApplicationProgressRoute.Unknown");

        /// <summary>
        /// When the user adds a second progress row right after the initial office-preparation step,
        /// pre-fills state/location to the route's typical next step.
        /// </summary>
        public static void TryApplySuggestedDefaultsAfterOfficePreparation(ApplicationProgress progress)
        {
            if (progress.Application == null || progress.State != null || progress.Location != null)
                return;

            var objectSpace = ObjectSpaceHelper.Get(progress.Application) ?? ObjectSpaceHelper.Get(progress);
            if (objectSpace == null)
                return;

            var siblings = progress.Application.ProgressHistory?
                .Where(p => p != progress && !objectSpace.IsObjectToDelete(p))
                .ToList();
            if (siblings == null || siblings.Count != 1)
                return;

            var onlyRow = ApplicationProgressHelper.GetLatest(siblings, objectSpace);
            if (onlyRow == null || !IsInitialOfficePreparation(onlyRow))
                return;

            var suggested = GetSuggestedNextAfterOfficePreparation(progress.Application);
            if (!suggested.HasValue)
                return;

            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == suggested.Value.StateCode);
            var location = objectSpace.GetObjectsQuery<ApplicationLocation>()
                .FirstOrDefault(l => l.Code == suggested.Value.LocationCode);
            if (state == null || location == null)
                return;

            progress.State = state;
            progress.Location = location;
        }

        private static bool IsInitialOfficePreparation(ApplicationProgress progress) =>
            progress.State != null
            && progress.Location != null
            && string.Equals(progress.State.Code, ApplicationProgressDefaults.InitialStateCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(progress.Location.Code, ApplicationProgressDefaults.InitialLocationCode, StringComparison.OrdinalIgnoreCase);

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
