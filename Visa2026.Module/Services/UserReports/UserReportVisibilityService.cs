using DevExpress.Data.Filtering;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <inheritdoc cref="IUserReportVisibilityService"/>
    public class UserReportVisibilityService : IUserReportVisibilityService
    {
        public bool IsTemplateVisible(UserReportTemplate template, Application application)
        {
            if (!template.IsActive)
                return false;

            return template.ApplicabilityMode switch
            {
                ApplicabilityMode.AllTypes => true,
                ApplicabilityMode.SpecificTypes => IsSpecificTypeMatch(template, application),
                ApplicabilityMode.DataDriven => EvaluateDataCriteria(template, application),
                _ => false
            };
        }

        private bool IsSpecificTypeMatch(UserReportTemplate template, Application application)
        {
            if (string.IsNullOrEmpty(template.VisibilityCriteria))
            {
                // Fallback: check ApplicableTypes list directly
                if (template.ApplicableTypes?.Any() != true)
                    return true;

                return template.ApplicableTypes.Any(at => at.Name == application.ApplicationType?.Name);
            }

            // Use XAF criteria evaluation
            return EvaluateCriteria(template.VisibilityCriteria, application);
        }

        private bool EvaluateDataCriteria(UserReportTemplate template, Application application)
        {
            if (string.IsNullOrEmpty(template.VisibilityCriteria))
                return false;

            return EvaluateCriteria(template.VisibilityCriteria, application);
        }

        private bool EvaluateCriteria(string criteriaString, Application application)
        {
            try
            {
                var criteria = CriteriaOperator.Parse(criteriaString);
                return criteria.Evaluate(application);
            }
            catch (Exception)
            {
                // If criteria is invalid, don't show the template
                return false;
            }
        }
    }
}
