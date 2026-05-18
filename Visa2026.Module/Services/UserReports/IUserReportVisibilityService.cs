using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <summary>Evaluates optional type, project-contract, and criteria filters for Resminamalar.</summary>
    public interface IUserReportVisibilityService
    {
        /// <summary>Check if a template is visible for a given application.</summary>
        /// <param name="template">The user-defined template</param>
        /// <param name="application">The application to check against</param>
        /// <returns>True if the template should be shown in Resminamalar</returns>
        bool IsTemplateVisible(UserReportTemplate template, Application application);
    }
}
