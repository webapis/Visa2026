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
}
