using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

internal static class MailMergeFeatureRegistration
{
    public static void HideFromApplicationModel(ITypesInfo typesInfo)
    {
        HideType(typesInfo, typeof(RichTextMailMergeData));
        HideType(typesInfo, typeof(MailMergeVisibility));
    }

    static void HideType(ITypesInfo typesInfo, Type type)
    {
        ITypeInfo? typeInfo = typesInfo.FindTypeInfo(type);
        if (typeInfo != null)
            typeInfo.AddAttribute(new NavigationItemAttribute(false));
    }
}
