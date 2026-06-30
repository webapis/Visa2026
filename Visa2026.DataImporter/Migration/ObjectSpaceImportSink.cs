using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.DataImporter.Migration;

/// <summary>
/// Creates XAF entities from OData-style payload dictionaries (FK as { ID = guid }).
/// </summary>
internal static class ObjectSpaceImportSink
{
    public static void ApplyPayload(IObjectSpace objectSpace, object entity, IReadOnlyDictionary<string, object?> payload)
    {
        foreach (var (propertyName, rawValue) in payload)
        {
            if (rawValue == null)
                continue;

            var member = objectSpace.TypesInfo.FindTypeInfo(entity.GetType())?.FindMember(propertyName);
            if (member == null)
                continue;

            if (rawValue is Dictionary<string, object?> nestedDict)
            {
                if (TryReadFkId(nestedDict, out var fkId))
                {
                    SetReference(objectSpace, entity, propertyName, member.MemberType, fkId);
                    continue;
                }

                SetNestedOwned(objectSpace, entity, propertyName, member.MemberType, nestedDict);
                continue;
            }

            if (TryReadFkIdFromAnonymous(rawValue, out var refId))
            {
                SetReference(objectSpace, entity, propertyName, member.MemberType, refId);
                continue;
            }

            object? converted = ConvertScalar(rawValue, member.MemberType);
            member.SetValue(entity, converted);
        }
    }

    private static void SetNestedOwned(
        IObjectSpace objectSpace,
        object parent,
        string propertyName,
        Type memberType,
        IReadOnlyDictionary<string, object?> nestedDict)
    {
        var child = objectSpace.CreateObject(memberType);
        ApplyPayload(objectSpace, child, nestedDict);
        var parentMember = objectSpace.TypesInfo.FindTypeInfo(parent.GetType())?.FindMember(propertyName);
        parentMember?.SetValue(parent, child);
    }

    private static void SetReference(
        IObjectSpace objectSpace,
        object entity,
        string propertyName,
        Type memberType,
        Guid id)
    {
        var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        var referenced = objectSpace.GetObjectByKey(targetType, id);
        if (referenced == null)
            throw new InvalidOperationException($"FK {propertyName} target {targetType.Name}({id}) not found.");

        var member = objectSpace.TypesInfo.FindTypeInfo(entity.GetType())?.FindMember(propertyName);
        member?.SetValue(entity, referenced);
    }

    private static bool TryReadFkId(IReadOnlyDictionary<string, object?> dict, out Guid id)
    {
        id = Guid.Empty;
        if (!dict.TryGetValue("ID", out var raw) || raw == null)
            return false;

        return raw switch
        {
            Guid g => (id = g) != Guid.Empty || true,
            string s when Guid.TryParse(s, out var parsed) => (id = parsed) != Guid.Empty || true,
            _ => false,
        };
    }

    private static bool TryReadFkIdFromAnonymous(object rawValue, out Guid id)
    {
        id = Guid.Empty;
        var idProperty = rawValue.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
        if (idProperty == null)
            return false;

        var value = idProperty.GetValue(rawValue);
        return value switch
        {
            Guid g => (id = g) != Guid.Empty || true,
            string s when Guid.TryParse(s, out var parsed) => (id = parsed) != Guid.Empty || true,
            _ => false,
        };
    }

    private static object? ConvertScalar(object rawValue, Type memberType)
    {
        var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (targetType.IsInstanceOfType(rawValue))
            return rawValue;

        if (targetType.IsEnum)
        {
            if (rawValue is string enumText)
                return Enum.Parse(targetType, enumText, ignoreCase: true);

            return Enum.ToObject(targetType, rawValue);
        }

        if (targetType == typeof(Guid) && rawValue is string guidText && Guid.TryParse(guidText, out var guid))
            return guid;

        if (targetType == typeof(DateTime) && rawValue is DateTime dt)
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

        return Convert.ChangeType(rawValue, targetType);
    }
}
