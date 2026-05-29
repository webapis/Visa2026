using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Validation;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Validation
{
    /// <summary>
    /// Adds <c>!IsDeleted</c> to <see cref="RuleRequiredFieldAttribute.TargetCriteria"/> on members
    /// declared on types that implement <see cref="ISoftDelete"/>.
    /// </summary>
    internal static class SoftDeleteRuleRequiredFieldRegistration
    {
        public const string ActiveOnlyCriteria = "!IsDeleted";

        public static void Register(ITypesInfo typesInfo)
        {
            foreach (ITypeInfo typeInfo in typesInfo.PersistentTypes)
            {
                if (typeInfo.Type == null || typeInfo.IsAbstract)
                    continue;
                if (!typeof(ISoftDelete).IsAssignableFrom(typeInfo.Type))
                    continue;

                foreach (IMemberInfo member in typeInfo.OwnMembers)
                    AugmentMemberRequiredFieldRules(member);
            }
        }

        private static void AugmentMemberRequiredFieldRules(IMemberInfo member)
        {
            foreach (RuleRequiredFieldAttribute rule in member.Attributes.OfType<RuleRequiredFieldAttribute>().ToList())
            {
                rule.TargetCriteria = MergeActiveOnlyCriteria(rule.TargetCriteria);
            }
        }

        internal static string MergeActiveOnlyCriteria(string? existingCriteria)
        {
            if (string.IsNullOrWhiteSpace(existingCriteria))
                return ActiveOnlyCriteria;

            if (ContainsIsDeletedCriterion(existingCriteria))
                return existingCriteria;

            return $"({existingCriteria}) And {ActiveOnlyCriteria}";
        }

        private static bool ContainsIsDeletedCriterion(string criteria) =>
            criteria.Contains("IsDeleted", System.StringComparison.OrdinalIgnoreCase);
    }
}
