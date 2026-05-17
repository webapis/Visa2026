using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
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

            // Use XAF criteria evaluation (target type follows RootBoType; see CriteriaTargetType on the template)
            return EvaluateCriteriaForTemplate(template, application);
        }

        private bool EvaluateDataCriteria(UserReportTemplate template, Application application)
        {
            if (string.IsNullOrEmpty(template.VisibilityCriteria))
                return false;

            return EvaluateCriteriaForTemplate(template, application);
        }

        /// <summary>
        /// Evaluates <see cref="UserReportTemplate.VisibilityCriteria"/> against the same type as the criteria editor
        /// (<see cref="UserReportTemplate.CriteriaTargetType"/>): Application instance, or any matching child row.
        /// </summary>
        private static bool EvaluateCriteriaForTemplate(UserReportTemplate template, Application application)
        {
            var criteriaString = template.VisibilityCriteria;
            if (string.IsNullOrEmpty(criteriaString) || application == null)
                return false;

            return template.RootBoType switch
            {
                UserReportBoType.Application => EvaluateCriteriaOnInstance(criteriaString, application),
                UserReportBoType.ApplicationItem => AnyChildMatches(
                    application.ApplicationItems?.Where(i => i != null && !i.IsDeleted),
                    criteriaString),
                UserReportBoType.Person => AnyChildMatches(GetPersonsFromApplication(application), criteriaString),
                _ => EvaluateCriteriaOnInstance(criteriaString, application)
            };
        }

        private static IEnumerable<Person> GetPersonsFromApplication(Application application)
        {
            var items = application.ApplicationItems?.Where(i => i != null && !i.IsDeleted);
            if (items == null)
                yield break;

            foreach (var item in items)
            {
                if (item.Person != null && !item.Person.IsDeleted)
                    yield return item.Person;
            }
        }

        private static bool AnyChildMatches<T>(IEnumerable<T> rows, string criteriaString)
            where T : class
        {
            if (rows == null)
                return false;

            foreach (var row in rows)
            {
                if (EvaluateCriteriaOnInstance(criteriaString, row))
                    return true;
            }

            return false;
        }

        private static bool EvaluateCriteriaOnInstance(string criteriaString, object instance)
        {
            if (instance == null || string.IsNullOrEmpty(criteriaString))
                return false;

            try
            {
                var criteria = CriteriaOperator.Parse(criteriaString);
                var evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(instance), criteria, false);
                var value = evaluator.Evaluate(instance);
                return value is bool b && b;
            }
            catch (Exception)
            {
                // If criteria is invalid, don't show the template
                return false;
            }
        }
    }
}
