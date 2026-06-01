using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Seeds the first <see cref="ApplicationProgress"/> row when an <see cref="Application"/> is created
    /// (in preparation at office).
    /// </summary>
    public static class ApplicationProgressInitializer
    {
        public static void EnsureInitialProgress(Application application, IObjectSpace objectSpace)
        {
            if (application == null || objectSpace == null)
            {
                return;
            }

            if (HasInitialProgress(application, objectSpace))
            {
                return;
            }

            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == ApplicationProgressDefaults.InitialStateCode)
                ?? objectSpace.GetObjectsQuery<ApplicationState>().FirstOrDefault(s => s.IsDefault);

            var location = objectSpace.GetObjectsQuery<ApplicationLocation>()
                .FirstOrDefault(l => l.Code == ApplicationProgressDefaults.InitialLocationCode)
                ?? objectSpace.GetObjectsQuery<ApplicationLocation>().FirstOrDefault(l => l.IsDefault);

            if (state == null || location == null)
            {
                return;
            }

            var progress = objectSpace.CreateObject<ApplicationProgress>();
            progress.Application = application;
            progress.State = state;
            progress.Location = location;
            progress.Date = application.ApplicationDate != default
                ? application.ApplicationDate
                : DateTime.Now;
            // Do not call ProgressHistory.Add — [Aggregated] adds the child when Application is set.
        }

        private static bool HasInitialProgress(Application application, IObjectSpace objectSpace)
        {
            if (application.ProgressHistory != null &&
                application.ProgressHistory.Any(MatchesInitialStep))
            {
                return true;
            }

            // New applications are not in the database yet — only inspect the aggregated collection.
            if (objectSpace.IsNewObject(application))
            {
                return false;
            }

            if (application.ID == Guid.Empty)
            {
                return false;
            }

            var stateCode = ApplicationProgressDefaults.InitialStateCode;
            var locationCode = ApplicationProgressDefaults.InitialLocationCode;

            return objectSpace.GetObjectsQuery<ApplicationProgress>()
                .Any(p =>
                    p.Application != null
                    && p.Application.ID == application.ID
                    && p.State != null
                    && p.Location != null
                    && p.State.Code == stateCode
                    && p.Location.Code == locationCode);
        }

        private static bool MatchesInitialStep(ApplicationProgress progress) =>
            progress.State != null
            && progress.Location != null
            && string.Equals(progress.State.Code, ApplicationProgressDefaults.InitialStateCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(progress.Location.Code, ApplicationProgressDefaults.InitialLocationCode, StringComparison.OrdinalIgnoreCase);
    }
}
