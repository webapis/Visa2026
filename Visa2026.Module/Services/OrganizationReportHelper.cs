using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    /// <summary>Read organization singletons for reports, PDF, and Word merge (live settings per database).</summary>
    public static class OrganizationReportHelper
    {
        public static CompanyProfile? GetCompanyProfile(IObjectSpace? objectSpace) =>
            objectSpace == null ? null : CompanyProfile.TryGetInstance(objectSpace);

        public static AuthorizedSignatory? GetSignatory(IObjectSpace? objectSpace) =>
            objectSpace == null ? null : AuthorizedSignatory.TryGetInstance(objectSpace);

        public static AuthorizedRepresentative? GetRepresentative(IObjectSpace? objectSpace) =>
            objectSpace == null ? null : AuthorizedRepresentative.TryGetInstance(objectSpace);

        public static SystemSettings? GetSettings(IObjectSpace? objectSpace) =>
            objectSpace == null ? null : SystemSettings.TryGetInstance(objectSpace);

        public static SystemSettings GetOrCreateSettings(IObjectSpace objectSpace) =>
            SystemSettings.GetOrCreateInstance(objectSpace);

        public static CompanyProfile GetOrCreateCompanyProfile(IObjectSpace objectSpace) =>
            CompanyProfile.GetOrCreateInstance(objectSpace);

        public static ApplicationNumberingProfile? GetApplicationNumbering(IObjectSpace? objectSpace) =>
            objectSpace == null ? null : ApplicationNumberingProfile.TryGetInstance(objectSpace);

        public static ApplicationNumberingProfile GetOrCreateApplicationNumbering(IObjectSpace objectSpace) =>
            ApplicationNumberingProfile.GetOrCreateInstance(objectSpace);

        public static IObjectSpace? ResolveObjectSpace(IObjectSpace? primary, Application? application) =>
            primary ?? ObjectSpaceHelper.Get(application);
    }
}
