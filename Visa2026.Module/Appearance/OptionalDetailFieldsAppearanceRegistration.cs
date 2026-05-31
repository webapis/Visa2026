using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Appearance;

/// <summary>
/// Registers conditional appearance rules for types marked with <see cref="SupportsOptionalDetailFieldsAttribute"/>.
/// </summary>
internal static class OptionalDetailFieldsAppearanceRegistration
{
    private const string ViewItemRuleSuffix = "_HideOptionalViewItems";
    private const string LayoutItemRuleSuffix = "_HideOptionalLayoutItems";

    public static void Register(ITypesInfo typesInfo)
    {
        foreach (ITypeInfo typeInfo in typesInfo.PersistentTypes)
        {
            if (typeInfo.Type == null || typeInfo.IsAbstract)
            {
                continue;
            }

            if (!typeof(IOptionalDetailFields).IsAssignableFrom(typeInfo.Type))
            {
                continue;
            }

            if (typeInfo.FindAttribute<SupportsOptionalDetailFieldsAttribute>(true) == null)
            {
                continue;
            }

            RegisterForType(typeInfo);
        }
    }

    private static void RegisterForType(ITypeInfo typeInfo)
    {
        string targetItems = OptionalDetailFieldsMetadata.GetOptionalTargetItems(typeInfo);
        if (string.IsNullOrEmpty(targetItems))
        {
            return;
        }

        string typeName = typeInfo.Name;
        if (!HasAppearance(typeInfo, typeName + ViewItemRuleSuffix))
        {
            typeInfo.AddAttribute(CreateHideOptionalAppearance(
                typeName + ViewItemRuleSuffix,
                "ViewItem",
                targetItems));
        }

        if (!HasAppearance(typeInfo, typeName + LayoutItemRuleSuffix))
        {
            typeInfo.AddAttribute(CreateHideOptionalAppearance(
                typeName + LayoutItemRuleSuffix,
                "LayoutItem",
                targetItems));
        }
    }

    private static bool HasAppearance(ITypeInfo typeInfo, string ruleId) =>
        typeInfo.Attributes.OfType<AppearanceAttribute>().Any(a => a.Id == ruleId);

    private static AppearanceAttribute CreateHideOptionalAppearance(
        string ruleId,
        string appearanceItemType,
        string targetItems) =>
        new(ruleId)
        {
            AppearanceItemType = appearanceItemType,
            TargetItems = targetItems,
            Visibility = ViewItemVisibility.Hide,
            Criteria = "!ShowOptionalFields",
            Context = "DetailView",
            Priority = 100,
        };
}

internal static class OptionalDetailFieldsMetadata
{
    internal const string ToggleMemberName = nameof(IOptionalDetailFields.ShowOptionalFields);

    internal static bool Supports(Type type) =>
        type != null
        && typeof(IOptionalDetailFields).IsAssignableFrom(type)
        && type.GetCustomAttributes(typeof(SupportsOptionalDetailFieldsAttribute), inherit: true).Length > 0;

    internal static IReadOnlyList<string> GetOptionalMemberNames(ITypeInfo typeInfo)
    {
        if (typeInfo?.Type == null)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (IMemberInfo member in typeInfo.OwnMembers)
        {
            if (!IsOptionalDetailMember(member))
            {
                continue;
            }

            names.Add(member.Name);
        }

        return names;
    }

    internal static string GetOptionalTargetItems(ITypeInfo typeInfo) =>
        string.Join(';', GetOptionalMemberNames(typeInfo));

    internal static bool HasPopulatedOptionalFields(object target, ITypesInfo typesInfo) =>
        HasPopulatedOptionalFields(target, typesInfo, objectSpace: null);

    internal static bool HasPopulatedOptionalFields(object target, ITypesInfo typesInfo, IObjectSpace objectSpace)
    {
        if (target == null || typesInfo == null)
        {
            return false;
        }

        ITypeInfo typeInfo = typesInfo.FindTypeInfo(target.GetType());
        if (typeInfo == null)
        {
            return false;
        }

        bool isNewObject = objectSpace?.IsNewObject(target) == true;

        foreach (string memberName in GetOptionalMemberNames(typeInfo))
        {
            IMemberInfo member = typeInfo.FindMember(memberName);
            if (member == null || !ShouldConsiderForAutoExpand(member, isNewObject))
            {
                continue;
            }

            object value = member.GetValue(target);
            if (HasMeaningfulOptionalValue(value, member.MemberType))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsOptionalMember(ITypeInfo typeInfo, string propertyName)
    {
        if (typeInfo == null || string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        return GetOptionalMemberNames(typeInfo).Contains(propertyName, StringComparer.Ordinal);
    }

    internal static bool IsOptionalDetailMember(IMemberInfo member)
    {
        if (member == null || !member.IsPublic || !member.IsVisible)
        {
            return false;
        }

        if (string.Equals(member.Name, ToggleMemberName, StringComparison.Ordinal))
        {
            return false;
        }

        if (member.FindAttribute<BrowsableAttribute>() is { Browsable: false })
        {
            return false;
        }

        if (IsHiddenFromDetailView(member))
        {
            return false;
        }

        if (member.FindAttribute<RuleRequiredFieldAttribute>() != null)
        {
            return false;
        }

        // Collections (list properties) are always shown on the detail view; not part of the gear scope.
        if (member.IsList)
        {
            return false;
        }

        if (member.MemberType == typeof(string))
        {
            return true;
        }

        if (member.MemberType == typeof(DateTime?))
        {
            return true;
        }

        if (member.MemberType == typeof(bool) || member.MemberType == typeof(bool?))
        {
            return true;
        }

        Type underlyingType = Nullable.GetUnderlyingType(member.MemberType);
        if (underlyingType?.IsEnum == true)
        {
            return true;
        }

        if (member.MemberType.IsClass && member.MemberType != typeof(string))
        {
            return true;
        }

        if (member.MemberType.IsEnum)
        {
            return true;
        }

        if (member.IsPersistent && member.MemberType == typeof(DateTime))
        {
            return true;
        }

        if (member.FindAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null)
        {
            // Computed display fields (e.g. Person.Age) stay visible next to required data.
            return !IsNotMappedComputedDisplay(member);
        }

        return false;
    }

    private static bool IsNotMappedComputedDisplay(IMemberInfo member) =>
        member.IsReadOnly
        || member.MemberType == typeof(int)
        || member.MemberType == typeof(int?)
        || IsAllowEditFalse(member);

    private static bool ShouldConsiderForAutoExpand(IMemberInfo member, bool isNewObject)
    {
        if (member.FindAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null
            && IsNotMappedComputedDisplay(member))
        {
            return false;
        }

        // On new records, StartDate is often defaulted in OnCreated; still allow other optional members to expand.
        if (isNewObject && member.MemberType == typeof(DateTime))
        {
            return false;
        }

        return true;
    }

    private static bool IsHiddenFromDetailView(IMemberInfo member) =>
        member.Attributes.OfType<ModelDefaultAttribute>()
            .Any(attribute => attribute.PropertyName == "VisibleInDetailView"
                && string.Equals(
                    Convert.ToString(attribute.PropertyValue, System.Globalization.CultureInfo.InvariantCulture),
                    bool.FalseString,
                    StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowEditFalse(IMemberInfo member) =>
        member.Attributes.OfType<ModelDefaultAttribute>()
            .Any(attribute => attribute.PropertyName == "AllowEdit"
                && string.Equals(
                    Convert.ToString(attribute.PropertyValue, System.Globalization.CultureInfo.InvariantCulture),
                    bool.FalseString,
                    StringComparison.OrdinalIgnoreCase));

    private static bool HasMeaningfulOptionalValue(object value, Type memberType)
    {
        if (value == null)
        {
            return false;
        }

        if (value is string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        if (value is DateTime dateTime)
        {
            return dateTime != default;
        }

        if (value is bool boolean)
        {
            return boolean;
        }

        if (value is byte[] bytes)
        {
            return bytes.Length > 0;
        }

        Type underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        if (underlyingType.IsEnum)
        {
            return true;
        }

        return true;
    }
}
