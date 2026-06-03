using System;
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
        /// <summary>
        /// Resminamalar visibility is the AND of optional filters: application types, project contracts, visibility criteria.
        /// Empty link lists or empty criteria mean no filter on that axis.
        /// </summary>
        public bool IsTemplateVisible(UserReportTemplate template, Application application)
        {
            if (!template.IsActive)
                return false;

            return IsApplicationTypeMatch(template, application)
                && IsProjectContractMatch(template, application)
                && MatchesVisibilityCriteria(template, application);
        }

        /// <summary>
        /// When <see cref="UserReportTemplate.ApplicableTypeLinks"/> has rows, the application type must match one of them.
        /// Empty list means no application-type filter.
        /// </summary>
        private static bool IsApplicationTypeMatch(UserReportTemplate template, Application application)
        {
            var typeLinks = template.ApplicableTypeLinks?
                .Where(l => l.ApplicationTypeId != Guid.Empty || l.ApplicationType != null)
                .ToList();
            if (typeLinks == null || typeLinks.Count == 0)
                return true;

            var applicationType = application?.ApplicationType;
            if (applicationType == null)
                return false;

            var applicationTypeId = applicationType.ID;
            return typeLinks.Any(l =>
                l.ApplicationTypeId == applicationTypeId
                || (l.ApplicationType != null
                    && string.Equals(l.ApplicationType.Name, applicationType.Name, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// When <see cref="UserReportTemplate.ApplicableProjectContractLinks"/> has rows, the application must have a matching
        /// <see cref="Application.ProjectContract"/>. Empty list means no project-contract filter.
        /// </summary>
        private static bool IsProjectContractMatch(UserReportTemplate template, Application application)
        {
            var contractLinks = template.ApplicableProjectContractLinks?
                .Where(l => l.ProjectContractId != Guid.Empty || l.ProjectContract != null)
                .ToList();
            if (contractLinks == null || contractLinks.Count == 0)
                return true;

            var applicationContract = application?.ProjectContract;
            if (applicationContract == null)
                return false;

            var applicationContractId = applicationContract.ID;
            return contractLinks.Any(l =>
                l.ProjectContractId == applicationContractId
                || (l.ProjectContract != null && l.ProjectContract.ID == applicationContractId));
        }

        /// <summary>When <see cref="UserReportTemplate.VisibilityCriteria"/> is empty, no extra filter. Otherwise criteria must pass.</summary>
        private static bool MatchesVisibilityCriteria(UserReportTemplate template, Application application)
        {
            if (string.IsNullOrWhiteSpace(template.VisibilityCriteria))
                return true;

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
                    application.ApplicationItems?.Where(i => i != null),
                    criteriaString),
                UserReportBoType.Person => AnyChildMatches(GetPersonsFromApplication(application), criteriaString),
                _ => EvaluateCriteriaOnInstance(criteriaString, application)
            };
        }

        private static IEnumerable<Person> GetPersonsFromApplication(Application application)
        {
            var items = application.ApplicationItems?.Where(i => i != null);
            if (items == null)
                yield break;

            foreach (var item in items)
            {
                if (item.Person != null)
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
                return false;
            }
        }
    }
}
