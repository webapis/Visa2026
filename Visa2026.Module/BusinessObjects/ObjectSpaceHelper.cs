using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves the XAF session for an entity. With <c>UseObjectSpaceLinkProxies()</c>, EF proxies implement
/// <see cref="IObjectSpaceLink"/> at runtime even when the entity class does not declare it.
/// </summary>
public static class ObjectSpaceHelper
{
    public static IObjectSpace? Get(BaseObject? obj) =>
        obj is IObjectSpaceLink link ? link.ObjectSpace : null;

    /// <summary>
    /// Resolves the object space that owns <paramref name="entity"/>, or falls back to
    /// <paramref name="contextObjectSpace"/> / its outermost parent session.
    /// </summary>
    public static IObjectSpace ResolveObjectSpace(IObjectSpace contextObjectSpace, BaseObject? entity) =>
        Get(entity) ?? GetRootObjectSpace(contextObjectSpace) ?? contextObjectSpace;

    /// <summary>
    /// Walks a version-dependent parent-object-space chain when present (XPO nested spaces).
    /// EF Core aggregated-child popups usually use a separate session resolved via <see cref="Get"/>.
    /// </summary>
    public static IObjectSpace? GetRootObjectSpace(IObjectSpace? objectSpace)
    {
        var current = objectSpace;
        while (current != null)
        {
            var parent = GetParentObjectSpace(current);
            if (parent == null || ReferenceEquals(parent, current))
                return current;

            current = parent;
        }

        return objectSpace;
    }

    private static IObjectSpace? GetParentObjectSpace(IObjectSpace objectSpace)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var propertyName in new[] { "ParentObjectSpace", "OwnerObjectSpace" })
        {
            var property = objectSpace.GetType().GetProperty(propertyName, flags);
            if (property?.GetValue(objectSpace) is IObjectSpace parent)
                return parent;
        }

        return null;
    }
}
