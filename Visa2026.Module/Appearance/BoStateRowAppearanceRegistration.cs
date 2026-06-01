using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Appearance;

/// <summary>
/// Registers ListView row <see cref="AppearanceAttribute"/> rules for <see cref="IBoListRowState"/> types.
/// </summary>
internal static class BoStateRowAppearanceRegistration
{
    public static void Register(ITypesInfo typesInfo)
    {
        ITypeInfo? applicationType = typesInfo.FindTypeInfo(typeof(Application));
        if (applicationType?.Type == null)
            return;

        foreach (BoStateAppearance appearance in BoStateAppearanceColors.ApplicationProgressRowStates)
        {
            if (HasRule(applicationType, appearance.StateCode))
                continue;

            applicationType.AddAttribute(CreateListViewRowAppearance(appearance));
        }
    }

    private static bool HasRule(ITypeInfo typeInfo, string stateCode) =>
        typeInfo.Attributes.OfType<AppearanceAttribute>()
            .Any(a => a.Id == RuleId(stateCode));

    private static string RuleId(string stateCode) => $"AppProgressRow_{stateCode}";

    private static AppearanceAttribute CreateListViewRowAppearance(BoStateAppearance appearance) =>
        new(RuleId(appearance.StateCode))
        {
            AppearanceItemType = "ViewItem",
            TargetItems = "*",
            Criteria = $"IsDeleted = false And PrimaryStateCode = '{appearance.StateCode}'",
            Context = "ListView",
            BackColor = appearance.BackColor,
            FontColor = appearance.FontColor,
            Priority = appearance.DisplayPriority,
        };
}
