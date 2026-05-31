using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.Appearance;

namespace Visa2026.Module.BusinessObjects;

public static class OptionalDetailFieldsSupport
{
    public static bool Supports(System.Type type) => OptionalDetailFieldsMetadata.Supports(type);

    public static bool HasPopulatedOptionalFields(object target, IObjectSpace objectSpace) =>
        target != null
        && objectSpace != null
        && OptionalDetailFieldsMetadata.HasPopulatedOptionalFields(target, objectSpace.TypesInfo, objectSpace);

    public static bool HasPopulatedOptionalFields(object target, ITypesInfo typesInfo) =>
        OptionalDetailFieldsMetadata.HasPopulatedOptionalFields(target, typesInfo, objectSpace: null);

    public static bool IsOptionalMember(ITypeInfo typeInfo, string propertyName) =>
        OptionalDetailFieldsMetadata.IsOptionalMember(typeInfo, propertyName);
}
