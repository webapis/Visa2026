using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Appearance
{
    /// <summary>
    /// Applies gray background and text to ListView and DetailView when <see cref="ISoftDelete.IsDeleted"/> is true.
    /// </summary>
    internal static class SoftDeleteAppearanceRegistration
    {
        public const string RuleId = "SoftDelete_GrayOutIfDeleted";
        public const string LegacyRuleId = "GrayOutIfDeleted";

        public static void Register(ITypesInfo typesInfo)
        {
            foreach (ITypeInfo typeInfo in typesInfo.PersistentTypes)
            {
                if (typeInfo.Type == null || typeInfo.IsAbstract)
                    continue;
                if (!typeof(ISoftDelete).IsAssignableFrom(typeInfo.Type))
                    continue;
                if (HasSoftDeleteAppearance(typeInfo))
                    continue;

                typeInfo.AddAttribute(CreateViewItemAppearance());
                typeInfo.AddAttribute(CreateLayoutItemAppearance());
            }
        }

        private static bool HasSoftDeleteAppearance(ITypeInfo typeInfo) =>
            typeInfo.Attributes.OfType<AppearanceAttribute>()
                .Any(a => a.Id == RuleId || a.Id == LegacyRuleId);

        private static AppearanceAttribute CreateViewItemAppearance() =>
            new(RuleId)
            {
                AppearanceItemType = "ViewItem",
                TargetItems = "*",
                Criteria = "IsDeleted",
                Context = "ListView,DetailView",
                BackColor = "Gainsboro",
                FontColor = "Gray",
                Priority = 500,
            };

        private static AppearanceAttribute CreateLayoutItemAppearance() =>
            new(RuleId + "_Layout")
            {
                AppearanceItemType = "LayoutItem",
                TargetItems = "*",
                Criteria = "IsDeleted",
                Context = "DetailView",
                BackColor = "Gainsboro",
                FontColor = "Gray",
                Priority = 500,
            };
    }
}
